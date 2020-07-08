using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReunionGet.Aria2Rpc;
using ReunionGet.Aria2Rpc.Json;

namespace ReunionGet.Models.Aria2
{
    public sealed class Aria2Host : IHostedService, IDisposable, IAsyncDisposable
    {
        private readonly ProcessStartInfo _processStartInfo;
        private readonly ILogger? _logger;

        public int ListeningPort { get; }
        public string RpcToken { get; }
        public int RefreshInterval { get; set; }

        private Process? _process;
        public int ProcessId => _process?.Id ?? throw new InvalidOperationException("Host not started or already exited.");

        private Aria2Connection? _connection;
        public Aria2Connection Connection => _connection ?? throw new InvalidOperationException("Host not started or already exited.");

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task? _refreshTask;

        public Aria2Host(IOptions<Aria2HostOptions> options, ILoggerFactory? loggerFactory = null)
            : this(options.Value.ExecutablePath ?? "aria2c",
                 options.Value.WorkingDirectory ?? ".",
                 options.Value.Token,
                 options.Value.ListenPort,
                 options.Value.RefreshInterval,
                 loggerFactory?.CreateLogger<Aria2Host>())
        {
        }

        public Aria2Host(
            string executablePath,
            string workingDirectory,
            string? token = null,
            int listeningPort = 6800,
            int refreshInterval = 1000,
            ILogger? logger = null)
        {
            if (token is null)
            {
                static string GenerateRandomString(int bytes)
                {
                    Span<byte> span = stackalloc byte[bytes];

                    var random = new Random();
                    random.NextBytes(span);

                    // TODO: Use Convert.ToHex
                    const string HexString = "0123456789abcdef";
                    Span<char> chars = stackalloc char[bytes * 2];
                    for (int i = 0; i < span.Length; i++)
                    {
                        chars[i * 2] = HexString[span[i] >> 4];
                        chars[i * 2 + 1] = HexString[span[i] & 0x0F];
                    }
                    return new string(chars);
                }
                token = GenerateRandomString(8);
            }

            _processStartInfo = new ProcessStartInfo(executablePath)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                ArgumentList =
                {
                    "--enable-rpc=true",
                    $"--rpc-listen-port={listeningPort}",
                    $"--rpc-secret={token}",
                    "--bt-save-metadata=true",
                    "-d ."
                }
            };

            ListeningPort = listeningPort;
            RpcToken = token;
            RefreshInterval = refreshInterval;
            _logger = logger;
        }

        public void Dispose()
        {
            _process?.Dispose();
            _process = null;

            _connection?.Dispose();
            _connection = null;

            _cts.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            _process?.Dispose();
            _process = null;

            if (_connection != null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
                _connection = null;
            }
            _cts.Dispose();
        }

        /// <summary>
        /// Try shut down an earlier started instance.
        /// </summary>
        /// <param name="port">The port specified for the instance to listen.</param>
        /// <param name="token">The secret token specified for the instance.</param>
        /// <param name="processId">The process id of the instance.
        /// <see langword="null"/> if previous host fails to log its pid. </param>
        /// <returns>If the specified port and token can be safely reused.</returns>
        public static async ValueTask<bool> TryShutdownExistingInstanceAsync(int port, string token, int? processId)
        {
            if (processId is int id)
            {
                try
                {
                    using var process = Process.GetProcessById(id);
                    if (!process.HasExited)
                        process.Kill();
                    return true;
                }
                catch (ArgumentException)
                {
                    // The process has exited. Assuming it's exited by external reason.
                    return true;
                }
                catch (InvalidOperationException)
                {
                    // The process has exited during the query.
                    return true;
                }
            }
            else
            {
                try
                {
                    await using var connection = new Aria2Connection("localhost", port, token);
                    // Let dispose to shutdown it
                    return true;
                }
                catch (WebException)
                {
                    // Seems no process is listening to the port.
                    return true;
                }
                catch (JsonRpcException)
                {
                    // The process rejects the shutdown request. The port cannot be reused.
                    return false;
                }
            }
        }

        private async Task RefreshAsync()
        {
            async Task<bool> InitialConnectionAsync(int retries, int interval)
            {
                for (int i = 0; i < retries; i++)
                {
                    try
                    {
                        var version = await Connection.GetVersionAsync().ConfigureAwait(false);
                        return true;
                    }
                    catch (JsonRpcException) // RPC is configured wrong.
                    {
                        _logger?.LogCritical("The started aria2 instance refuses initial query.");
                        return false;
                    }
                    catch (WebException) // Maybe the process hasn't started
                    {
                    }

                    await Task.Delay(interval).ConfigureAwait(false);
                }

                _logger?.LogCritical($"Initial aria2 query fails with {retries} tries.");
                return false;
            }

            if (!await InitialConnectionAsync(10, 1000).ConfigureAwait(false))
            {
                Updated?.Invoke();
                IsFaulted = true;
                return;
            }

            while (true)
            {
                if (_cts.IsCancellationRequested)
                    return;

                try
                {
                    _ = await Connection.TellActiveAsync().ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (WebException)
                {
                    return;
                }
#pragma warning disable CA1031 // Don't catch general exception type
                catch (Exception e)
#pragma warning restore CA1031 // Don't catch general exception type
                {
                    _logger?.LogCritical(e, "An unexpected exception happens during refreshing loop.");
                    return;
                }

                Updated?.Invoke();
                await Task.Delay(RefreshInterval).ConfigureAwait(false);
            }
        }

        public bool IsFaulted { get; private set; }

        public event Action? Updated;

        public Task StartAsync(CancellationToken cancellationToken)
            => Task.Run(() =>
            {
                if (_logger?.IsEnabled(LogLevel.Information) == true)
                {
                    _logger.LogInformation("Using aria2 executable path: {0}", _processStartInfo.FileName);
                    _logger.LogInformation("Using working directory: {0}", _processStartInfo.WorkingDirectory);
                    _logger.LogInformation("Using rpc token: {0}", RpcToken);
                    _logger.LogInformation("Using listening port: {0}", ListeningPort);
                }

                try
                {
                    _process = Process.Start(_processStartInfo) ?? throw new InvalidOperationException("Failed to start aria2 process.");
                    _logger?.LogInformation("aria2 started with PID {0}.", _process.Id);
                }
#pragma warning disable CA1031 // Don't catch general exception type
                catch (Exception ex)
#pragma warning restore CA1031 // Don't catch general exception type
                {
                    _logger?.LogCritical(ex, "Failed to start aria2 process.");
                    return;
                }

                _connection = new Aria2Connection("localhost", ListeningPort, RpcToken, shutdownOnDisposal: true);
                _refreshTask = RefreshAsync();
            }, cancellationToken);

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_refreshTask is null)
                return;

            _cts.Cancel();
            await _refreshTask.ConfigureAwait(false);
            _refreshTask = null;
            _logger?.LogInformation("Refreshing loop cancelled");

            Debug.Assert(_connection != null);
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
            _logger?.LogInformation("Shutdown request sent to aria2.");

            Debug.Assert(_process != null);
            await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            _process = null;
            _logger?.LogInformation("Aria2 process exited.");
        }
    }
}

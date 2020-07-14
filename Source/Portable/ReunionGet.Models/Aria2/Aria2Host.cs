using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReunionGet.Aria2Rpc;
using ReunionGet.Aria2Rpc.Json;

namespace ReunionGet.Models.Aria2
{
    public sealed class Aria2Host : IDisposable, IAsyncDisposable
    {
        private readonly ILogger? _logger;

        private readonly Process? _process;
        public int ProcessId => _process?.Id ?? throw new InvalidOperationException("The aria2 process doesn't start succesfully.");

        private readonly Aria2Connection? _connection;
        public Aria2Connection Connection => _connection ?? throw new InvalidOperationException("The aria2 process doesn't start succesfully.");

        public int ListenPort { get; }

        public bool SuccessfullyStarted { get; }

        public Aria2Host(IOptions<Aria2HostOptions> options, ILogger<Aria2Host>? logger = null)
            : this(
                options.Value.ExecutablePath ?? "aria2c",
                options.Value.WorkingDirectory ?? ".",
                options.Value.Token,
                options.Value.ListenPort,
                logger)
        {
        }

        public Aria2Host(
            string executablePath,
            string workingDirectory,
            string? token = null,
            int? listeningPort = null,
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

            ListenPort = listeningPort ?? new Random().Next(6000, 7000);

            var psi = new ProcessStartInfo(executablePath)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                ArgumentList =
                {
                    "--enable-rpc=true",
                    $"--rpc-listen-port={ListenPort}",
                    $"--rpc-secret={token}",
                    "--pause-metadata=true",
                    "--bt-save-metadata=true",
                    "--rpc-save-upload-metadata=false",
                    "-d ."
                }
            };

            _logger = logger;

            try
            {
                _logger?.LogInformation("Using aria2 executable path: {0}", executablePath);
                _logger?.LogInformation("Using working directory: {0}", workingDirectory);
                _logger?.LogInformation("Using listening port: {0}", ListenPort);
                _logger?.LogInformation("Using RPC token: {0}", token);

                _process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start aria2 process.");
                _connection = new Aria2Connection("localhost", ListenPort, token, shutdownOnDisposal: true);

                _logger?.LogInformation("aria2 started with PID {0}.", ProcessId);
                SuccessfullyStarted = true;
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "Failed to start aria2 process.");

                _process = null;
                _connection = null;

                SuccessfullyStarted = false;
            }
        }

        public void Dispose()
        {
            _process?.Dispose();
            _connection?.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            _process?.Dispose();
            return _connection?.DisposeAsync() ?? default;
        }

        public async ValueTask WaitForShutdownAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
                _logger?.LogInformation("RPC shotdown request sent to aria2.");
            }

            if (_process != null)
            {
                await _process.WaitForExitAsync().ConfigureAwait(false);
                _logger?.LogInformation("aria2 process exited.");
            }
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
                catch (HttpRequestException)
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
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReunionGet.Aria2Rpc.Json;

namespace ReunionGet.Models.Aria2
{
    public sealed class Aria2MonitorService : IHostedService, IDisposable, IAsyncDisposable
    {
        private Aria2Host? _host;
        private readonly Aria2HostOptions _options;
        private readonly SimpleMessenger _messenger;
        private readonly ILogger? _logger;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task? _refreshTask;

        private bool _disposed;

        public Aria2MonitorService(IOptions<Aria2HostOptions> options, SimpleMessenger messenger, ILogger<Aria2MonitorService>? logger = null)
        {
            _options = options.Value;
            _messenger = messenger;
            _logger = logger;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _host?.Dispose();
            _host = null;

            _cts.Cancel();
            _cts.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_host != null)
            {
                await _host.DisposeAsync().ConfigureAwait(false);
                _host = null;
            }

            _cts.Cancel();
            _cts.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            string executablePath = _options.ExecutablePath ?? "aria2c";
            string workingDirectory = _options.WorkingDirectory ?? ".";

            if (_logger?.IsEnabled(LogLevel.Information) == true)
            {
                _logger.LogInformation("Using aria2 executable path: {0}", executablePath);
                _logger.LogInformation("Using working directory: {0}", workingDirectory);
                _logger.LogInformation("Using listening port: {0}", _options.ListenPort);
            }

            try
            {
                _host = new Aria2Host(
                    executablePath,
                    workingDirectory,
                    _options.Token,
                    _options.ListenPort,
                    _logger);
            }
#pragma warning disable CA1031 // Don't catch general exception type
            catch (Exception ex)
#pragma warning restore CA1031 // Don't catch general exception type
            {
                _logger?.LogCritical(ex, "Failed to start aria2 process.");
                return;
            }

            _logger?.LogInformation("aria2 started with PID {0}.", _host.ProcessId);
            _logger?.LogInformation("Using rpc token: {0}", _host.RpcToken);

            _refreshTask = RefreshAsync();

            string task = _messenger.Downloads.Take(cancellationToken);

            if (task.StartsWith("magnet:", StringComparison.Ordinal))
            {
                _logger?.LogInformation("Adding magnet link: {0}", task);
                var gid = await _host.Connection.AddUriAsync(task).ConfigureAwait(false);
                _logger?.LogInformation("Magnet task added with GID {0}", gid);
            }
            else
            {
                _logger?.LogInformation("Adding torrent file: {0}", task);
                using var stream = File.OpenRead(task);
                var gid = await _host.Connection.AddTorrentAsync(stream).ConfigureAwait(false);
                _logger?.LogInformation("Torrent task added with GID {0}", gid);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_refreshTask is null)
                return;

            _cts.Cancel();
            await _refreshTask.ConfigureAwait(false);
            _refreshTask = null;
            _logger?.LogInformation("Refreshing loop cancelled");

            Debug.Assert(_host != null);
            await _host.WaitForShutdownAsync().ConfigureAwait(false);
            await _host.DisposeAsync().ConfigureAwait(false);
            _host = null;
        }

        private async Task RefreshAsync()
        {
            Debug.Assert(_host != null);
            var token = _cts.Token;

            async Task<bool> InitialConnectionAsync(int retries, int interval)
            {
                Debug.Assert(_host != null);

                for (int i = 0; i < retries; i++)
                {
                    try
                    {
                        var version = await _host.Connection.GetVersionAsync().ConfigureAwait(false);
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
                IsFaulted = true;
                return;
            }

            while (true)
            {
                if (token.IsCancellationRequested)
                    return;

                try
                {
                    _messenger.Updated?.Invoke(await _host.Connection.TellActiveAsync().ConfigureAwait(false));
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

                await Task.Delay(_options.RefreshInterval).ConfigureAwait(false);
            }
        }

        public bool IsFaulted { get; private set; }
    }
}

using System;
using System.Diagnostics;
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
        private readonly ILogger? _logger;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task? _refreshTask;

        public Aria2MonitorService(IOptions<Aria2HostOptions> options, ILoggerFactory? loggerFactory = null)
        {
            _options = options.Value;
            _logger = loggerFactory?.CreateLogger<Aria2MonitorService>();
        }

        public void Dispose()
        {
            _host?.Dispose();
            _host = null;

            _cts.Cancel();
            _cts.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_host != null)
            {
                await _host.DisposeAsync().ConfigureAwait(false);
                _host = null;
            }

            _cts.Cancel();
            _cts.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
            => Task.Run(() =>
            {
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
                        _options.ListenPort);
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
            }, cancellationToken);

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
                    _ = await _host.Connection.TellActiveAsync().ConfigureAwait(false);
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

                _logger?.LogInformation("Refreshed from aria2.");
                Updated?.Invoke();
                await Task.Delay(_options.RefreshInterval).ConfigureAwait(false);
            }
        }

        public bool IsFaulted { get; private set; }
        public event Action? Updated;
    }
}

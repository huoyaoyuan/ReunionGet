using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
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
        private readonly Aria2HostOptions _options;
        private readonly Aria2Host _host;
        private readonly Aria2State _state;
        private readonly ILogger? _logger;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task? _refreshTask;

        private bool _disposed;

        public Aria2MonitorService(
            IOptions<Aria2HostOptions> options,
            Aria2Host host,
            Aria2State state,
            ILogger<Aria2MonitorService>? logger = null)
        {
            _options = options.Value;
            _host = host;
            _state = state;
            _logger = logger;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _cts.Cancel();
            _cts.Dispose();

            _refreshTask?.Wait();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            _disposed = true;

            _cts.Cancel();
            _cts.Dispose();

            if (_refreshTask != null)
                await _refreshTask.ConfigureAwait(false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (!_host.SuccessfullyStarted)
                return;

            _refreshTask = RefreshAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_refreshTask is null)
                return;

            _cts.Cancel();
            await _refreshTask.ConfigureAwait(false);
            _refreshTask = null;
            _logger?.LogInformation("Refreshing loop cancelled");

            await _host.WaitForShutdownAsync().ConfigureAwait(false);
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
                    catch (HttpRequestException) // Maybe the process hasn't started
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
                    var tasks = (await _host.Connection.TellActiveAsync().ConfigureAwait(true)).AsEnumerable();

                    for (int i = 0; true; i += 10)
                    {
                        var waiting = await _host.Connection.TellWaitingAsync(i, 10).ConfigureAwait(false);

                        if (waiting.Length == 0)
                            break;

                        tasks = tasks.Concat(waiting);
                    }

                    for (int i = 0; true; i += 10)
                    {
                        var stopped = await _host.Connection.TellStoppedAsync(i, 10).ConfigureAwait(false);

                        if (stopped.Length == 0)
                            break;

                        tasks = tasks.Concat(stopped);
                    }

                    _state.PostAllTrackedRefresh(tasks);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (HttpRequestException)
                {
                    return;
                }
                catch (Exception e)
                {
                    _logger?.LogCritical(e, "An unexpected exception happens during refreshing loop.");
                    return;
                }

                if (token.IsCancellationRequested)
                    return;

                await Task.Delay(_options.RefreshInterval).ConfigureAwait(false);
            }
        }

        public bool IsFaulted { get; private set; }
    }
}

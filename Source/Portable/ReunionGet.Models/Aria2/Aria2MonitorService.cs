using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
        private readonly SimpleMessenger _messenger;
        private readonly ILogger? _logger;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task? _refreshTask;

        private bool _disposed;

        public Aria2MonitorService(IOptions<Aria2HostOptions> options, Aria2Host host, SimpleMessenger messenger, ILogger<Aria2MonitorService>? logger = null)
        {
            _options = options.Value;
            _host = host;
            _messenger = messenger;
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
                    var active = await _host.Connection.TellActiveAsync().ConfigureAwait(false);
                    var waiting = await _host.Connection.TellWaitingAsync(0, 10).ConfigureAwait(false);
                    var stopped = await _host.Connection.TellStoppedAsync(0, 10).ConfigureAwait(false);
                    _messenger.Post(active.Concat(waiting).Concat(stopped).ToArray());
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

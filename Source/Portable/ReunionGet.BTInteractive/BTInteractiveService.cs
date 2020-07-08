using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReunionGet.Aria2Rpc.Json.Responses;
using ReunionGet.Models;
using ReunionGet.Models.Aria2;

namespace ReunionGet.BTInteractive
{
    public class BTInteractiveService : IHostedService
    {
        private readonly string _magnetOrTorrent;
        private readonly Aria2Host _aria2Host;
        private readonly SimpleMessenger _messenger;
        private readonly ILogger<BTInteractiveService>? _logger;

        public BTInteractiveService(IOptions<BTInteractiveOptions> options, Aria2Host aria2Host, SimpleMessenger messenger, ILogger<BTInteractiveService>? logger = null)
        {
            _magnetOrTorrent = options.Value.MagnetOrTorrent ?? throw new InvalidOperationException("Misconfigured option.");
            _aria2Host = aria2Host;
            _messenger = messenger;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_magnetOrTorrent.StartsWith("magnet:", StringComparison.Ordinal))
            {
                _logger?.LogInformation("Adding magnet link: {0}", (string)_magnetOrTorrent);
                var gid = await _aria2Host.Connection.AddUriAsync((string)_magnetOrTorrent).ConfigureAwait(false);
                _logger?.LogInformation("Magnet task added with GID {0}", gid);
            }
            else
            {
                _logger?.LogInformation("Adding torrent file: {0}", (string)_magnetOrTorrent);
                using var stream = File.OpenRead(_magnetOrTorrent);
                var gid = await _aria2Host.Connection.AddTorrentAsync(stream).ConfigureAwait(false);
                _logger?.LogInformation("Torrent task added with GID {0}", gid);
            }

            _messenger.Updated += OnUpdated;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _messenger.Updated -= OnUpdated;
            return Task.CompletedTask;
        }

        private void OnUpdated(IReadOnlyList<DownloadProgressStatus> progresses)
        {
            foreach (var task in progresses)
            {
                Console.WriteLine("================");
                Console.WriteLine($"Status: {task.Status}");
                Console.WriteLine($"Download: {task.DownloadSpeed:N0} B/s Upload: {task.UploadSpeed:N0} B/s");
                Console.WriteLine("Files:");
                foreach (var file in task.Files!)
                {
                    Console.Write($"{file.CompletedLength:N0}B/{file.Length:N0}B\t\t");
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(file.Path);
                    Console.ResetColor();
                }
            }

            if (progresses.Count == 0)
                Console.WriteLine("All downloads completed.");
        }
    }

    public class BTInteractiveOptions
    {
        public string? MagnetOrTorrent { get; set; }
    }
}

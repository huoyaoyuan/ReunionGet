using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReunionGet.Aria2Rpc;
using ReunionGet.Aria2Rpc.Json;
using ReunionGet.Aria2Rpc.Json.Responses;
using ReunionGet.Models;
using ReunionGet.Models.Aria2;
using ReunionGet.Parser;

namespace ReunionGet.BTInteractive
{
    public class BTInteractiveService : IHostedService
    {
        private readonly string _magnetOrTorrent;
        private readonly Aria2Host _aria2Host;
        private readonly SimpleMessenger _messenger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<BTInteractiveService>? _logger;
        private Aria2GID _gid;

        public BTInteractiveService(
            IOptions<BTInteractiveOptions> options,
            Aria2Host aria2Host,
            SimpleMessenger messenger,
            IHostApplicationLifetime lifetime,
            ILogger<BTInteractiveService>? logger = null)
        {
            _magnetOrTorrent = options.Value.MagnetOrTorrent ?? throw new InvalidOperationException("Misconfigured option.");
            _aria2Host = aria2Host;
            _messenger = messenger;
            _lifetime = lifetime;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            async Task AddMagnetAsync(string magnet)
            {
                _logger?.LogInformation("Adding magnet link: {0}", magnet);
                _gid = await _aria2Host.Connection.AddUriAsync(magnet).ConfigureAwait(false);
                _logger?.LogInformation("Magnet task added with GID {0}", _gid);
                _state = State.Medadata;
            }

            async Task AddTorrentAsync(string torrentPath)
            {
                _logger?.LogInformation("Adding torrent file: {0}", torrentPath);
                using var stream = File.OpenRead(torrentPath);
                _gid = await _aria2Host.Connection.AddTorrentAsync(stream,
                    options: new Aria2Options
                    {
                        Pause = true
                    }).ConfigureAwait(false);
                _logger?.LogInformation("Torrent task added with GID {0}", _gid);
                _state = State.BeforeTorrent;
            }

            if (_magnetOrTorrent.StartsWith("magnet:", StringComparison.Ordinal))
            {
                if (Magnet.TryCreate(_magnetOrTorrent, out var magnet))
                {
                    string savedTorrent = magnet.HashValue.ToStringLower() + ".torrent";
                    if (File.Exists(savedTorrent))
                    {
                        _logger?.LogInformation("Loading saved torrent from {0}.", savedTorrent);
                        await AddTorrentAsync(savedTorrent).ConfigureAwait(false);
                    }
                    else
                    {
                        await AddMagnetAsync(_magnetOrTorrent).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await AddTorrentAsync(_magnetOrTorrent).ConfigureAwait(false);
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

            if (progresses.SingleOrDefault(t => t.Gid == _gid) is { } t)
            {
                switch (_state)
                {
                    case State.Medadata:
                        if (t.Status == DownloadStatus.Complete)
                        {
                            _gid = t.FollowedBy![0];
                            _state = State.BeforeTorrent;
                        }
                        break;

                    case State.BeforeTorrent:
                    {
                        Console.WriteLine("Listing torrent files:");
                        foreach (var file in t.Files!)
                        {
                            Console.Write($"{file.Index}. ");
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.Write(file.Path);
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.WriteLine($" {file.Length:N0}B");
                            Console.ResetColor();
                        }

                        Console.Write("Select file index(empty to select all):");
                        string? selected = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(selected))
                        {
                            _aria2Host.Connection.ChangeOptionAsync(_gid, new Aria2Options
                            {
                                SelectFile = selected
                            }).Wait();
                        }

                        _aria2Host.Connection.UnpauseAsync(_gid).Wait();
                        _state = State.Torrent;
                        break;
                    }

                    case State.Torrent:
                        if (t.Status == DownloadStatus.Complete)
                            _lifetime.StopApplication();
                        break;
                }
            }
        }

        private enum State
        {
            Medadata,
            BeforeTorrent,
            Torrent
        }

        private State _state;
    }

    public class BTInteractiveOptions
    {
        public string? MagnetOrTorrent { get; set; }
    }
}

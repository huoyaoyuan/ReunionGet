using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReunionGet.Aria2Rpc.Json.Responses;
using ReunionGet.Models;
using ReunionGet.Models.Aria2;
using ReunionGet.Parser;

namespace ReunionGet.BTInteractive
{
    public class BTInteractiveService : IHostedService
    {
        private readonly string _magnetOrTorrent;
        private readonly Aria2State _aria2State;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<BTInteractiveService>? _logger;

        private Aria2Task? _currentTask;

        public BTInteractiveService(
            IOptions<BTInteractiveOptions> options,
            Aria2State state,
            IHostApplicationLifetime lifetime,
            ILogger<BTInteractiveService>? logger = null)
        {
            _magnetOrTorrent = options.Value.MagnetOrTorrent ?? throw new InvalidOperationException("Misconfigured option.");
            _aria2State = state;
            _lifetime = lifetime;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            async Task AddMagnetAsync(string magnet)
            {
                _logger?.LogInformation("Adding magnet link: {0}", magnet);
                _currentTask = await _aria2State.AddMangetTaskAsync(magnet).ConfigureAwait(false);
                _logger?.LogInformation("Magnet task added with GID {0}", _currentTask.GID);
                _currentTask.StatusUpdated += OnMagnetUpdated;
            }

            async Task AddTorrentAsync(string torrentPath)
            {
                _logger?.LogInformation("Adding torrent file: {0}", torrentPath);
                using var stream = File.OpenRead(torrentPath);
                _currentTask = await _aria2State.AddTorrentTaskAsync(stream).ConfigureAwait(false);
                _logger?.LogInformation("Torrent task added with GID {0}", _currentTask.GID);
                _currentTask.StatusUpdated += InitTorrentTaskLoaded;
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
        }

        private void OnMagnetUpdated()
        {
            Debug.Assert(_currentTask != null);
            PrintStatus(_currentTask.RpcResponse!);

            if (_currentTask.RpcResponse!.Status == DownloadStatus.Complete)
            {
                _currentTask.StatusUpdated -= OnMagnetUpdated;

                using (_aria2State.TasksLock.UseReadLock())
                {
                    if (_currentTask.FollowedTasks.Count > 0)
                        PrepareTorrentTask(_currentTask.FollowedTasks[0]);
                    else
                        _currentTask.FollowedTaskAdded += PrepareTorrentTask;
                }
            }
        }

        private void PrepareTorrentTask(Aria2Task torrentTask)
        {
            Debug.Assert(_currentTask != null);
            _currentTask.FollowedTaskAdded -= PrepareTorrentTask;
            _currentTask = torrentTask;

            if (torrentTask.Loaded)
                InitTorrentTaskLoaded();
            else
                torrentTask.StatusUpdated += InitTorrentTaskLoaded;
        }

        private async void InitTorrentTaskLoaded()
        {
            Debug.Assert(_currentTask != null);
            Debug.Assert(_currentTask.Loaded);

            _currentTask.StatusUpdated -= InitTorrentTaskLoaded;

            Console.WriteLine("Listing torrent files:");
            foreach (var file in _currentTask.RpcResponse!.Files!)
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
                await _currentTask.SetFilesAsync(selected).ConfigureAwait(false);

            await _currentTask.UnpauseAsync().ConfigureAwait(false);
            _currentTask.StatusUpdated += PrintBeforeComplete;
        }

        private void PrintBeforeComplete()
        {
            Debug.Assert(_currentTask!.RpcResponse != null);
            PrintStatus(_currentTask.RpcResponse);

            if (_currentTask.RpcResponse.Status == DownloadStatus.Complete)
            {
                _currentTask.StatusUpdated -= PrintBeforeComplete;
                _lifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static void PrintStatus(DownloadProgressStatus task)
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
    }

    public class BTInteractiveOptions
    {
        public string? MagnetOrTorrent { get; set; }
    }
}

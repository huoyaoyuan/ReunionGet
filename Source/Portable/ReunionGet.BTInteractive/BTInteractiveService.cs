using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ReunionGet.Aria2Rpc.Json.Responses;
using ReunionGet.Models;

namespace ReunionGet.BTInteractive
{
    public class BTInteractiveService : IHostedService
    {
        private readonly string _magnetOrTorrent;
        private readonly SimpleMessenger _messenger;

        public BTInteractiveService(IOptions<BTInteractiveOptions> options, SimpleMessenger messenger)
        {
            _magnetOrTorrent = options.Value.MagnetOrTorrent ?? throw new InvalidOperationException("Misconfigured option.");
            _messenger = messenger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _messenger.Downloads.Add(_magnetOrTorrent, cancellationToken);
            _messenger.Updated += OnUpdated;
            return Task.CompletedTask;
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

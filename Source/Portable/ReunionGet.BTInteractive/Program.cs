using System;
using System.IO;
using System.Threading.Tasks;
using ReunionGet.Aria2Rpc.Json;
using ReunionGet.Aria2Rpc.Json.Responses;
using ReunionGet.Models.Aria2;
using ReunionGet.Parser;

#pragma warning disable CA2007 // Use ConfigureAwait

namespace ReunionGet.BTInteractive
{
    internal class Program
    {
        public static async Task Main(string? aria2Path, string? target)
        {
            bool canceled = false;
            Console.CancelKeyPress += (s, e) =>
            {
                canceled = true;
                e.Cancel = true;
            };

            string? cwd = null;

            if (aria2Path is null && target is null)
            {
                Console.Write("Storage path(use current working directory if empty):");
                cwd = Console.ReadLine();
            }

            if (string.IsNullOrWhiteSpace(cwd))
                cwd = ".";

            if (aria2Path is null)
            {
                Console.Write("Aria2 path(use current path if empty):");
                aria2Path = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(aria2Path))
                    aria2Path = "aria2c";
            }

            if (target is null)
            {
                Console.Write("Magnet or torrent path:");
                do
                    target = Console.ReadLine();
                while (string.IsNullOrWhiteSpace(target));
            }

            try
            {
                int port = new Random().Next(6000, 7000);
                await using var host = new Aria2Host(aria2Path, cwd, listeningPort: port);
                var version = await host.Connection.GetVersionAsync();
                Console.WriteLine($"aria2 started at port {port} with PID {host.ProcessId}");
                Console.WriteLine($"aria2 version: {version.Version}");

                Aria2GID gid;
                if (target.StartsWith("magnet:", StringComparison.Ordinal))
                {
                    var magnet = new Magnet(target);
                    Console.WriteLine($"SHA256 Hex hash: {magnet.HashValue}");
                    Console.WriteLine($"Display name: {magnet.DisplayName}");

                    gid = await host.Connection.AddUriAsync(target);
                }
                else
                {
                    using var torrentFile = File.OpenRead(target);
                    byte[] bytes = new byte[torrentFile.Length];
                    _ = await torrentFile.ReadAsync(bytes);

                    var torrent = new BitTorrent(bytes);
                    Console.WriteLine($"Display name: {torrent.Name}");
                    Console.WriteLine($"Info hash: {torrent.InfoHash}");

                    gid = await host.Connection.AddTorrentAsync(bytes);
                }

                while (true)
                {
                    if (canceled)
                        return;

                    var progress = await host.Connection.TellStatusAsync(gid);

                    Console.WriteLine($"Status: {progress.Status}");
                    Console.WriteLine($"Download: {progress.DownloadSpeed:N0} B/s Upload: {progress.UploadSpeed:N0} B/s");
                    Console.WriteLine("Files:");
                    foreach (var file in progress.Files!)
                    {
                        Console.Write($"{file.CompletedLength:N0}B/{file.Length:N0}B\t\t");
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine(file.Path);
                        Console.ResetColor();
                    }

                    if (progress.Status == DownloadStatus.Complete)
                    {
                        Console.WriteLine($"Download {gid} completed.");

                        if (progress.FollowedBy?.Count > 0)
                            gid = progress.FollowedBy[0];
                        else
                            return;
                    }

                    await Task.Delay(2000);
                }
            }
#pragma warning disable CA1031 // Don't catch general exception type
            catch (Exception ex)
#pragma warning restore CA1031 // Don't catch general exception type
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }
        }
    }
}

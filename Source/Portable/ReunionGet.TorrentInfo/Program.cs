using System;
using System.IO;
using ReunionGet.Parser;

namespace ReunionGet.TorrentInfo
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: TorrentInfo .torrent");
                return -1;
            }

            static string FormatSize(long size)
            {
                if ((size >> 10) == 0)
                    return $"{size} B";
                else if ((size >> 20) == 0)
                    return $"{TruncateTo3SD(size / (double)(1 << 10))} KB";
                else if ((size >> 30) == 0)
                    return $"{TruncateTo3SD(size / (double)(1 << 20))} MB";
                else if ((size >> 40) == 0)
                    return $"{TruncateTo3SD(size / (double)(1 << 30))} GB";
                else if ((size >> 50) == 0)
                    return $"{TruncateTo3SD(size / (double)(1 << 40))} TB";
                else
                    return $"{size >> 40} TB";
            }

            static double TruncateTo3SD(double value)
                => (int)Math.Log10(value) switch
                {
                    3 => Math.Truncate(value),
                    2 => Math.Truncate(value),
                    1 => Math.Truncate(value * 10) / 10,
                    0 => Math.Truncate(value * 100) / 100,
                    _ => throw new ArgumentOutOfRangeException(nameof(value), "Unexpected scaling. Expected 1-1024.")
                };

            try
            {
                var torrent = BitTorrent.FromFile(args[0]);

                Console.Write("Info hash: ");
                Console.WriteLine(torrent.InfoHash.ToString());

                if (torrent.Announce != null)
                {
                    Console.Write("Announce url: ");
                    Console.WriteLine(torrent.Announce.ToString());
                }

                Console.Write("Name: ");
                Console.WriteLine(torrent.Name);

                int bitCometPaddingFiles = 0;

                if (torrent.IsSingleFile)
                {
                    Console.WriteLine("Mode: single");
                    Console.Write("Length: ");
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(FormatSize(torrent.SingleFileLength!.Value));
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("Mode: folder");

                    foreach (var (length, path) in torrent.Files!)
                    {
                        if (path.StartsWith("_____padding_file_", StringComparison.Ordinal))
                        {
                            bitCometPaddingFiles++;
                            continue;
                        }

                        Console.Write(path);
                        Console.Write("  Size: ");
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine(FormatSize(length));
                        Console.ResetColor();
                    }
                }

                if (bitCometPaddingFiles > 0)
                    Console.WriteLine($"BitComet padding files hidden. (Total {bitCometPaddingFiles})");

                return 0;
            }
            catch (IOException)
            {
                Console.WriteLine($"Fail to read file {args[0]}.");
                return -1;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Cannot access file {args[0]}.");
                return -1;
            }
            catch (FormatException)
            {
                Console.WriteLine($"The file {args[0]} is not a valid torrent.");
                return -1;
            }
        }
    }
}

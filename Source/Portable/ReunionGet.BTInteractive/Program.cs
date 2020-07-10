using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReunionGet.Models;
using ReunionGet.Models.Aria2;

namespace ReunionGet.BTInteractive
{
    internal class Program
    {
        public static Task Main(string[] args)
        {
            return CreateHostBuilder(args)
                .RunConsoleAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) => services
                .AddSingleton<SimpleMessenger>()
                .AddSingleton<Aria2Host>()
                .AddHostedService<Aria2MonitorService>()
                .AddHostedService<BTInteractiveService>()
                .Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromMinutes(1))
                .Configure<Aria2HostOptions>(
                    context.Configuration.GetSection(Aria2HostOptions.SectionName))
                .Configure<BTInteractiveOptions>(o => GetDownloadTask(o, args))
                .PostConfigure<Aria2HostOptions>(AskFromCommandLine))
            .ConfigureAppConfiguration(configuration =>
                configuration.AddJsonFile("aria2options.json", optional: true));

        private static void AskFromCommandLine(Aria2HostOptions aria2)
        {
            if (aria2.ExecutablePath is null)
            {
                Console.Write("aria2 executable path(leave empty to use from PATH):");
                string? line = Console.ReadLine();
                if (!string.IsNullOrEmpty(line))
                    aria2.ExecutablePath = line;
            }

            if (aria2.WorkingDirectory is null)
            {
                Console.Write("Working directory(leave empty to use current directory):");
                string? line = Console.ReadLine();
                if (!string.IsNullOrEmpty(line))
                    aria2.WorkingDirectory = line;
            }
        }

        private static void GetDownloadTask(BTInteractiveOptions options, string[] args)
        {
            if (args.Length > 0)
                options.MagnetOrTorrent = args[0];
            else
            {
                Console.Write("Torrent path or magnet:");
                options.MagnetOrTorrent = Console.ReadLine();
            }
        }
    }
}

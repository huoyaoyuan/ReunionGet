using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using ReunionGet.Aria2Rpc;
using ReunionGet.Aria2Rpc.Json;

namespace ReunionGet.Models.Aria2
{
    public sealed class Aria2Host : IDisposable, IAsyncDisposable
    {
        private readonly Process _process;

        public int ProcessId => _process.Id;

        public Aria2Connection Connection { get; }

        public Aria2Host(string executablePath, string workingDirectory, string? token = null, int listeningPort = 6800)
        {
            if (token is null)
            {
                static string GenerateRandomString(int bytes)
                {
                    Span<byte> span = stackalloc byte[bytes];

                    var random = new Random();
                    random.NextBytes(span);

                    // TODO: Use Convert.ToHex
                    const string HexString = "0123456789abcdef";
                    Span<char> chars = stackalloc char[bytes * 2];
                    for (int i = 0; i < span.Length; i++)
                    {
                        chars[i * 2] = HexString[span[i] >> 4];
                        chars[i * 2 + 1] = HexString[span[i] & 0x0F];
                    }
                    return new string(chars);
                }
                token = GenerateRandomString(8);
            }

            var psi = new ProcessStartInfo(executablePath)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                ArgumentList =
                {
                    "--enable-rpc=true",
                    $"--rpc-listen-port={listeningPort}",
                    $"--rpc-secret={token}"
                }
            };

            _process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start aria2 process.");

            Connection = new Aria2Connection("localhost", listeningPort, token, shutdownOnDisposal: true);
        }

        public void Dispose()
        {
            _process.Dispose();
            Connection.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            _process.Dispose();
            return Connection.DisposeAsync();
        }

        /// <summary>
        /// Try shut down an earlier started instance.
        /// </summary>
        /// <param name="port">The port specified for the instance to listen.</param>
        /// <param name="token">The secret token specified for the instance.</param>
        /// <param name="processId">The process id of the instance.
        /// <see langword="null"/> if previous host fails to log its pid. </param>
        /// <returns>The awaitable task of shutting down.</returns>
        public static async ValueTask TryShutdownExistingInstanceAsync(int port, string token, int? processId)
        {
            if (processId is int id)
            {
                try
                {
                    using var process = Process.GetProcessById(id);
                    if (!process.HasExited)
                        process.Kill();
                }
                catch (ArgumentException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }
            else
            {
                try
                {
                    await using var connection = new Aria2Connection("localhost", port, token);
                    // Let dispose to shutdown it
                }
                catch (WebException)
                {
                }
                catch (JsonRpcException)
                {
                }
            }
        }

        private async void RefreshAsync()
        {
            async Task<bool> InitialConnectionAsync(int retries, int interval)
            {
                while (retries-- > 0)
                {
                    try
                    {
                        var version = await Connection.GetVersionAsync().ConfigureAwait(false);
                        return true;
                    }
                    catch (JsonRpcException) // RPC is configured wrong.
                    {
                        return false;
                    }
                    catch (WebException) // Maybe the process hasn't started
                    {
                    }

                    await Task.Delay(interval).ConfigureAwait(false);
                }

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
                try
                {
                    _ = await Connection.TellActiveAsync().ConfigureAwait(false);
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
                catch
#pragma warning restore CA1031 // Don't catch general exception type
                {
                    // should log
                    return;
                }

                Updated?.Invoke();
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        public bool IsFaulted { get; private set; }

        public event Action? Updated;
    }
}

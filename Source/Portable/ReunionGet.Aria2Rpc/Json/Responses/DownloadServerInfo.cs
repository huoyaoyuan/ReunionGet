using System.Collections.Generic;
using System.Text.Json.Serialization;

#pragma warning disable CA1054 // Uri parameter should not be string
#pragma warning disable CA1056 // Uri property should not be string

namespace ReunionGet.Aria2Rpc.Json.Responses
{
    public sealed class DownloadServerInfoOfFile
    {
        [JsonConstructor]
        public DownloadServerInfoOfFile(int index, IReadOnlyList<DownloadServerInfo> servers)
        {
            Index = index;
            Servers = servers;
        }

        /// <summary>
        /// Index of the file, starting at 1, in the same order as files appear in the multi-file metalink.
        /// </summary>
        public int Index { get; }

        public IReadOnlyList<DownloadServerInfo> Servers { get; }
    }

    public sealed class DownloadServerInfo
    {
        [JsonConstructor]
        public DownloadServerInfo(string uri, string currentUri, int downloadSpeed)
        {
            Uri = uri;
            CurrentUri = currentUri;
            DownloadSpeed = downloadSpeed;
        }

        /// <summary>
        /// Original URI.
        /// </summary>
        public string Uri { get; }

        /// <summary>
        /// This is the URI currently used for downloading.
        /// If redirection is involved, <see cref="CurrentUri"/> and <see cref="Uri"/> may differ.
        /// </summary>
        public string CurrentUri { get; }

        public int DownloadSpeed { get; }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using ReunionGet.Aria2Rpc.Json.Requests;
using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Aria2Rpc
{
    public partial class Aria2Connection
    {
        /// <summary>
        /// List the available RPC methods.
        /// </summary>
        /// <returns>Names of available methods.</returns>
        public Task<string[]> ListMethodsAsync() => DoRpcWithoutTokenAsync<string[]>("system.listMethods");

        /// <summary>
        /// List the available RPC notifications.
        /// </summary>
        /// <returns>Names of available notifications.</returns>
        public Task<string[]> ListNotificationsAsync() => DoRpcWithoutTokenAsync<string[]>("system.listNotifications");

        /// <summary>
        /// This method adds a new download.
        /// </summary>
        /// <param name="uris">An array of HTTP/FTP/SFTP/BitTorrent URIs (strings) pointing to the same resource.</param>
        /// <returns>The GID of the newly registered download.</returns>
        public Task<long> AddUriAsync(params string[] uris) => AddUriAsync(uris, null, null);

        /// <summary>
        /// Adds a new download.
        /// </summary>
        /// <remarks>
        /// If you mix URIs pointing to different resources, then the download may fail or be corrupted without aria2 complaining.
        /// When adding BitTorrent Magnet URIs, <paramref name="uris"/> must have only one element and it should be BitTorrent Magnet URI.
        /// </remarks>
        /// <param name="uris">An array of HTTP/FTP/SFTP/BitTorrent URIs (strings) pointing to the same resource.</param>
        /// <param name="options">Optional aria2 options.</param>
        /// <param name="position">The new download will be inserted at position in the waiting queue.</param>
        /// <returns>The GID of the newly registered download.</returns>
        public Task<long> AddUriAsync(string[] uris, Aria2Options? options = null, int? position = null)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be non-negative.");

            if (position != null)
                options ??= Aria2Options.Empty;

            return DoRpcAsync(new AddUriRequest
            {
                Uris = uris,
                Options = options,
                Position = position
            });
        }

        private static async ValueTask<byte[]> ReadToEndAsync(Stream stream)
        {
            byte[] content = new byte[stream.Length - stream.Position];
            _ = await stream.ReadAsync(content).ConfigureAwait(false);
            return content;
        }

        /// <summary>
        /// Adds a BitTorrent download by uploading a ".torrent" file.
        /// </summary>
        /// <remarks>
        /// If you want to add a BitTorrent Magnet URI, use the <see cref="AddUriAsync(string[], Aria2Options?, int?)"/> method instead.
        /// </remarks>
        /// <param name="torrent">Contents of the ".torrent" file.</param>
        /// <param name="uris">An array of URIs (string) used for Web-seeding.
        /// <list type="bullet">
        /// <item>For single file torrents, the URI can be a complete URI pointing to the resource; if URI ends with /, name in torrent file is added.</item>
        /// <item>For multi-file torrents, name and path in torrent are added to form a URI for each file.</item>
        /// </list>
        /// </param>
        /// <param name="options">Optional aria2 options.</param>
        /// <param name="position">The new download will be inserted at position in the waiting queue.</param>
        /// <returns>The GID of the newly registered download.</returns>
        public Task<long> AddTorrentAsync(ReadOnlyMemory<byte> torrent, string[]? uris = null, Aria2Options? options = null, int? position = null)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be non-negative.");

            if (position != null)
                options ??= Aria2Options.Empty;

            if (options != null)
                uris ??= Array.Empty<string>();

            return DoRpcAsync(new AddTorrentRequest
            {
                Torrent = torrent,
                Uris = uris,
                Options = options,
                Position = position
            });
        }

        /// <summary>
        /// Adds a BitTorrent download by uploading a ".torrent" file.
        /// </summary>
        /// <remarks>
        /// If you want to add a BitTorrent Magnet URI, use the <see cref="AddUriAsync(string[], Aria2Options?, int?)"/> method instead.
        /// </remarks>
        /// <param name="torrent">Contents of the ".torrent" file.</param>
        /// <param name="uris">An array of URIs (string) used for Web-seeding.
        /// <list type="bullet">
        /// <item>For single file torrents, the URI can be a complete URI pointing to the resource; if URI ends with /, name in torrent file is added.</item>
        /// <item>For multi-file torrents, name and path in torrent are added to form a URI for each file.</item>
        /// </list>
        /// </param>
        /// <param name="options">Optional aria2 options.</param>
        /// <param name="position">The new download will be inserted at position in the waiting queue.</param>
        /// <returns>The GID of the newly registered download.</returns>
        public async Task<long> AddTorrentAsync(Stream torrent, string[]? uris = null, Aria2Options? options = null, int? position = null)
            => await AddTorrentAsync(await ReadToEndAsync(torrent).ConfigureAwait(false), uris, options, position)
            .ConfigureAwait(false);

        /// <summary>
        /// Adds a Metalink download by uploading a ".metalink" file.
        /// </summary>
        /// <param name="metalink">Contents of the ".metalink" file.</param>
        /// <param name="options">Optional aria2 options.</param>
        /// <param name="position">The new download will be inserted at position in the waiting queue.</param>
        /// <returns>An array GIDs of the newly registered downloads.</returns>
        public Task<long[]> AddMetalinkAsync(ReadOnlyMemory<byte> metalink, Aria2Options? options = null, int? position = null)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be non-negative.");

            if (position != null)
                options ??= Aria2Options.Empty;

            return DoRpcAsync(new AddMetalinkRequest
            {
                Metalink = metalink,
                Options = options,
                Position = position
            });
        }

        /// <summary>
        /// Adds a Metalink download by uploading a ".metalink" file.
        /// </summary>
        /// <param name="metalink">Contents of the ".metalink" file.</param>
        /// <param name="options">Optional aria2 options.</param>
        /// <param name="position">The new download will be inserted at position in the waiting queue.</param>
        /// <returns>An array GIDs of the newly registered downloads.</returns>
        public async Task<long[]> AddMetalinkAsync(Stream metalink, Aria2Options? options = null, int? position = null)
            => await AddMetalinkAsync(await ReadToEndAsync(metalink).ConfigureAwait(false), options, position)
            .ConfigureAwait(false);

        /// <summary>
        /// Removes the download denoted by <paramref name="gid"/>.
        /// </summary>
        /// <remarks>
        /// If the specified download is in progress, it is first stopped.
        /// The status of the removed download becomes <see cref="DownloadStatus.Removed"/>.
        /// </remarks>
        /// <param name="gid">The gid of download to remove.</param>
        /// <returns>GID of removed download.</returns>
        public Task<long> RemoveAsync(long gid)
            => DoRpcAsync(new RemoveRequest
            {
                Gid = gid
            });

        /// <summary>
        /// Force removes the download denoted by <paramref name="gid"/>.
        /// </summary>
        /// <remarks>
        /// This method behaves just like <see cref="RemoveAsync(long)"/>
        /// except that this method removes the download without performing any actions which take time.
        /// </remarks>
        /// <param name="gid">The gid of download to remove.</param>
        /// <returns>GID of removed download.</returns>
        /// <seealso cref="RemoveAsync(long)"/>
        public Task<long> ForceRemoveAsync(long gid)
            => DoRpcAsync(new ForceRemoveRequest
            {
                Gid = gid
            });

        /// <summary>
        /// Pauses the download denoted by <paramref name="gid"/>.
        /// </summary>
        /// <remarks>
        /// The status of paused download becomes <see cref="DownloadStatus.Paused"/>.
        /// If the download was active, the download is placed in the front of waiting queue.
        /// While the status is <see cref="DownloadStatus.Paused"/>, the download is not started.
        /// To change status to <see cref="DownloadStatus.Waiting"/>, use the <see cref="UnpauseAsync(long)"/> method.
        /// </remarks>
        /// <param name="gid">The gid of download to pause.</param>
        /// <returns>GID of paused download.</returns>
        public Task<long> PauseAsync(long gid)
            => DoRpcAsync(new PauseRequest
            {
                Gid = gid
            });

        /// <summary>
        /// Pauses every active/waiting download.
        /// </summary>
        /// <returns>OK(<see langword="true"/>) if success.</returns>
        public Task<bool> PauseAllAsync()
            => DoRpcAsync(new PauseAllRequest());

        /// <summary>
        /// Force pauses the download denoted by <paramref name="gid"/>.
        /// </summary>
        /// <remarks>
        /// This method behaves just like <see cref="PauseAsync(long)"/>
        /// except that this method pauses the download without performing any actions which take time.
        /// </remarks>
        /// <param name="gid">The gid of download to pause.</param>
        /// <returns>GID of paused download.</returns>
        /// <seealso cref="PauseAsync(long)"/>
        public Task<long> ForcePauseAsync(long gid)
            => DoRpcAsync(new ForcePauseRequest
            {
                Gid = gid
            });

        /// <summary>
        /// Force pauses every active/waiting download.
        /// </summary>
        /// <remarks>
        /// This method behaves just like <see cref="PauseAllAsync"/>
        /// except that this method pauses the download without performing any actions which take time.
        /// </remarks>
        /// <returns>OK(<see langword="true"/>) if success.</returns>
        /// <seealso cref="PauseAllAsync"/>
        public Task<bool> ForcePauseAllAsync()
            => DoRpcAsync(new ForcePauseAllRequest());

        /// <summary>
        /// Unpauses the download denoted by <paramref name="gid"/>.
        /// </summary>
        /// <remarks>
        /// This method changes the status of the download denoted by <paramref name="gid"/>
        /// from <see cref="DownloadStatus.Paused"/> to <see cref="DownloadStatus.Waiting"/>,
        /// making the download eligible to be restarted. 
        /// </remarks>
        /// <param name="gid">The gid of download to unpause.</param>
        /// <returns>GID of unpaused download.</returns>
        public Task<long> UnpauseAsync(long gid)
            => DoRpcAsync(new UnpauseRequest
            {
                Gid = gid
            });

        /// <summary>
        /// Unpauses every paused download.
        /// </summary>
        /// <returns>OK(<see langword="true"/>) if success.</returns>
        public Task<bool> UnpauseAllAsync()
            => DoRpcAsync(new UnpauseAllRequest());

        /// <summary>
        /// Gets the progress of the download denoted by <paramref name="gid"/>.
        /// </summary>
        /// <param name="gid">The GID of the download.</param>
        /// <param name="keys">Limits keys of information to query.
        /// If null or empty, the response will contain all keys.</param>
        /// <returns>The status of queried download.</returns>
        public Task<DownloadProgressStatus> TellStatusAsync(long gid, string[]? keys = null)
            => DoRpcAsync(new TellStatusRequest
            {
                Gid = gid,
                Keys = keys
            });

        /// <summary>
        /// Gets the URIs used in the download denoted by <paramref name="gid"/>.
        /// </summary>
        /// <param name="gid">The GID of the download.</param>
        /// <returns>Uris used by the download. Corresponding to uris in <see cref="AddUriAsync(string[])"/>.</returns>
        public Task<FileDownloadUriInfo[]> GetUrisAsync(long gid)
            => DoRpcAsync(new GetUrisRequest
            {
                Gid = gid
            });

        /// <summary>
        /// Gets the files in the download denoted by <paramref name="gid"/>.
        /// </summary>
        /// <param name="gid">The GID of the download.</param>
        /// <returns>Files and status in the download.</returns>
        public Task<DownloadFileStatus[]> GetFilesAsync(long gid)
            => DoRpcAsync(new GetFilesRequest
            {
                Gid = gid
            });

        /// <summary>
        /// Gets a list peers of the download denoted by <paramref name="gid"/>.
        /// </summary>
        /// <remarks>
        /// This method is for BitTorrent only.
        /// </remarks>
        /// <param name="gid">The GID of the download.</param>
        /// <returns>Peers of the download.</returns>
        public Task<PeerInfo[]> GetPeersAsync(long gid)
            => DoRpcAsync(new GetPeersRequest
            {
                Gid = gid
            });

        /// <summary>
        /// Gets currently connected HTTP(S)/FTP/SFTP servers of the download denoted by <paramref name="gid"/>.
        /// </summary>
        /// <param name="gid">The GID of the download.</param>
        /// <returns>Connected servers of the download.</returns>
        public Task<DownloadServerInfoOfFile[]> GetServersAsync(long gid)
            => DoRpcAsync(new GetServersRequest
            {
                Gid = gid
            });

        /// <summary>
        /// Gets the progress of all active downloads.
        /// </summary>
        /// <param name="keys">Limits keys of information to query.
        /// If null or empty, the response will contain all keys.</param>
        /// <returns>Progress of all active downloads.</returns>
        /// <seealso cref="TellStatusAsync(long, string[]?)"/>
        public Task<DownloadProgressStatus[]> TellActiveAsync(string[]? keys = null)
            => DoRpcAsync(new TellActiveRequest
            {
                Keys = keys
            });

        /// <summary>
        /// Gets status of a list of waiting downloads, including paused ones.
        /// </summary>
        /// <remarks>
        /// If <paramref name="offset"/> is negative, counts from the end of the queue,
        /// and the order of result is reversed.
        /// </remarks>
        /// <param name="offset">Specifies the offset from the download waiting at the front.</param>
        /// <param name="num">Specifies the max number of downloads to be returned.</param>
        /// <param name="keys">Limits keys of information to query.
        /// If null or empty, the response will contain all keys.</param>
        /// <returns>Progress of paused downloads in the range.</returns>
        /// <seealso cref="TellStatusAsync(long, string[]?)"/>
        public Task<DownloadProgressStatus[]> TellWaitingAsync(int offset, int num, string[]? keys = null)
        {
            if (num < 0)
                throw new ArgumentOutOfRangeException(nameof(num), "num must be positive");

            return DoRpcAsync(new TellWaitingRequest
            {
                Offset = offset,
                Num = num,
                Keys = keys
            });
        }

        /// <summary>
        /// Gets status of a list of stopped downloads.
        /// </summary>
        /// <remarks>
        /// If <paramref name="offset"/> is negative, counts from the end of the queue,
        /// and the order of result is reversed.
        /// </remarks>
        /// <param name="offset">Specifies the offset from the download least recently stopped download.</param>
        /// <param name="num">Specifies the max number of downloads to be returned.</param>
        /// <param name="keys">Limits keys of information to query.
        /// If null or empty, the response will contain all keys.</param>
        /// <returns>Progress of stopped downloads in the range.</returns>
        /// <seealso cref="TellStatusAsync(long, string[]?)"/>
        public Task<DownloadProgressStatus[]> TellStoppedAsync(int offset, int num, string[]? keys = null)
        {
            if (num < 0)
                throw new ArgumentOutOfRangeException(nameof(num), "num must be positive");

            return DoRpcAsync(new TellWaitingRequest
            {
                Offset = offset,
                Num = num,
                Keys = keys
            });
        }

        /// <summary>
        /// Changes the position of the download denoted by <paramref name="gid"/> in the queue.
        /// </summary>
        /// <param name="gid">The GID of the download.</param>
        /// <param name="pos">The relative position to change.</param>
        /// <param name="how">The origin of the move.</param>
        /// <returns>The resulting position.</returns>
        public Task<int> ChangePositionAsync(long gid, int pos, ChangePositionOrigin how)
            => DoRpcAsync(new ChangePositionRequest
            {
                Gid = gid,
                Pos = pos,
                How = how
            });

        /// <summary>
        /// Change uris of the download denoted by <paramref name="gid"/>.
        /// </summary>
        /// <remarks>
        /// The method first executes deletion then addition when calculating <paramref name="position"/>.
        /// </remarks>
        /// <param name="gid">The GID of the download.</param>
        /// <param name="fileIndex">1-based index to select the file in the download.</param>
        /// <param name="delUris">The uris to remove from the list.</param>
        /// <param name="addUris">The uris to add into the list.</param>
        /// <param name="position">The position new uris to add to.
        /// If <see langword="null"/>, appended at the end.</param>
        /// <returns></returns>
        public Task<(int numDeleted, int numAdded)> ChangeUriAsync(
            long gid, int fileIndex = 0,
            string[]? delUris = null, string[]? addUris = null,
            int? position = null)
            => DoRpcAsync(new ChangeUriRequest
            {
                Gid = gid,
                FileIndex = fileIndex,
                DelUris = delUris ?? Array.Empty<string>(),
                AddUris = addUris ?? Array.Empty<string>(),
                Position = position
            });

        /// <summary>
        /// Gets global statistics such as the overall download and upload speeds.
        /// </summary>
        /// <returns>The global statistics.</returns>
        public Task<GlobalStat> GetGlobalStatAsync()
            => DoRpcAsync(new GetGlobalStatRequest());

        /// <summary>
        /// Purges completed/error/removed downloads to free memory. 
        /// </summary>
        /// <returns>OK(<see langword="true"/>) if success.</returns>
        public Task<bool> PurgeDownloadResultAsync()
            => DoRpcAsync(new PurgeDownloadRequest());

        /// <summary>
        /// Removes a completed/error/removed download denoted by <paramref name="gid"/> from memory. 
        /// </summary>
        /// <param name="gid">The GID of download to remove.</param>
        /// <returns>OK(<see langword="true"/>) if success.</returns>
        public Task<bool> RemoveDownloadResultAsync(long gid)
            => DoRpcAsync(new RemoveDownloadRequest
            {
                Gid = gid
            });

        /// <summary>
        /// Gets the version of aria2 and the list of enabled features.
        /// </summary>
        /// <returns>Version info.</returns>
        public Task<Aria2VersionInfo> GetVersionAsync()
            => DoRpcAsync(new GetVersionRequest());
    }
}

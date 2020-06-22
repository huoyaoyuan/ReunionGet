using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ReunionGet.Aria2Rpc.Json.Converters;

#pragma warning disable CA1054 // Uri parameter should not be string
#pragma warning disable CA1056 // Uri property should not be string

namespace ReunionGet.Aria2Rpc.Json.Responses
{
    public sealed class DownloadProgressStatus
    {
        [JsonConstructor]
        public DownloadProgressStatus(
            long gid, DownloadStatus status, long totalLength, long completedLength, long uploadedLength,
            BitArray? bitfield, int downloadSpeed, int uploadSpeed, string? infoHash, int numSeeders, bool seeder,
            long pieceLength, int numPieces, int connections, int errorCode, string? errorMessage,
            IReadOnlyList<long>? followedBy, long? following, long? belongsTo, string? dir,
            IReadOnlyList<DownloadFileStatus>? files, BitTorrentDownloadInfo? bitTorrent, long? verifiedLength,
            bool verifyIntegrityPending)
        {
            Gid = gid;
            Status = status;
            TotalLength = totalLength;
            CompletedLength = completedLength;
            UploadedLength = uploadedLength;
            Bitfield = bitfield;
            DownloadSpeed = downloadSpeed;
            UploadSpeed = uploadSpeed;
            InfoHash = infoHash;
            NumSeeders = numSeeders;
            Seeder = seeder;
            PieceLength = pieceLength;
            NumPieces = numPieces;
            Connections = connections;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            FollowedBy = followedBy;
            Following = following;
            BelongsTo = belongsTo;
            Dir = dir;
            Files = files;
            BitTorrent = bitTorrent;
            VerifiedLength = verifiedLength;
            VerifyIntegrityPending = verifyIntegrityPending;
        }

        public long Gid { get; }

        public DownloadStatus Status { get; }

        public long TotalLength { get; }

        public long CompletedLength { get; }

        public long UploadedLength { get; }

        /// <summary>
        /// Hexadecimal representation of the download progress.
        /// </summary>
        /// <remarks>
        /// The highest bit corresponds to the piece at index 0.
        /// Any set bits indicate loaded pieces, while unset bits indicate not yet loaded and/or missing pieces.
        /// Any overflow bits at the end are set to zero.
        /// When the download was not started yet, this key will not be included in the response.
        /// </remarks>
        [JsonConverter(typeof(HexBitArrayConverter))]
        public BitArray? Bitfield { get; }

        /// <summary>
        /// Download speed of this download measured in bytes/sec.
        /// </summary>
        public int DownloadSpeed { get; }

        /// <summary>
        /// Upload speed of this download measured in bytes/sec.
        /// </summary>
        public int UploadSpeed { get; }

        public string? InfoHash { get; }

        public int NumSeeders { get; }

        public bool Seeder { get; }

        public long PieceLength { get; }

        public int NumPieces { get; }

        public int Connections { get; }

        public int ErrorCode { get; }

        public string? ErrorMessage { get; }

        /// <summary>
        /// List of GIDs which are generated as the result of this download.
        /// </summary>
        /// <remarks>
        /// For example, when aria2 downloads a Metalink file, it generates downloads described in the Metalink
        /// (see the --follow-metalink option).
        /// This value is useful to track auto-generated downloads.
        /// If there are no such downloads, this key will not be included in the response.
        /// </remarks>
        public IReadOnlyList<long>? FollowedBy { get; }

        /// <summary>
        /// The reverse link for <see cref="FollowedBy"/>.
        /// A download included in <see cref="FollowedBy"/> has this object's GID in its <see cref="Following"/> value.
        /// </summary>
        public long? Following { get; }

        /// <summary>
        /// GID of a parent download.
        /// </summary>
        /// <remarks>
        /// Some downloads are a part of another download.
        /// For example, if a file in a Metalink has BitTorrent resources, the downloads of ".torrent" files are parts of that parent.
        /// If this download has no parent, this key will not be included in the response.
        /// </remarks>
        public long? BelongsTo { get; }

        public string? Dir { get; }

        public IReadOnlyList<DownloadFileStatus>? Files { get; }

        public BitTorrentDownloadInfo? BitTorrent { get; }

        public long? VerifiedLength { get; }

        public bool VerifyIntegrityPending { get; }
    }

    public sealed class DownloadFileStatus
    {
        public DownloadFileStatus(
            int index, string path, long length, long completedLength, bool selected,
            IReadOnlyList<FileDownloadUriInfo> uris)
        {
            Index = index;
            Path = path;
            Length = length;
            CompletedLength = completedLength;
            Selected = selected;
            Uris = uris;
        }

        /// <summary>
        /// Index of the file, starting at 1, in the same order as files appear in the multi-file torrent.
        /// </summary>
        public int Index { get; }

        public string Path { get; }

        public long Length { get; }

        /// <summary>
        /// Completed length of this file in bytes.
        /// </summary>
        /// <remarks>
        /// Please note that it is possible that sum of <see cref="CompletedLength"/> is less than <see cref="DownloadProgressStatus.CompletedLength"/>.
        /// This is because <see cref="CompletedLength"/> only includes completed pieces.
        /// On the other hand, <see cref="DownloadProgressStatus.CompletedLength"/> also includes partially completed pieces.
        /// </remarks>
        public long CompletedLength { get; }

        public bool Selected { get; }

        public IReadOnlyList<FileDownloadUriInfo> Uris { get; }
    }

    public sealed class FileDownloadUriInfo
    {
        [JsonConstructor]
        public FileDownloadUriInfo(string uri, FileDownloadUriStatus status)
        {
            Uri = uri;
            Status = status;
        }

        public string Uri { get; }

        public FileDownloadUriStatus Status { get; }
    }

    public sealed class BitTorrentDownloadInfo
    {
        [JsonConstructor]
        public BitTorrentDownloadInfo(
            IReadOnlyList<string> announceList,
            string? comment,
            DateTimeOffset creationDate,
            BitTorrentMode mode,
            object? info)
        {
            AnnounceList = announceList;
            Comment = comment;
            CreationDate = creationDate;
            Mode = mode;
            Info = info;
        }

        public IReadOnlyList<string> AnnounceList { get; }

        public string? Comment { get; }

        [JsonConverter(typeof(DateTimeOffsetSecondsConverter))]
        public DateTimeOffset CreationDate { get; }

        public BitTorrentMode Mode { get; }

        public object? Info { get; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DownloadStatus
    {
        Unknown,
        Active,
        Waiting,
        Paused,
        Error,
        Complete,
        Removed
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BitTorrentMode
    {
        Unknown,
#pragma warning disable CA1720 // Identifier contains type name
        Single,
#pragma warning restore CA1720 // Identifier contains type name
        Multi
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FileDownloadUriStatus
    {
        Waiting,
        Used
    }
}

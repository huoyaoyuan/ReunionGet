﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ReunionGet.Parser
{
    /// <summary>
    /// A parsed bittorrent file.
    /// </summary>
    public class BitTorrent
    {
        // TODO: use MemberNotNullWhen and is not null

        #region Standard Specified Members
        public Uri? Announce { get; }

        public string Name { get; }

        public long PieceLength { get; }

        /// <summary>
        /// Hash of each piece of files of this torrrent in <see cref="SHA1"/>.
        /// </summary>
        public ImmutableArray<HashBlock> PieceHashes { get; }

        public long? SingleFileLength { get; }

        public IReadOnlyList<(long length, string path)>? Files { get; }

        public IEnumerable<string> FilePathes
            => Files?.Select(x => x.path)
            ?? Enumerable.Empty<string>();

        public long TotalLength => IsSingleFile
            ? SingleFileLength!.Value
            : Files!.Sum(f => f.length);

        public bool IsSingleFile => SingleFileLength != null;

        public bool IsPrivate { get; }

        /// <summary>
        /// Hash of the info section of this torrrent in <see cref="SHA1"/>.
        /// </summary>
        public HashBlock InfoHash { get; }
        #endregion

        #region Additional Members
        public string? Comment { get; }

        public IReadOnlyList<Uri>? AnnounceList { get; }

        public string? CreatedBy { get; }

        public DateTimeOffset? CreationTime { get; }
        #endregion

        public BitTorrent(ReadOnlySpan<byte> content)
        {
            string? name = null;

            try
            {
                var reader = new BEncodingReader(content);

                reader.ReadDictStart();
                while (!reader.TryReadListDictEnd())
                    switch (reader.ReadString())
                    {
                        case "announce":
                            Announce = new Uri(reader.ReadString());
                            break;

                        case "info":
                        {
                            int infoStart = reader.BytesConsumed;

                            reader.ReadDictStart();
                            while (!reader.TryReadListDictEnd())
                                switch (reader.ReadString())
                                {
                                    case "name":
                                        name = reader.ReadString();
                                        break;

                                    case "piece length":
                                        PieceLength = reader.ReadInt64();
                                        break;

                                    case "pieces":
                                    {
                                        var span = reader.ReadBytes();
                                        var builder = ImmutableArray.CreateBuilder<HashBlock>(span.Length / 20);

                                        while (!span.IsEmpty)
                                        {
                                            builder.Add(new HashBlock(span[0..20].ToArray()));
                                            span = span.Slice(20);
                                        }

                                        PieceHashes = builder.MoveToImmutable();
                                        break;
                                    }

                                    case "length":
                                        SingleFileLength = reader.ReadInt64();
                                        break;

                                    case "files":
                                    {
                                        reader.ReadListStart();
                                        var list = new List<(long length, string path)>();
                                        while (!reader.TryReadListDictEnd())
                                        {
                                            long length = 0;
                                            string? path = null;

                                            reader.ReadDictStart();
                                            while (!reader.TryReadListDictEnd())
                                                switch (reader.ReadString())
                                                {
                                                    case "length":
                                                        length = reader.ReadInt64();
                                                        break;

                                                    case "path":
                                                    {
                                                        reader.ReadListStart();

                                                        var pathParts = new List<string>();
                                                        while (!reader.TryReadListDictEnd())
                                                            pathParts.Add(reader.ReadString());

                                                        path = string.Join('/', pathParts);
                                                        break;
                                                    }

                                                    default:
                                                        reader.SkipValue();
                                                        break;
                                                }

                                            list.Add((length, path ?? throw new FormatException("File has no path specified.")));
                                        }
                                        Files = list;
                                        break;
                                    }

                                    case "private":
                                        if (reader.ReadInt32() == 1)
                                            IsPrivate = true;
                                        break;

                                    default:
                                        reader.SkipValue();
                                        break;
                                }

                            int infoEnd = reader.BytesConsumed;
                            var infoSpan = content[infoStart..infoEnd];
                            using (var sha = SHA1.Create())
                            {
                                byte[] shaSpan = new byte[20];
                                if (!sha.TryComputeHash(infoSpan, shaSpan, out int bw) || bw != 20)
                                    throw new InvalidOperationException("Failed to compute SHA1 hash.");

                                InfoHash = new HashBlock(shaSpan);
                            }

                            break;
                        }

                        case "comment":
                            Comment = reader.ReadString();
                            break;

                        case "announce-list":
                        {
                            var list = new List<Uri>();

                            reader.ReadListStart();
                            while (!reader.TryReadListDictEnd())
                            {
                                reader.ReadListStart();
                                while (!reader.TryReadListDictEnd())
                                    list.Add(new Uri(reader.ReadString()));
                            }

                            AnnounceList = list;
                            break;
                        }

                        case "creation date":
                        {
                            if (reader.TryReadInt64(out long date))
                                CreationTime = DateTimeOffset.FromUnixTimeSeconds(date);
                            else if (reader.TryReadString(out string? str)
                                && long.TryParse(str, out long strDate))
                                CreationTime = DateTimeOffset.FromUnixTimeSeconds(strDate);
                            else
                                reader.SkipValue();
                            break;
                        }

                        case "created by":
                            CreatedBy = reader.ReadString();
                            break;

                        default:
                            reader.SkipValue();
                            break;
                    }

                if (!reader.Ends())
                    throw new FormatException("Unexpected content after ending.");
            }
            catch (Exception ex)
            {
                throw new FormatException("Bad formatted torrent content.", ex);
            }

            // Sanity check
            Name = name ?? throw new FormatException("The torrent doesn't have name part.");

            if (Files != null && SingleFileLength != null
                || Files is null && SingleFileLength is null)
                throw new FormatException("The torrent must have exactly either of files and length.");

            if (SingleFileLength < 0
                || Files?.Any(f => f.length < 0) == true)
                throw new FormatException("Negative length detected.");

            if ((TotalLength + PieceLength - 1) / PieceLength != PieceHashes.Length)
                throw new FormatException("Size mismatch between hash and file length.");
        }

        public static BitTorrent FromStream(Stream stream)
        {
            int length = (int)(stream.Length - stream.Position);
            byte[] buffer = new byte[length];
            _ = stream.Read(buffer);
            return new BitTorrent(buffer);
        }

        public static async ValueTask<BitTorrent> FromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            int length = (int)(stream.Length - stream.Position);
            byte[] buffer = new byte[length];
            _ = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            return new BitTorrent(buffer);
        }

        public static BitTorrent FromFile(string path)
        {
            using var stream = File.OpenRead(path);
            return FromStream(stream);
        }

        public static ValueTask<BitTorrent> FromFileAsync(string path, CancellationToken cancellationToken = default)
        {
            using var stream = File.OpenRead(path);
            return FromStreamAsync(stream, cancellationToken);
        }

        public Magnet ToMagnet()
            => new Magnet(MagnetHashAlgorithm.BTIH,
                InfoHash,
                displayName: Name,
                trackers: AnnounceList ?? (Announce is null ? Array.Empty<Uri>() : new[] { Announce }),
                exactLength: SingleFileLength);
    }
}

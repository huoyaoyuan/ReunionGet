using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReunionGet.Parser
{
    public class BitTorrent
    {
        // TODO: use MemberNotNullWhen and is not null

        public Uri Announce { get; }

        public string Name { get; }

        public long PieceLength { get; }

        public ImmutableArray<Sha1Hash> PieceHashes { get; }

        public long? TotalLength { get; }

        public IReadOnlyList<(long length, string path)>? Files { get; }

        public IEnumerable<string> FilePathes
            => Files?.Select(x => x.path)
            ?? Enumerable.Empty<string>();

        public bool IsSingleFile => TotalLength != null;

        public readonly struct Sha1Hash : IEquatable<Sha1Hash>
        {
            private readonly byte[] _hash;

            public ReadOnlySpan<byte> Hash => _hash ?? throw new InvalidOperationException($"Using uninitialized {nameof(Sha1Hash)} instance.");

            internal Sha1Hash(ReadOnlySpan<byte> byteSpan)
            {
                if (byteSpan.Length != 20)
                    throw new ArgumentException("Bad hash size.", nameof(byteSpan));

                _hash = byteSpan.ToArray();
            }

            public override bool Equals(object? obj) => obj is Sha1Hash hash && Equals(hash);
            public bool Equals(Sha1Hash other) => Hash.SequenceEqual(other.Hash);
            public override int GetHashCode()
            {
                HashCode hash = default;
                foreach (byte b in Hash)
                    hash.Add(b);
                return hash.ToHashCode();
            }

            public static bool operator ==(Sha1Hash left, Sha1Hash right) => left.Equals(right);
            public static bool operator !=(Sha1Hash left, Sha1Hash right) => !(left == right);

            public override string ToString()
                => _hash is null
                ? string.Empty
                : string.Create(40, _hash, (span, array) =>
                {
                    for (int i = 0; i < array.Length; i++)
                        _ = array[i].TryFormat(span.Slice(i * 2), out _, "X2");
                });
        }

        public BitTorrent(ReadOnlySpan<byte> content)
        {
            string? announce = null;
            string? name = null;

            try
            {
                var reader = new BEncodingReader(content);

                reader.ReadDictStart();
                while (!reader.TryReadListDictEnd())
                    switch (reader.ReadString())
                    {
                        case "announce":
                            announce = reader.ReadString();
                            break;

                        case "info":
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
                                        var builder = ImmutableArray.CreateBuilder<Sha1Hash>(span.Length / 20);

                                        while (!span.IsEmpty)
                                        {
                                            builder.Add(new Sha1Hash(span[0..20]));
                                            span = span.Slice(20);
                                        }

                                        PieceHashes = builder.MoveToImmutable();
                                        break;
                                    }

                                    case "length":
                                        TotalLength = reader.ReadInt64();
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
                                                        path = reader.ReadString();
                                                        break;

                                                    default:
                                                        reader.SkipValue();
                                                        break;
                                                }

                                            list.Add((length, path ?? throw new FormatException("File has no path specified.")));
                                        }
                                        Files = list;
                                        break;
                                    }

                                    default:
                                        reader.SkipValue();
                                        break;
                                }
                            break;

                        default:
                            reader.SkipValue();
                            break;
                    }
            }
            catch (Exception ex)
            {
                throw new FormatException("Bad formatted torrent content.", ex);
            }

            // Sanity check
            Announce = new Uri(announce ?? throw new FormatException("The torrent doesn't have annouce part."));
            Name = name ?? throw new FormatException("The torrent doesn't have name part.");

            if (Files != null && TotalLength != null
                || Files is null && TotalLength is null)
                throw new FormatException("The torrent must have exactly either of files and length.");

            if (TotalLength < 0
                || Files?.Any(f => f.length < 0) == true)
                throw new FormatException("Negative length detected.");
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

        public static BitTorrent FromFile(string path) => FromStream(File.OpenRead(path));

        public static ValueTask<BitTorrent> FromFileAsync(string path, CancellationToken cancellationToken = default)
            => FromStreamAsync(File.OpenRead(path), cancellationToken);
    }
}

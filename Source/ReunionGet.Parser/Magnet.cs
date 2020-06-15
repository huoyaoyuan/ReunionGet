using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Web;

namespace ReunionGet.Parser
{
    public class Magnet : IEquatable<Magnet?>
    {
        public MagnetHashAlgorithm HashAlgorithm { get; }

        public HashBlock HashValue { get; }

        public string? OriginalString { get; }

        public string? DisplayName { get; }

        public IReadOnlyCollection<Uri> Trackers { get; }

        #region Uncommon Fields
        public long? ExactLength { get; }

        public Uri? ExactSource { get; }

        public IReadOnlyCollection<Uri> AcceptableSources { get; }

        public IReadOnlyCollection<string> KeywordTopic { get; }

        public Uri? ManifestTopic { get; }
        #endregion

        public Magnet(string source)
            : this(new Uri(source))
        {
        }

        public Magnet(Uri uri)
        {
            if (!TryGetMagnetParts(uri,
                out var hashAlgorithm,
                out byte[]? hash,
                out string? displayName,
                out Uri[]? trackers,
                out long? exactLength,
                out Uri? exactSource,
                out Uri[]? acceptableSources,
                out string[]? keywordTopic,
                out Uri? manifestTopic))
                throw new MagnetFormatException();

            HashAlgorithm = hashAlgorithm;
            HashValue = new HashBlock(hash);
            OriginalString = uri.OriginalString;
            DisplayName = displayName;
            Trackers = trackers ?? Array.Empty<Uri>();
            ExactLength = exactLength;
            ExactSource = exactSource;
            AcceptableSources = acceptableSources ?? Array.Empty<Uri>();
            KeywordTopic = keywordTopic ?? Array.Empty<string>();
            ManifestTopic = manifestTopic;
        }

        private Magnet(MagnetHashAlgorithm hashAlgorithm,
            byte[] hash,
            string originalString,
            string? displayName,
            Uri[]? trackers,
            long? exactLength,
            Uri? exactSource,
            Uri[]? acceptableSources,
            string[]? keywordTopic,
            Uri? manifestTopic)
        {
            HashAlgorithm = hashAlgorithm;
            HashValue = new HashBlock(hash);
            OriginalString = originalString;
            DisplayName = displayName;
            Trackers = trackers ?? Array.Empty<Uri>();
            ExactLength = exactLength;
            ExactSource = exactSource;
            AcceptableSources = acceptableSources ?? Array.Empty<Uri>();
            KeywordTopic = keywordTopic ?? Array.Empty<string>();
            ManifestTopic = manifestTopic;
        }

        public Magnet(MagnetHashAlgorithm hashAlgorithm,
            HashBlock hash,
            string? displayName = null,
            IReadOnlyCollection<Uri>? trackers = null,
            long? exactLength = null,
            Uri? exactSource = null,
            IReadOnlyCollection<Uri>? acceptableSources = null,
            IReadOnlyCollection<string>? keywordTopic = null,
            Uri? manifestTopic = null)
        {
            int expectedHashSize = hashAlgorithm switch
            {
                MagnetHashAlgorithm.BTIH => 20,
                MagnetHashAlgorithm.SHA1 => 20,
                MagnetHashAlgorithm.MD5 => 16,
                _ => throw new ArgumentException($"Unknown hash algoritm {hashAlgorithm}.", nameof(hashAlgorithm))
            };

            if (hash.Hash.Length != expectedHashSize)
                throw new ArgumentException($"Hash size mismatch. Expect {expectedHashSize}, got {hash.Hash.Length}.", nameof(hash));

            HashAlgorithm = hashAlgorithm;
            HashValue = hash;
            DisplayName = displayName;
            Trackers = trackers ?? Array.Empty<Uri>();
            ExactLength = exactLength;
            ExactSource = exactSource;
            AcceptableSources = acceptableSources ?? Array.Empty<Uri>();
            KeywordTopic = keywordTopic ?? Array.Empty<string>();
            ManifestTopic = manifestTopic;
        }

        public static bool TryCreate(string source, [NotNullWhen(true)] out Magnet? magnet)
        {
            magnet = null;
            return Uri.TryCreate(source, UriKind.Absolute, out var uri)
                   && TryCreate(uri, out magnet);
        }

        public static bool TryCreate(Uri uri, [NotNullWhen(true)] out Magnet? magnet)
        {
            if (TryGetMagnetParts(uri,
                out var hashAlgorithm,
                out byte[]? hash,
                out string? displayName,
                out Uri[]? trackers,
                out long? exactLength,
                out Uri? exactSource,
                out Uri[]? acceptableSources,
                out string[]? keywordTopic,
                out Uri? manifestTopic))
            {
                magnet = new Magnet(hashAlgorithm,
                                    hash,
                                    uri.OriginalString,
                                    displayName,
                                    trackers,
                                    exactLength,
                                    exactSource,
                                    acceptableSources,
                                    keywordTopic,
                                    manifestTopic);
                return true;
            }
            else
            {
                magnet = null;
                return false;
            }
        }

        private static bool TryGetMagnetParts(
            Uri uri,
            out MagnetHashAlgorithm hashAlgorithm,
            [NotNullWhen(true)] out byte[]? hash,
            out string? displayName,
            out Uri[]? trackers,
            out long? exactLength,
            out Uri? exactSource,
            out Uri[]? acceptableSources,
            out string[]? keywordTopic,
            out Uri? manifestTopic)
        {
            hashAlgorithm = default;
            hash = null;
            displayName = null;
            trackers = null;
            exactLength = null;
            exactSource = null;
            acceptableSources = null;
            keywordTopic = null;
            manifestTopic = null;

            if (!uri.IsAbsoluteUri)
                return false;

            if (uri.Scheme != "magnet")
                return false;

            if (!string.IsNullOrEmpty(uri.AbsolutePath))
                return false;

            var query = HttpUtility.ParseQueryString(uri.Query);

            if (query["xt"] is string xt)
            {
                string[]? parts = xt.Split(':');
                if (parts.Length != 3 || parts[0] != "urn") // TODO: use collection pattern
                    return false;

                ReadOnlySpan<char> hashPart = parts[2];

                switch (parts[1])
                {
                    case "btih":
                    {
                        hashAlgorithm = MagnetHashAlgorithm.BTIH;
                        if (!TryDecodeHash(hashPart, 20, out hash))
                            return false;

                        break;
                    }

                    case "sha1":
                    {
                        hashAlgorithm = MagnetHashAlgorithm.SHA1;
                        if (!TryDecodeHash(hashPart, 20, out hash))
                            return false;

                        break;
                    }

                    case "md5":
                    {
                        hashAlgorithm = MagnetHashAlgorithm.MD5;
                        if (!TryDecodeHash(hashPart, 16, out hash))
                            return false;

                        break;
                    }

                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }

            displayName = query["dn"];

            if (query.GetValues("tr") is string[] tr)
            {
                trackers = new Uri[tr.Length];
                for (int i = 0; i < tr.Length; i++)
                    if (!Uri.TryCreate(tr[i], UriKind.Absolute, out trackers[i]!))
                        return false;
            }

            if (query["xl"] is string xl)
            {
                if (long.TryParse(xl, out long xlValue))
                    exactLength = xlValue;
                else
                    return false;
            }

            if (query["kt"] is string kt)
                keywordTopic = kt.Split('+', StringSplitOptions.RemoveEmptyEntries);

            if (query["xs"] is string xs)
            {
                if (!Uri.TryCreate(xs, UriKind.Absolute, out exactSource))
                    return false;
            }

            if (query.GetValues("as") is string[] @as)
            {
                acceptableSources = new Uri[@as.Length];
                for (int i = 0; i < @as.Length; i++)
                    if (!Uri.TryCreate(@as[i], UriKind.Absolute, out acceptableSources[i]!))
                        return false;
            }

            if (query["mt"] is string mt)
            {
                if (!Uri.TryCreate(mt, UriKind.Absolute, out manifestTopic))
                    return false;
            }

            return true;
        }

        private static bool TryDecodeHash(ReadOnlySpan<char> encoded, int expectedLength, [NotNullWhen(true)] out byte[]? result)
        {
            // TODO: convert to local function when attributes can be applied

            if (encoded.Length == expectedLength * 2) // Hex
            {
                result = new byte[expectedLength];
                for (int i = 0; i < result.Length; i++)
                {
                    if (!byte.TryParse(encoded.Slice(i * 2, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out result[i]))
                        return false;
                }
                return true;
            }
            else if (encoded.Length == Base32.GetMaxEncodedToUtf8Length(expectedLength)) // Base32
            {
                result = new byte[expectedLength];
                if (Base32.DecodeFromUtf16(encoded, result, out int bytesConsumed, out int bytesWritten) == OperationStatus.Done
                    && bytesConsumed == encoded.Length
                    && bytesWritten == expectedLength)
                    return true;
            }

            result = null;
            return false;
        }

        public bool Fits(BitTorrent torrent)
            => HashAlgorithm == MagnetHashAlgorithm.BTIH
            && HashValue == torrent.InfoHash;

        public override bool Equals(object? obj) => Equals(obj as Magnet);
        public bool Equals(Magnet? other) => other is { } && HashAlgorithm == other.HashAlgorithm && HashValue.Equals(other.HashValue); // TODO: use is not null
        public override int GetHashCode() => HashCode.Combine(HashAlgorithm, HashValue);

        public static bool operator ==(Magnet? left, Magnet? right) => EqualityComparer<Magnet>.Default.Equals(left, right);
        public static bool operator !=(Magnet? left, Magnet? right) => !(left == right);

        public override string ToString() => ToString(false);
        public string ToStringBase32() => ToString(true);

        private string ToString(bool useBase32)
        {
            StringBuilder sb = new StringBuilder("magnet:?xt=urn:");

            string? hashAlgorithmPart = HashAlgorithm switch
            {
                MagnetHashAlgorithm.BTIH => "btih:",
                MagnetHashAlgorithm.SHA1 => "sha1:",
                MagnetHashAlgorithm.MD5 => "md5:",
                _ => null
            };
            if (hashAlgorithmPart is null)
                return string.Empty;

            _ = sb.Append(hashAlgorithmPart)
                .Append(useBase32 ? HashValue.ToBase32() : HashValue.ToString());

            return sb.ToString();
        }
    }

    public enum MagnetHashAlgorithm
    {
        BTIH,
        SHA1,
        MD5
    }

    public sealed class MagnetFormatException : FormatException
    {
        public MagnetFormatException() : base("The uri is not a valid magnet.")
        {
        }

        public MagnetFormatException(string message) : base(message)
        {
        }

        public MagnetFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

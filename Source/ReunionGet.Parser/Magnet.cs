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

        public Magnet(string source)
            : this(new Uri(source))
        {
        }

        public Magnet(Uri uri)
        {
            if (!TryGetMagnetParts(uri, out var hashAlgorithm, out byte[]? hash))
                throw new MagnetFormatException();

            HashAlgorithm = hashAlgorithm;
            HashValue = new HashBlock(hash);
            OriginalString = uri.OriginalString;
        }

        private Magnet(MagnetHashAlgorithm hashAlgorithm, byte[] hash, string originalString)
        {
            HashAlgorithm = hashAlgorithm;
            HashValue = new HashBlock(hash);
            OriginalString = originalString;
        }

        public Magnet(MagnetHashAlgorithm hashAlgorithm, HashBlock hash)
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
        }

        public static bool TryCreate(string source, [NotNullWhen(true)] out Magnet? magnet)
        {
            magnet = null;
            return Uri.TryCreate(source, UriKind.Absolute, out var uri)
                   && TryCreate(uri, out magnet);
        }

        public static bool TryCreate(Uri uri, [NotNullWhen(true)] out Magnet? magnet)
        {
            if (TryGetMagnetParts(uri, out var hashAlgorithm, out byte[]? hash))
            {
                magnet = new Magnet(hashAlgorithm, hash, uri.OriginalString);
                return true;
            }
            else
            {
                magnet = null;
                return false;
            }
        }

        private static bool TryGetMagnetParts(Uri uri, out MagnetHashAlgorithm hashAlgorithm, [NotNullWhen(true)] out byte[]? hash)
        {
            hashAlgorithm = default;
            hash = null;

            if (!uri.IsAbsoluteUri)
                return false;

            if (uri.Scheme != "magnet")
                return false;

            if (!string.IsNullOrEmpty(uri.AbsolutePath))
                return false;

            var query = HttpUtility.ParseQueryString(uri.Query);

            string? xt = query["xt"];
            if (xt is null)
                return false;

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

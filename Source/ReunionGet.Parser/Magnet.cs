using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Web;

namespace ReunionGet.Parser
{
    public class Magnet
    {
        public MagnetHashAlgorithm HashAlgorithm { get; }

        public HashBlock HashValue { get; }

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
        }

        private Magnet(MagnetHashAlgorithm hashAlgorithm, byte[] hash)
        {
            HashAlgorithm = hashAlgorithm;
            HashValue = new HashBlock(hash);
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
                magnet = new Magnet(hashAlgorithm, hash);
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

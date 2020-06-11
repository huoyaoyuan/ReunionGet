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

        private readonly byte[] _hash;
        public ReadOnlySpan<byte> Hash => _hash;

        public Magnet(string source)
            : this(new Uri(source))
        {
        }

        public Magnet(Uri uri)
        {
            if (!TryGetMagnetParts(uri, out var hashAlgorithm, out byte[]? hash))
                throw new ArgumentException("The uri is not a valid magnet.", nameof(uri));

            HashAlgorithm = hashAlgorithm;
            _hash = hash;
        }

        private Magnet(MagnetHashAlgorithm hashAlgorithm, byte[] hash)
        {
            HashAlgorithm = hashAlgorithm;
            _hash = hash;
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
                        if (hashPart.Length != 40)
                            return false;

                        hashAlgorithm = MagnetHashAlgorithm.BTIH;
                        hash = new byte[20];

                        for (int i = 0; i < hash.Length; i++)
                        {
                            if (!byte.TryParse(hashPart.Slice(i * 2, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hash[i]))
                                return false;
                        }

                        break;
                    }

                    case "sha1":
                    {
                        if (hashPart.Length != 32)
                            return false;

                        hashAlgorithm = MagnetHashAlgorithm.SHA1;
                        hash = new byte[20];

                        if (Base32.DecodeFromUtf16(hashPart, hash, out int bytesConsumed, out int bytesWritten) != OperationStatus.Done
                            || bytesConsumed != 32
                            || bytesWritten != 20)
                            return false;

                        break;
                    }

                    case "md5":
                    {
                        if (hashPart.Length != 32)
                            return false;

                        hashAlgorithm = MagnetHashAlgorithm.MD5;
                        hash = new byte[16];

                        for (int i = 0; i < hash.Length; i++)
                        {
                            if (!byte.TryParse(hashPart.Slice(i * 2, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hash[i]))
                                return false;
                        }

                        break;
                    }

                    default:
                        return false;
                }
            }

            return true;
        }
    }

    public enum MagnetHashAlgorithm
    {
        BTIH,
        MD5,
        SHA1
    }
}

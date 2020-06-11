using System;
using System.Buffers;
using System.Diagnostics;

namespace ReunionGet.Parser
{
    internal static class Base32
    {
        private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567=";

        private static char Base32ByteToU16(byte base32)
            => Base32Chars[base32];

        private static byte Base32ByteToU8(byte base32)
            => (byte)Base32Chars[base32];

        private static bool Base32U16ToByte(char u16Char, out byte base32)
        {
            // TODO: use range pattern

            if (u16Char >= 'A' && u16Char <= 'Z')
            {
                base32 = (byte)(u16Char - 'A');
                return true;
            }
            else if (u16Char >= 'a' && u16Char <= 'z')
            {
                base32 = (byte)(u16Char - (byte)'a');
                return true;
            }
            else if (u16Char >= '2' && u16Char <= '7')
            {
                base32 = (byte)(u16Char - '2' + 26);
                return true;
            }
            else
            {
                base32 = 0;
                return false;
            }
        }

        private static bool Base32U8ToByte(byte u8byte, out byte base32)
            => Base32U16ToByte((char)u8byte, out base32);

        public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> utf8, Span<byte> bytes, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            bytesConsumed = 0;
            bytesWritten = 0;

            Span<byte> buffer = stackalloc byte[8];
            Span<byte> decodeBuffer = stackalloc byte[5];

            while (true)
            {
                if (utf8.IsEmpty)
                    return OperationStatus.Done;

                if (utf8.Length < 8)
                    return isFinalBlock ? OperationStatus.InvalidData : OperationStatus.NeedMoreData;

                int effectiveBytes = 0;
                for (int i = 0; i < buffer.Length; i++)
                {
                    byte b = utf8[i];

                    if (effectiveBytes != i)
                    {
                        if (b != (byte)'=')
                            return OperationStatus.InvalidData;
                    }
                    else
                    {
                        if (b == (byte)'=')
                            buffer[i] = 0;
                        else if (Base32U8ToByte(b, out buffer[i]))
                            effectiveBytes++;
                        else
                            return OperationStatus.InvalidData;
                    }
                }

                int effectiveChars = effectiveBytes switch
                {
                    2 => 1,
                    4 => 2,
                    5 => 3,
                    7 => 4,
                    8 => 5,
                    _ => -1
                };

                if (effectiveChars == -1)
                    return OperationStatus.InvalidData;

                if (bytes.Length < effectiveChars)
                    return OperationStatus.DestinationTooSmall;

                decodeBuffer[0] = (byte)((buffer[0] << 3) | (buffer[1] >> 2));
                decodeBuffer[1] = (byte)((buffer[1] << 6) | (buffer[2] << 1) | (buffer[3] >> 4));
                decodeBuffer[2] = (byte)((buffer[3] << 4) | (buffer[4] >> 1));
                decodeBuffer[3] = (byte)((buffer[4] << 7) | (buffer[5] << 2) | (buffer[6] >> 3));
                decodeBuffer[4] = (byte)((buffer[6] << 5) | buffer[7]);

                decodeBuffer[..effectiveChars].CopyTo(bytes);

                utf8 = utf8.Slice(8);
                bytesConsumed += 8;
                bytes = bytes.Slice(effectiveChars);
                bytesWritten += effectiveChars;

                if (effectiveChars < 5)
                    return utf8.IsEmpty ? OperationStatus.Done : OperationStatus.InvalidData;
            }
        }

        public static OperationStatus DecodeFromUtf16(ReadOnlySpan<char> utf16, Span<byte> bytes, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            bytesConsumed = 0;
            bytesWritten = 0;

            Span<byte> buffer = stackalloc byte[8];
            Span<byte> decodeBuffer = stackalloc byte[5];

            while (true)
            {
                if (utf16.IsEmpty)
                    return OperationStatus.Done;

                if (utf16.Length < 8)
                    return isFinalBlock ? OperationStatus.InvalidData : OperationStatus.NeedMoreData;

                int effectiveBytes = 0;
                for (int i = 0; i < buffer.Length; i++)
                {
                    char b = utf16[i];

                    if (effectiveBytes != i)
                    {
                        if (b != '=')
                            return OperationStatus.InvalidData;
                    }
                    else
                    {
                        if (b == '=')
                            buffer[i] = 0;
                        else if (Base32U16ToByte(b, out buffer[i]))
                            effectiveBytes++;
                        else
                            return OperationStatus.InvalidData;
                    }
                }

                int effectiveChars = effectiveBytes switch
                {
                    2 => 1,
                    4 => 2,
                    5 => 3,
                    7 => 4,
                    8 => 5,
                    _ => -1
                };

                if (effectiveChars == -1)
                    return OperationStatus.InvalidData;

                if (bytes.Length < effectiveChars)
                    return OperationStatus.DestinationTooSmall;

                decodeBuffer[0] = (byte)((buffer[0] << 3) | (buffer[1] >> 2));
                decodeBuffer[1] = (byte)((buffer[1] << 6) | (buffer[2] << 1) | (buffer[3] >> 4));
                decodeBuffer[2] = (byte)((buffer[3] << 4) | (buffer[4] >> 1));
                decodeBuffer[3] = (byte)((buffer[4] << 7) | (buffer[5] << 2) | (buffer[6] >> 3));
                decodeBuffer[4] = (byte)((buffer[6] << 5) | buffer[7]);

                decodeBuffer[..effectiveChars].CopyTo(bytes);

                utf16 = utf16.Slice(8);
                bytesConsumed += 8;
                bytes = bytes.Slice(effectiveChars);
                bytesWritten += effectiveChars;

                if (effectiveChars < 5)
                    return utf16.IsEmpty ? OperationStatus.Done : OperationStatus.InvalidData;
            }
        }

        public static OperationStatus EncodeToUtf8(ReadOnlySpan<byte> bytes, Span<byte> utf8, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            bytesConsumed = 0;
            bytesWritten = 0;

            Span<byte> buffer = stackalloc byte[8];

            while (true)
            {
                if (bytes.IsEmpty)
                    return OperationStatus.Done;

                if (!isFinalBlock && bytes.Length < 5)
                    return OperationStatus.NeedMoreData;

                if (utf8.Length < 8)
                    return OperationStatus.DestinationTooSmall;

                int consumed = GetNextGroup(bytes, buffer);
                for (int i = 0; i < buffer.Length; i++)
                    utf8[i] = Base32ByteToU8(buffer[i]);

                bytes = bytes.Slice(consumed);
                bytesConsumed += consumed;
                utf8 = utf8.Slice(8);
                bytesWritten += 8;
            }
        }

        public static OperationStatus EncodeToUtf16(ReadOnlySpan<byte> bytes, Span<char> utf16, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            bytesConsumed = 0;
            bytesWritten = 0;

            Span<byte> buffer = stackalloc byte[8];

            while (true)
            {
                if (bytes.IsEmpty)
                    return OperationStatus.Done;

                if (!isFinalBlock && bytes.Length < 5)
                    return OperationStatus.NeedMoreData;

                if (utf16.Length < 8)
                    return OperationStatus.DestinationTooSmall;

                int consumed = GetNextGroup(bytes, buffer);
                for (int i = 0; i < buffer.Length; i++)
                    utf16[i] = Base32ByteToU16(buffer[i]);

                bytes = bytes.Slice(consumed);
                bytesConsumed += consumed;
                utf16 = utf16.Slice(8);
                bytesWritten += 8;
            }
        }

        private static int GetNextGroup(ReadOnlySpan<byte> source, Span<byte> dest)
        {
            Debug.Assert(dest.Length == 8);

            int written = source.Length switch
            {
                1 => 2,
                2 => 4,
                3 => 5,
                4 => 7,
                _ => 8
            };

            uint b5 = source.Length >= 5 ? source[4] : 0U;
            uint b4 = source.Length >= 4 ? source[3] : 0U;
            uint b3 = source.Length >= 3 ? source[2] : 0U;
            uint b2 = source.Length >= 2 ? source[1] : 0U;
            uint b1 = source.Length >= 1 ? source[0] : 0U;

            dest[0] = (byte)(b1 >> 3);
            dest[1] = (byte)(((b1 & 0b_0000_0111) << 2) | (b2 >> 6));
            dest[2] = (byte)((b2 >> 1) & 0b_0001_1111);
            dest[3] = (byte)(((b2 & 0b_0000_0001) << 4) | (b3 >> 4));
            dest[4] = (byte)(((b3 & 0b_0000_1111) << 1) | (b4 >> 7));
            dest[5] = (byte)((b4 >> 2) & 0b_0001_1111);
            dest[6] = (byte)(((b4 & 0b_0000_0011) << 3) | (b5 >> 5));
            dest[7] = (byte)(b5 & 0b_0001_1111);

            for (int i = 0; i < dest.Length; i++)
                if (i >= written)
                {
                    Debug.Assert(dest[i] == 0);
                    dest[i] = 32;
                }

            return Math.Min(source.Length, 5);
        }

        public static int GetMaxDecodedFromUtf8Length(int length)
            => (length + 7) / 8 * 5;

        public static int GetMaxEncodedToUtf8Length(int length)
            => (length + 4) / 5 * 8;
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ReunionGet.Parser
{
    internal ref struct BEncodingReader
    {
        private ReadOnlySpan<byte> _bytes;

        public BEncodingReader(ReadOnlySpan<byte> bytes) => _bytes = bytes;

        private static bool TryParseUtf8Number(ReadOnlySpan<byte> span, out long value, bool strict = false)
        {
            bool negative = false;
            if (!span.IsEmpty && span[0] == (byte)'-')
            {
                negative = true;
                span = span[1..];
            }

            if (span.IsEmpty)
            {
                value = 0;
                return !strict;
            }

            if (strict && span[0] == (byte)'0')
            {
                if (negative || span.Length != 1) // Leading 0s are not allowed
                {
                    value = 0;
                    return false;
                }
            }

            long v = 0;
            foreach (byte b in span)
            {
                if (b < (byte)'0' || b > (byte)'9') // Non-numeric character in integer literal.
                {
                    value = 0;
                    return false;
                }

                v = v * 10 + b - (byte)'0';
            }

            value = negative ? -v : v;
            return true;
        }

        public bool TryReadBytes(out ReadOnlySpan<byte> span)
        {
            if (_bytes.IsEmpty || _bytes[0] < (byte)'0' || _bytes[0] > (byte)'9')
            {
                span = default;
                return false;
            }

            for (int i = 1; i < _bytes.Length; i++)
            {
                if (_bytes[i] == (byte)':')
                {
                    if (TryParseUtf8Number(_bytes[..i], out long l) &&
                       l >= 0 &&
                       l <= int.MaxValue &&
                       _bytes.Length >= i + 1 + l)
                    {
                        int length = (int)l;
                        span = _bytes.Slice(i + 1, length);
                        _bytes = _bytes.Slice(i + 1 + length);
                        return true;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            span = default;
            return false;
        }

        public ReadOnlySpan<byte> ReadBytes()
        {
            if (TryReadBytes(out var span))
                return span;
            else
                throw new FormatException("Current content is not a valid byte sequence.");
        }

        public bool TryReadString([NotNullWhen(true)] out string? str)
        {
            if (TryReadBytes(out var span))
            {
                str = Encoding.UTF8.GetString(span);
                return true;
            }
            else
            {
                str = null;
                return false;
            }
        }

        public string ReadString()
        {
            if (TryReadString(out string? str))
                return str;
            else
                throw new FormatException("Current content is not a valid string.");
        }

        public bool TryReadInt64(out long value, bool strict = false)
        {
            if (_bytes.IsEmpty || _bytes[0] != (byte)'i')
            {
                value = 0;
                return false;
            }

            for (int i = 1; i < _bytes.Length; i++)
            {
                if (_bytes[i] == (byte)'e')
                {
                    if (TryParseUtf8Number(_bytes[1..i], out value, strict))
                    {
                        _bytes = _bytes.Slice(i + 1);
                        return true;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            value = 0;
            return false;
        }

        public long ReadInt64(bool strict = false)
        {
            if (TryReadInt64(out long value, strict))
                return value;
            else
                throw new FormatException("Current content is not a valid number.");
        }

        public bool TryReadInt32(out int value, bool strict = false)
        {
            if (_bytes.IsEmpty || _bytes[0] != (byte)'i')
            {
                value = 0;
                return false;
            }

            for (int i = 1; i < _bytes.Length; i++)
            {
                if (_bytes[i] == (byte)'e')
                {
                    if (TryParseUtf8Number(_bytes[1..i], out long v, strict) &&
                       v >= int.MinValue &&
                       v <= int.MaxValue)
                    {
                        value = (int)v;
                        _bytes = _bytes.Slice(i + 1);
                        return true;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            value = 0;
            return false;
        }

        public int ReadInt32(bool strict = false) => checked((int)ReadInt64(strict));

        public bool TryReadListStart() => TryReadCharacter((byte)'l');

        public bool TryReadDictStart() => TryReadCharacter((byte)'d');

        public bool TryReadListDictEnd() => TryReadCharacter((byte)'e');

        private bool TryReadCharacter(byte ch)
        {
            if (_bytes.IsEmpty || _bytes[0] != ch)
                return false;

            _bytes = _bytes.Slice(1);
            return true;
        }

        public void ReadListStart()
        {
            if (!TryReadListStart())
                throw new FormatException("Current content is not a list.");
        }

        public void ReadDictStart()
        {
            if (!TryReadDictStart())
                throw new FormatException("Current content is not a dict.");
        }

        public readonly bool Ends() => _bytes.IsEmpty;
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace ReunionGet.Parser
{
    public static class BEncoding
    {
        public static object Read(ReadOnlySpan<byte> content)
        {
            object obj = ReadInternal(ref content);

            if (!content.IsEmpty)
                throw new FormatException("Unexpected content after ending.");

            return obj;
        }

        private static object ReadInternal(ref ReadOnlySpan<byte> content)
        {
            try
            {
                byte first = content[0];

                if (first == (byte)'i')
                {
                    int i = 1;
                    while (content[i] != 'e')
                        i++;

                    int value = ParseUtf8Int(content[1..i]);
                    content = content[(i + 1)..];
                    return value;
                }
                else if (first == (byte)'l')
                {
                    var list = new List<object>();
                    content = content[1..];

                    while (content[0] != (byte)'e')
                        list.Add(ReadInternal(ref content));

                    content = content[1..];
                    return list;
                }
                else if (first == (byte)'d')
                {
                    var dict = new Dictionary<string, object>();
                    content = content[1..];

                    while (content[0] != (byte)'e')
                    {
                        object key = ReadInternal(ref content);
                        if (!(key is string keyStr))
                            throw new FormatException("Dictionary key must be string.");

                        object value = ReadInternal(ref content);
                        dict.Add(keyStr, value);
                    }

                    content = content[1..];
                    return dict;
                }
                else if (first >= (byte)'0' && first <= (byte)'9')
                {
                    int i = 1;
                    while (content[i] != (byte)':')
                        i++;

                    int length = ParseUtf8Int(content[..i]);

                    string result = Encoding.UTF8.GetString(content.Slice(i + 1, length));
                    content = content[(i + 1 + length)..];
                    return result;
                }
                else
                {
                    throw new FormatException($"Unknown BEncoding start character {first}.");
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new FormatException("BEncoding input incomplete.", e);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new FormatException("BEncoding input incomplete.", e);
            }

            static int ParseUtf8Int(ReadOnlySpan<byte> span)
            {
                bool negative = false;
                if (span[0] == (byte)'-')
                {
                    negative = true;
                    span = span[1..];
                }

                if (span[0] == (byte)'0')
                {
                    if (negative || span.Length != 1)
                        throw new FormatException("Leading 0s are not allowed.");
                    else
                        return 0;
                }

                int value = 0;
                foreach (byte b in span)
                {
                    if (b < (byte)'0' || b > (byte)'9')
                        throw new FormatException($"Non-numeric character {(char)b} in integer literal.");

                    value = value * 10 + b - (byte)'0';
                }

                return negative ? -value : value;
            }
        }
    }
}

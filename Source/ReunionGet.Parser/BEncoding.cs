using System;
using System.Collections.Generic;

namespace ReunionGet.Parser
{
    public static class BEncoding
    {
        public static object Read(ReadOnlySpan<byte> content, bool strict = false)
        {
            var reader = new BEncodingReader(content);
            object obj = ReadCore(ref reader, strict);

            if (!reader.Ends())
                throw new FormatException("Unexpected content after ending.");

            return obj;
        }

        private static object ReadCore(ref BEncodingReader reader, bool strict)
        {
            if (reader.TryReadInt64(out long l, strict))
            {
                return l;
            }
            else if (reader.TryReadString(out string? str))
            {
                return str;
            }
            else if (reader.TryReadListStart())
            {
                var list = new List<object>();

                while (!reader.TryReadListDictEnd())
                    list.Add(ReadCore(ref reader, strict));

                return list;
            }
            else if (reader.TryReadDictStart())
            {
                var dict = new Dictionary<string, object>();
                string? lastKey = null;

                while (!reader.TryReadListDictEnd())
                {
                    string key = reader.ReadString();
                    if (strict &&
                        string.CompareOrdinal(lastKey, key) >= 0)
                        throw new FormatException("Dictionary keys must appear in sorted order.");

                    lastKey = key;
                    dict.Add(key, ReadCore(ref reader, strict));
                }

                return dict;
            }
            else
            {
                throw new FormatException("Can't read current state as any type of object.");
            }
        }
    }
}

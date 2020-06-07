using System;
using System.Collections.Generic;

namespace ReunionGet.Parser
{
    public static class BEncoding
    {
        public static object Read(ReadOnlySpan<char> content)
        {
            if (content.IsEmpty)
                throw new ArgumentException("Unexpected ending.", nameof(content));

            object obj = ReadInternal(ref content);

            if (!content.IsEmpty)
                throw new ArgumentException("Unexpected content after ending.", nameof(content));

            return obj;
        }

        private static object ReadInternal(ref ReadOnlySpan<char> content)
        {
            char first = content[0];

            if (first == 'i')
            {
                int i = 1;
                while (content[i] != 'e')
                    i++;

                int value = int.Parse(content[1..i]);
                content = content[(i + 1)..];
                return value;
            }
            else if (first == 'l')
            {
                var list = new List<object>();
                content = content[1..];

                while (content[0] != 'e')
                    list.Add(ReadInternal(ref content));

                content = content[1..];
                return list;
            }
            else if (first == 'd')
            {
                var dict = new Dictionary<string, object>();
                content = content[1..];

                while (content[0] != 'e')
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
            else if (first >= '0' && first <= '9')
            {
                int i = 1;
                while (content[i] != ':')
                    i++;

                int length = int.Parse(content[..i]);

                string result = new string(content.Slice(i + 1, length));
                content = content[(i + 1 + length)..];
                return result;
            }
            else
            {
                throw new FormatException($"Unknown BEncoding start character {first}.");
            }
        }
    }
}

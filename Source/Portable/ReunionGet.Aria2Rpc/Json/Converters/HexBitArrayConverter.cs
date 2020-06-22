using System;
using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json.Converters
{
    internal class HexBitArrayConverter : JsonConverter<BitArray>
    {
        public override BitArray? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? strValue = reader.GetString();
            if (strValue is null)
                return null;

            // TODO: use Convert.From/ToHex
            byte[] buffer = new byte[strValue.Length / 2];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = byte.Parse(strValue.AsSpan(i * 2, 2), NumberStyles.HexNumber);

            return new BitArray(buffer);
        }

        public override void Write(Utf8JsonWriter writer, BitArray value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }
}

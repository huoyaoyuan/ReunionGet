using System;
using System.Collections;
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

            return new BitArray(Convert.FromHexString(strValue));
        }

        public override void Write(Utf8JsonWriter writer, BitArray value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }
}

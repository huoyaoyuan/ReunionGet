using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json.Converters
{
    internal class QuotedIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => int.Parse(reader.GetString()!, NumberFormatInfo.InvariantInfo);

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(NumberFormatInfo.InvariantInfo));
    }

    internal class QuotedLongConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => long.Parse(reader.GetString()!, NumberFormatInfo.InvariantInfo);

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(NumberFormatInfo.InvariantInfo));
    }
}

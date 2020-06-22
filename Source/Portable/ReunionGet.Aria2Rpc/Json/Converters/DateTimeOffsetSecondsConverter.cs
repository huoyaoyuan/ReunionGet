using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json.Converters
{
    internal class DateTimeOffsetSecondsConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateTimeOffset.FromUnixTimeSeconds(long.Parse(reader.GetString()!, NumberFormatInfo.InvariantInfo));

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToUnixTimeSeconds().ToString(NumberFormatInfo.InvariantInfo));
    }
}

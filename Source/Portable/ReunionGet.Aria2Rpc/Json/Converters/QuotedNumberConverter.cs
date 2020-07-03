using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json.Converters
{
    internal class QuotedIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt32(),
                JsonTokenType.Null => 0,
                _ => int.Parse(reader.GetString()!, NumberFormatInfo.InvariantInfo)
            };

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(NumberFormatInfo.InvariantInfo));
    }

    internal class QuotedLongConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt64(),
                JsonTokenType.Null => 0,
                _ => long.Parse(reader.GetString()!, NumberFormatInfo.InvariantInfo)
            };

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(NumberFormatInfo.InvariantInfo));
    }

    internal class Aria2GIDConverter : JsonConverter<Aria2GID>
    {
        public override Aria2GID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt64(),
                JsonTokenType.Null => 0,
                _ => long.Parse(reader.GetString()!, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo)
            };

        public override void Write(Utf8JsonWriter writer, Aria2GID value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }
}

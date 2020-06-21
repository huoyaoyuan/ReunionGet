using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json
{
    internal class BoolConverter : JsonConverter<bool>
    {
        [return: MaybeNull]
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.String
            && (reader.ValueTextEquals("true") || reader.ValueTextEquals("OK"));

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            => writer.WriteStringValue(value ? "true" : "false");
    }
}

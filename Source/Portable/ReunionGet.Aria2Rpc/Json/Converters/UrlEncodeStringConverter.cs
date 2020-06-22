using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json.Converters
{
    internal class UrlEncodeStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => WebUtility.UrlDecode(reader.GetString());

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(WebUtility.UrlEncode(value));
    }
}

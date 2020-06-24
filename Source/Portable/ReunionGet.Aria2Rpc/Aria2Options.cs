using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc
{
    public sealed class Aria2Options
    {
        internal static Aria2Options Empty { get; } = new Aria2Options();

        [JsonPropertyName("all-proxy")]
        public string? AllProxy { get; set; }

        [JsonPropertyName("all-proxy-user")]
        public string? AllProxyUser { get; set; }

        [JsonPropertyName("all-proxy-password")]
        public string? AllProxyPassword { get; set; }

        [JsonPropertyName("allow-overwrite")]
        public bool? AllowOverwrite { get; set; }

        [JsonPropertyName("allow-piece-length-change")]
        public bool? AllowPieceLengthChange { get; set; }

        [JsonPropertyName("always-resume")]
        public bool? AlwaysResume { get; set; }

        [JsonPropertyName("async-dns")]
        public bool? AsyncDns { get; set; }

        [JsonPropertyName("auto-file-renaming")]
        public bool? AutoFileRenaming { get; set; }

        [JsonPropertyName("select-file")]
        public string? SelectFile { get; set; }

        //[JsonExtensionData]
        //public Dictionary<string, string> Other { get; set; } = new Dictionary<string, string>();

        // TODO: enable JsonExtensionData when it supports get-only Dictionary<string, string>
    }
}

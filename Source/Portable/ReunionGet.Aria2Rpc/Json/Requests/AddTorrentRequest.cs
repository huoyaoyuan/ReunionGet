using System;
using System.Text.Json.Serialization;
using ReunionGet.Aria2Rpc.Json.Converters;

namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class AddTorrentRequest : RpcParams<long>
    {
        protected internal override string MethodName => "aria2.addTorrent";

        [JsonConverter(typeof(Base64MemoryConverter))]
        public ReadOnlyMemory<byte> Torrent { get; set; }

        public string[]? Uris { get; set; }

        public Aria2Options? Options { get; set; }

        public int? Position { get; set; }
    }
}

using System;
using System.Text.Json.Serialization;
using ReunionGet.Aria2Rpc.Json.Converters;

namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class AddMetalinkRequest : RpcParams<Aria2GID[]>
    {
        protected internal override string MethodName => "aria2.addMetalink";

        [JsonConverter(typeof(Base64MemoryConverter))]
        public ReadOnlyMemory<byte> Metalink { get; set; }

        public Aria2Options? Options { get; set; }

        public int? Position { get; set; }
    }
}

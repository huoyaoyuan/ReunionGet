using System;
using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable CA1707 // Identifier should not contain underscore

namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class ChangePositionRequest : RpcParams<int>
    {
        protected internal override string MethodName => "aria2.changePosition";

        public Aria2GID Gid { get; set; }

        public int Pos { get; set; }

        public ChangePositionOrigin How { get; set; }
    }


    [JsonEnumConverterWithOriginalNaming]
    public enum ChangePositionOrigin
    {
        POS_SET,
        POS_CUR,
        POS_END
    }

    internal class JsonEnumConverterWithOriginalNamingAttribute : JsonConverterAttribute
    {
        private class InvariantNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name;
        }

        public override JsonConverter? CreateConverter(Type typeToConvert)
            => new JsonStringEnumConverter(new InvariantNamingPolicy());
    }
}

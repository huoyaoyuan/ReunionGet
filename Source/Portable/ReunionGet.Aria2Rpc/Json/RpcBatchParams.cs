using System.Linq;

namespace ReunionGet.Aria2Rpc.Json
{
    public sealed class RpcBatchParams<TResponse> : JsonRpcParams<TResponse[]>
    {
        protected internal override string MethodName => "system.multicall";

        public ParamHolder[] Params { get; set; }

#pragma warning disable CA1815 // Override Equals and equality operator on value types
        public readonly struct ParamHolder
#pragma warning restore CA1815 // Override Equals and equality operator on value types
        {
            public string MethodName => Params.MethodName;
            public JsonRpcParams<TResponse> Params { get; }

            public ParamHolder(JsonRpcParams<TResponse> value) => Params = value;
        }

        public RpcBatchParams(params JsonRpcParams<TResponse>[] requests)
            => Params = requests.Select(x => new ParamHolder(x)).ToArray();
    }
}

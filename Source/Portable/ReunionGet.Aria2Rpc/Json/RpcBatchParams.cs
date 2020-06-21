using System.Linq;

namespace ReunionGet.Aria2Rpc.Json
{
    public sealed class RpcBatchParams<TRequest, TResponse> : JsonRpcParams, IJsonRpcRequest<TResponse[]>
        where TRequest : JsonRpcParams, IJsonRpcRequest<TResponse>
    {
        string IJsonRpcRequest<TResponse[]>.MethodName => "system.multicall";

        public ParamHolder[] Params { get; set; }

#pragma warning disable CA1815 // Override Equals and equality operator on value types
        public readonly struct ParamHolder
#pragma warning restore CA1815 // Override Equals and equality operator on value types
        {
            public string MethodName => ((IJsonRpcRequest<TResponse[]>)Params).MethodName;
            public TRequest Params { get; }

            public ParamHolder(TRequest value) => Params = value;
        }

        public RpcBatchParams(params TRequest[] requests)
            => Params = requests.Select(x => new ParamHolder(x)).ToArray();
    }
}

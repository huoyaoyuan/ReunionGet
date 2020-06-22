using System.Linq;

namespace ReunionGet.Aria2Rpc.Json
{
#pragma warning disable CA1815 // Override Equals and equality operator on value types
    public readonly struct ParamHolder<TResponse>
#pragma warning restore CA1815 // Override Equals and equality operator on value types
    {
        public string MethodName => Params.MethodName;
        public JsonRpcParams<TResponse> Params { get; }

        public ParamHolder(JsonRpcParams<TResponse> value) => Params = value;
    }

    public sealed class RpcBatchParams<TResponse> : JsonRpcParams<TResponse[]>
    {
        protected internal override string MethodName => "system.multicall";

        public ParamHolder<TResponse>[] Params { get; set; }

        public RpcBatchParams(params JsonRpcParams<TResponse>[] requests)
            => Params = requests.Select(x => new ParamHolder<TResponse>(x)).ToArray();
    }

    public sealed class RpcBatchParams<T1, T2> : JsonRpcParams<(T1, T2)>
    {
        protected internal override string MethodName => "system.multicall";

        public (ParamHolder<T1>, ParamHolder<T2>) Params { get; set; }

        public RpcBatchParams(JsonRpcParams<T1> param1, JsonRpcParams<T2> param2)
            => Params = (new ParamHolder<T1>(param1), new ParamHolder<T2>(param2));
    }

    public sealed class RpcBatchParams<T1, T2, T3> : JsonRpcParams<(T1, T2, T3)>
    {
        protected internal override string MethodName => "system.multicall";

        public (ParamHolder<T1>, ParamHolder<T2>, ParamHolder<T3>) Params { get; set; }

        public RpcBatchParams(JsonRpcParams<T1> param1, JsonRpcParams<T2> param2, JsonRpcParams<T3> param3)
            => Params = (new ParamHolder<T1>(param1), new ParamHolder<T2>(param2), new ParamHolder<T3>(param3));
    }
}

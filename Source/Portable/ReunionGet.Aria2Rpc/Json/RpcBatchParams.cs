using System.Linq;

namespace ReunionGet.Aria2Rpc.Json
{
#pragma warning disable CA1815 // Override Equals and equality operator on value types
    public readonly struct ParamHolder<TResponse>
#pragma warning restore CA1815 // Override Equals and equality operator on value types
    {
        public string MethodName => Params.MethodName;
        public RpcParams<TResponse> Params { get; }

        public ParamHolder(RpcParams<TResponse> value) => Params = value;
    }

    public sealed class RpcBatchParams<TResponse> : RpcParams<TResponse[]>
    {
        protected internal override string MethodName => "system.multicall";

        protected internal override bool ShutsDown => Params.Any(p => p.Params.ShutsDown);

        public ParamHolder<TResponse>[] Params { get; set; }

        public RpcBatchParams(params RpcParams<TResponse>[] requests)
            => Params = requests.Select(x => new ParamHolder<TResponse>(x)).ToArray();
    }

    public sealed class RpcBatchParams<T1, T2> : RpcParams<(T1, T2)>
    {
        protected internal override string MethodName => "system.multicall";

        protected internal override bool ShutsDown
            => Params.Item1.Params.ShutsDown
            || Params.Item2.Params.ShutsDown;

        public (ParamHolder<T1>, ParamHolder<T2>) Params { get; set; }

        public RpcBatchParams(RpcParams<T1> param1, RpcParams<T2> param2)
            => Params = (new ParamHolder<T1>(param1), new ParamHolder<T2>(param2));
    }

    public sealed class RpcBatchParams<T1, T2, T3> : RpcParams<(T1, T2, T3)>
    {
        protected internal override string MethodName => "system.multicall";

        protected internal override bool ShutsDown
            => Params.Item1.Params.ShutsDown
            || Params.Item2.Params.ShutsDown
            || Params.Item3.Params.ShutsDown;

        public (ParamHolder<T1>, ParamHolder<T2>, ParamHolder<T3>) Params { get; set; }

        public RpcBatchParams(RpcParams<T1> param1, RpcParams<T2> param2, RpcParams<T3> param3)
            => Params = (new ParamHolder<T1>(param1), new ParamHolder<T2>(param2), new ParamHolder<T3>(param3));
    }
}

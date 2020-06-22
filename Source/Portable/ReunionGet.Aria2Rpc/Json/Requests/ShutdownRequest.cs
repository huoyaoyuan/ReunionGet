namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class ShutdownRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.shutdown";

        protected internal override bool ShutsDown => true;
    }

    public sealed class ForceShutdownRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.forceShutdown";

        protected internal override bool ShutsDown => true;
    }
}

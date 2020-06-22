namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class PauseRequest : RpcParams<long>
    {
        protected internal override string MethodName => "aria2.pause";

        public long Gid { get; set; }
    }

    public sealed class PauseAllRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.pauseAll";
    }

    public sealed class ForcePauseRequest : RpcParams<long>
    {
        protected internal override string MethodName => "aria2.forcePause";

        public long Gid { get; set; }
    }

    public sealed class ForcePauseAllRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.forcePauseAll";
    }

    public sealed class UnpauseRequest : RpcParams<long>
    {
        protected internal override string MethodName => "aria2.unpause";

        public long Gid { get; set; }
    }

    public sealed class UnpauseAllRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.unpauseAll";
    }
}

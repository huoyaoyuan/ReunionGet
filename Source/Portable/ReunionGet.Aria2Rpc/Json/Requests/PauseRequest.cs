namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class PauseRequest : RpcParams<Aria2GID>
    {
        protected internal override string MethodName => "aria2.pause";

        public Aria2GID Gid { get; set; }
    }

    public sealed class PauseAllRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.pauseAll";
    }

    public sealed class ForcePauseRequest : RpcParams<Aria2GID>
    {
        protected internal override string MethodName => "aria2.forcePause";

        public Aria2GID Gid { get; set; }
    }

    public sealed class ForcePauseAllRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.forcePauseAll";
    }

    public sealed class UnpauseRequest : RpcParams<Aria2GID>
    {
        protected internal override string MethodName => "aria2.unpause";

        public Aria2GID Gid { get; set; }
    }

    public sealed class UnpauseAllRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.unpauseAll";
    }
}

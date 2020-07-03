namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class RemoveRequest : RpcParams<Aria2GID>
    {
        protected internal override string MethodName => "aria2.remove";

        public Aria2GID Gid { get; set; }
    }

    public sealed class ForceRemoveRequest : RpcParams<Aria2GID>
    {
        protected internal override string MethodName => "aria2.forceRemove";

        public Aria2GID Gid { get; set; }
    }
}

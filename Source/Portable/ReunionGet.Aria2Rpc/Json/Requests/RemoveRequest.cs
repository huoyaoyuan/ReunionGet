namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class RemoveRequest : RpcParams<long>
    {
        protected internal override string MethodName => "aria2.remove";

        public long Gid { get; set; }
    }

    public sealed class ForceRemoveRequest : RpcParams<long>
    {
        protected internal override string MethodName => "aria2.forceRemove";

        public long Gid { get; set; }
    }
}

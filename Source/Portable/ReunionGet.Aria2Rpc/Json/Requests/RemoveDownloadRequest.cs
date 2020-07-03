namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class RemoveDownloadRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.removeDownloadResult";

        public Aria2GID Gid { get; set; }
    }

    public sealed class PurgeDownloadRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.purgeDownloadResult";
    }
}

namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class RemoveDownloadRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.removeDownloadResult";

        public long Gid { get; set; }
    }

    public sealed class PurgeDownloadRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.purgeDownloadResult";
    }
}

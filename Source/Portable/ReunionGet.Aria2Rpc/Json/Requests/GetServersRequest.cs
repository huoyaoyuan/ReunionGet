using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class GetServersRequest : RpcParams<DownloadServerInfoOfFile[]>
    {
        protected internal override string MethodName => "aria2.getServers";

        public long Gid { get; set; }
    }
}

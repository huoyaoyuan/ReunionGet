using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class GetServersRequest : RpcParams<DownloadServerInfoOfFile[]>
    {
        protected internal override string MethodName => "aria2.getServers";

        public Aria2GID Gid { get; set; }
    }
}

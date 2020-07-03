using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class GetPeersRequest : RpcParams<PeerInfo[]>
    {
        protected internal override string MethodName => "aria2.getPeers";

        public Aria2GID Gid { get; set; }
    }
}

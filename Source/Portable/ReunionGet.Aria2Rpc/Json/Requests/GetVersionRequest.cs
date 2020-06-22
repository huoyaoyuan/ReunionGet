using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class GetVersionRequest : RpcParams<Aria2VersionInfo>
    {
        protected internal override string MethodName => "aria2.getVersion";
    }
}

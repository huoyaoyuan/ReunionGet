using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class GetGlobalStatRequest : RpcParams<GlobalStat>
    {
        protected internal override string MethodName => "aria2.getGlobalStat";
    }
}

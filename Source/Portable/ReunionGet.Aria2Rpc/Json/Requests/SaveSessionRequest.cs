namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class SaveSessionRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.saveSession";
    }
}

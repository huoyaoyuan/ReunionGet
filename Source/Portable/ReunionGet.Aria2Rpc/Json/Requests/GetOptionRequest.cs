namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class GetOptionRequest : RpcParams<Aria2Options>
    {
        protected internal override string MethodName => "aria2.getOption";

        public Aria2GID Gid { get; set; }
    }

    public sealed class ChangeOptionRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.changeOption";

        public Aria2GID Gid { get; set; }

        public Aria2Options Options { get; set; } = new Aria2Options();
    }

    public sealed class GetGlobalOptionRequest : RpcParams<Aria2Options>
    {
        protected internal override string MethodName => "aria2.getGlobalOption";
    }

    public sealed class ChangeGlobalOptionRequest : RpcParams<bool>
    {
        protected internal override string MethodName => "aria2.changeGlobalOption";

        public Aria2Options Options { get; set; } = new Aria2Options();
    }
}

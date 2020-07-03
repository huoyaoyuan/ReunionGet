namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class AddUriRequest : RpcParams<Aria2GID>
    {
        protected internal override string MethodName => "aria2.addUri";

        public string[] Uris { get; set; } = null!;

        public Aria2Options? Options { get; set; }

        public int? Position { get; set; }
    }
}

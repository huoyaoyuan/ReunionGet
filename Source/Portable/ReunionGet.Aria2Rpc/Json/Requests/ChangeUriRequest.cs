using System;

namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class ChangeUriRequest : RpcParams<(int numDeleted, int numAdded)>
    {
        protected internal override string MethodName => "aria2.changeUri";

        public long Gid { get; set; }

        public int FileIndex { get; set; }

        public string[] DelUris { get; set; } = Array.Empty<string>();

        public string[] AddUris { get; set; } = Array.Empty<string>();

        public int? Position { get; set; }
    }
}

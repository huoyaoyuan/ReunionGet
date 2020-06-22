using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Aria2Rpc.Json.Requests
{
    public sealed class TellStatusRequest : RpcParams<DownloadProgressStatus>
    {
        protected internal override string MethodName => "aria2.tellStatus";

        public long Gid { get; set; }

        public string[]? Keys { get; set; }
    }

    public sealed class GetUrisRequest : RpcParams<FileDownloadUriInfo[]>
    {
        protected internal override string MethodName => "aria2.getUris";

        public long Gid { get; set; }
    }

    public sealed class GetFilesRequest : RpcParams<DownloadFileStatus[]>
    {
        protected internal override string MethodName => "aria2.getFiles";

        public long Gid { get; set; }
    }

    public sealed class TellActiveRequest : RpcParams<DownloadProgressStatus[]>
    {
        protected internal override string MethodName => "aria2.tellActive";

        public string[]? Keys { get; set; }
    }

    public sealed class TellWaitingRequest : RpcParams<DownloadProgressStatus[]>
    {
        protected internal override string MethodName => "aria2.tellWaiting";

        public int Offset { get; set; }

        public int Num { get; set; }

        public string[]? Keys { get; set; }
    }

    public sealed class TellStoppedRequest : RpcParams<DownloadProgressStatus[]>
    {
        protected internal override string MethodName => "aria2.tellStopped";

        public int Offset { get; set; }

        public int Num { get; set; }

        public string[]? Keys { get; set; }
    }
}

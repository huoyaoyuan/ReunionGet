using System;
using System.Diagnostics.CodeAnalysis;

namespace ReunionGet.Aria2Rpc.Json
{
    internal abstract class JsonRpcMessage
    {
        public string Jsonrpc { get; set; } = "2.0";

        public string Id { get; set; } = "NONEED";
    }

    internal class JsonRpcRequest<T> : JsonRpcMessage
    {
        public string Method { get; set; }

        public T Params { get; set; }

        public JsonRpcRequest(string method, T @params)
        {
            Method = method;
            Params = @params;
        }
    }

    internal class JsonRpcResponse<T> : JsonRpcMessage
    {
        // TODO: use T??

        [MaybeNull, AllowNull]
        public T Result { get; set; } = default;

        public JsonRpcError? Error { get; set; }

        [return: NotNull]
        public T CheckSuccessfulResult()
        {
            CheckSuccess();

            return Result
                ?? throw new InvalidOperationException("The response object hasn't been correctly deserialized, or the remote endpoint doesn't follow the standard.");
        }

        public void CheckSuccess()
        {
            if (Error != null)
                throw new JsonRpcException(Error);
        }
    }

    internal class JsonRpcError
    {
        public int Code { get; set; }

        public string? Message { get; set; }
    }

    public class JsonRpcException : InvalidOperationException
    {
        public int Code { get; }

        internal JsonRpcException(JsonRpcError errorObj)
            : base(errorObj.Message ?? $"A json RPC operation failed with error code {errorObj.Code}.")
            => Code = errorObj.Code;
    }
}

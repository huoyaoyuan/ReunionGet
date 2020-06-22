using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable CA1822 // Mark member static

namespace ReunionGet.Aria2Rpc.Json
{
    internal class RpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string Version => "2.0";

        public int Id { get; }

        public string Method { get; }

        public RpcParams Params { get; }

        public RpcRequest(int id, string method, RpcParams @params)
        {
            Id = id;
            Method = method;
            Params = @params;
        }
    }

    internal class RpcResponse<T>
    {
        [JsonPropertyName("jsonrpc")]
        public string Version => "2.0"; // strawman

        public int Id { get; }

        public RpcError? Error { get; }

        [JsonConstructor]
        public RpcResponse(string version, int id, [MaybeNull] T result, RpcError? error)
        {
            if (version != Version)
                throw new ArgumentException("Bad jsonrpc version.", nameof(version));

            Id = id;
            Result = result;
            Error = error;
        }

        public void CheckSuccess()
        {
            if (Error != null)
                throw new JsonRpcException(Error);
        }

        [MaybeNull, AllowNull] // TODO: use T??
        public T Result { get; }

        [return: NotNull]
        public T CheckSuccessfulResult()
        {
            CheckSuccess();

            return Result
                ?? throw new InvalidOperationException("The response object hasn't been correctly deserialized, or the remote endpoint doesn't follow the standard.");
        }
    }

    internal class RpcError
    {
        public int Code { get; set; }

        public string? Message { get; set; }
    }

    public class JsonRpcException : InvalidOperationException
    {
        public int Code { get; }

        internal JsonRpcException(RpcError errorObj)
            : base(errorObj.Message ?? $"A json RPC operation failed with error code {errorObj.Code}.")
            => Code = errorObj.Code;
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable CA1822 // Mark member static

namespace ReunionGet.Aria2Rpc.Json
{
    internal class JsonRpcRequest<T>
    {
        [JsonPropertyName("jsonrpc")]
        public string Version => "2.0";

        public int Id { get; }

        public string Method { get; }

        public T Params { get; }

        public JsonRpcRequest(int id, string method, T @params)
        {
            Id = id;
            Method = method;
            Params = @params;
        }
    }

    internal class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string Version => "2.0"; // strawman

        public int Id { get; }

        public JsonRpcError? Error { get; }

        [JsonConstructor]
        public JsonRpcResponse(string version, int id, JsonRpcError? error)
        {
            if (version != Version)
                throw new ArgumentException("Bad jsonrpc version.", nameof(version));

            Id = id;
            Error = error;
        }

        public void CheckSuccess()
        {
            if (Error != null)
                throw new JsonRpcException(Error);
        }
    }

    internal class JsonRpcResponse<T> : JsonRpcResponse
    {
        [MaybeNull, AllowNull] // TODO: use T??
        public T Result { get; }

        [JsonConstructor]
        public JsonRpcResponse(string version, int id, [MaybeNull] T result, JsonRpcError? error)
            : base(version, id, error)
            => Result = result;

        [return: NotNull]
        public T CheckSuccessfulResult()
        {
            CheckSuccess();

            return Result
                ?? throw new InvalidOperationException("The response object hasn't been correctly deserialized, or the remote endpoint doesn't follow the standard.");
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

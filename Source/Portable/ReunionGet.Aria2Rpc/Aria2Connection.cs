using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ReunionGet.Aria2Rpc.Json;

namespace ReunionGet.Aria2Rpc
{
    public sealed class Aria2Connection : IDisposable
    {
        private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new ValueTupleToArrayConverter(),
                new BoolConverter(),
                new JsonRpcParamsConverter()
            }
        };

        private readonly HttpClient _httpClient;
        private readonly string _tokenParam;
        private readonly Random _random = new Random();

        public void Dispose() => _httpClient.Dispose();

        public Aria2Connection(Uri baseUri, string token)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = baseUri
            };
            _tokenParam = "token:" + token;
        }

        public Aria2Connection(string address, int port, string token)
            : this(new Uri($"http://{address}:{port}"), token)
        {
        }

        public async Task<TResponse> DoRpcAsync<TRequest, TResponse>(TRequest @params)
            where TRequest : JsonRpcParams, IJsonRpcRequest<TResponse>
        {
            @params.Token = _tokenParam;
            var rpcRequest = new JsonRpcRequest<TRequest>(_random.Next(), @params.MethodName, @params);
            var httpResponse = await _httpClient.PostAsJsonAsync("jsonrpc", rpcRequest, s_serializerOptions)
                .ConfigureAwait(false);
            @params.Token = null;

            var rpcResponse = await httpResponse.Content.ReadFromJsonAsync<JsonRpcResponse<TResponse>>(s_serializerOptions)
                .ConfigureAwait(false);

            if (rpcRequest.Id != rpcResponse.Id)
                throw new InvalidOperationException("Bad response id from remote.");

            return rpcResponse.CheckSuccessfulResult();
        }

        public Task<TResponse[]> BatchAsync<TRequest, TResponse>(params TRequest[] @params)
            where TRequest : JsonRpcParams, IJsonRpcRequest<TResponse>
            => DoRpcAsync<RpcBatchParams<TRequest, TResponse>, TResponse[]>(new RpcBatchParams<TRequest, TResponse>(@params));
    }
}

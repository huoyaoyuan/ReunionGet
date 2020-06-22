using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ReunionGet.Aria2Rpc.Json;
using ReunionGet.Aria2Rpc.Json.Converters;

namespace ReunionGet.Aria2Rpc
{
    public sealed partial class Aria2Connection : IDisposable, IAsyncDisposable
    {
        private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new ValueTupleToArrayConverter(),
                new BoolConverter(),
                new JsonRpcParamsConverter(),
                new QuotedIntConverter(),
                new QuotedLongConverter()
            },
            IgnoreNullValues = true
        };

        private readonly HttpClient _httpClient;
        private readonly string _tokenParam;
        private readonly Random _random = new Random();
        private readonly bool _shutdownOnDisposal;

        private bool _disposed;
        public bool ShutDown { get; private set; }

#pragma warning disable CA2012 // Use ValueTask correctly
        public void Dispose() => _ = DisposeAsync(); // never waited
#pragma warning restore CA2012 // Use ValueTask correctly

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_shutdownOnDisposal && !ShutDown)
                _ = await ForceShutdownAsync().ConfigureAwait(false);

            _httpClient.Dispose();
        }

        public Aria2Connection(Uri baseUri, string token, bool shutdownOnDisposal = true)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = baseUri
            };
            _tokenParam = "token:" + token;
            _shutdownOnDisposal = shutdownOnDisposal;
        }

        public Aria2Connection(string address, int port, string token, bool shutdownOnDisposal = true)
            : this(new Uri($"http://{address}:{port}"), token, shutdownOnDisposal)
        {
        }

        public async Task<TResponse> DoRpcAsync<TResponse>(RpcParams<TResponse> @params)
        {
            if (_disposed || ShutDown)
                throw new ObjectDisposedException(nameof(Aria2Connection),
                    "The connection has been disposed or shut down.");

            @params.Token = _tokenParam;
            var rpcRequest = new RpcRequest(_random.Next(), @params.MethodName, @params);
            var httpResponse = await _httpClient.PostAsJsonAsync("jsonrpc", rpcRequest, s_serializerOptions)
                .ConfigureAwait(false);
            @params.Token = null;

            var rpcResponse = await httpResponse.Content.ReadFromJsonAsync<RpcResponse<TResponse>>(s_serializerOptions)
                .ConfigureAwait(false);

            if (rpcRequest.Id != rpcResponse.Id)
                throw new InvalidOperationException("Bad response id from remote.");

            if (@params.ShutsDown)
                ShutDown = true;

            return rpcResponse.CheckSuccessfulResult();
        }

        public async Task<TResponse> DoRpcWithoutTokenAsync<TResponse>(string methodName)
        {
            if (_disposed || ShutDown)
                throw new ObjectDisposedException(nameof(Aria2Connection),
                    "The connection has been disposed or shut down.");

            var rpcRequest = new RpcRequest(_random.Next(), methodName, null);
            var httpResponse = await _httpClient.PostAsJsonAsync("jsonrpc", rpcRequest, s_serializerOptions)
                .ConfigureAwait(false);

            var rpcResponse = await httpResponse.Content.ReadFromJsonAsync<RpcResponse<TResponse>>(s_serializerOptions)
                .ConfigureAwait(false);

            if (rpcRequest.Id != rpcResponse.Id)
                throw new InvalidOperationException("Bad response id from remote.");

            return rpcResponse.CheckSuccessfulResult();
        }

        public Task<TResponse[]> BatchAsync<TResponse>(params RpcParams<TResponse>[] @params)
            => DoRpcAsync(new RpcBatchParams<TResponse>(@params));

        public Task<(T1, T2)> BatchAsync<T1, T2>(RpcParams<T1> param1, RpcParams<T2> param2)
            => DoRpcAsync(new RpcBatchParams<T1, T2>(param1, param2));

        public Task<(T1, T2, T3)> BatchAsync<T1, T2, T3>(RpcParams<T1> param1, RpcParams<T2> param2, RpcParams<T3> param3)
            => DoRpcAsync(new RpcBatchParams<T1, T2, T3>(param1, param2, param3));
    }
}

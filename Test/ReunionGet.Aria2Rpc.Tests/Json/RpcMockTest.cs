using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReunionGet.Aria2Rpc.Json;
using Xunit;

#pragma warning disable IDE1006 // Naming style
#pragma warning disable CA1812 // Internal class never instantiated

namespace ReunionGet.Aria2Rpc.Tests.Json
{
    internal class RequestObjectTemplate
    {
        public string jsonrpc { get; set; } = null!;

        public int id { get; set; }

        public string method { get; set; } = null!;

        public object[]? @params { get; set; }
    }

    internal class ResponseObjectTemplate
    {
        public string jsonrpc { get; set; } = null!;

        public int id { get; set; }

        public object? result { get; set; }

        public object? error { get; set; }
    }

    internal class MockHandler : HttpMessageHandler
    {
        private readonly (string, object[], object)[] _possibleResults;

        public MockHandler(params (string, object[], object)[] possibleResults) => _possibleResults = possibleResults;

        public List<string> Requested { get; } = new List<string>();

        private readonly MediaTypeHeaderValue _mediaType = new MediaTypeHeaderValue("application/json-rpc");

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/jsonrpc", request.RequestUri!.LocalPath);
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);

            Assert.NotNull(request.Content!.Headers.ContentLength); // aira2 quirk: content length must be set

            var requestObj = await request.Content.ReadFromJsonAsync<RequestObjectTemplate>().ConfigureAwait(false);
            Assert.Equal("2.0", requestObj.jsonrpc);

            foreach (var (method, req, rsp) in _possibleResults)
            {
                if (method == requestObj.method)
                {
                    Requested.Add(method);
                    Assert.Equal(JsonSerializer.Serialize(req), JsonSerializer.Serialize(requestObj.@params));

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new ResponseObjectTemplate
                        {
                            jsonrpc = "2.0",
                            id = requestObj.id,
                            result = rsp
                        }, mediaType: _mediaType)
                    };
                }
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = JsonContent.Create(new ResponseObjectTemplate
                {
                    jsonrpc = "2.0",
                    id = requestObj.id,
                    error = new
                    {
                        code = -1,
                        message = "error"
                    }
                }, mediaType: _mediaType)
            };
        }
    }

    public class RpcMockTest
    {
        [Fact]
        public async Task TestUseAndDispose()
        {
            var handler = new MockHandler(
                ("aria2.addUri",
                new object[]
                {
                    "token:abc",
                    new [] { "http://localhost" },
                    new { },
                    3
                }, "1a2b3c4d5e"),
                ("aria2.forceShutdown",
                new[] { "token:abc" },
                "OK"));

            await using (var connection = new Aria2Connection("localhost", 1000, "abc", handler))
            {
                long gid = await connection.AddUriAsync(new[] { "http://localhost" }, position: 3).ConfigureAwait(false);
                Assert.Equal(0x1a2b3c4d5e, gid);
            }

            Assert.Equal(new[]
            {
                "aria2.addUri",
                "aria2.forceShutdown"
            }, handler.Requested);
        }

        [Fact]
        public async Task TestException()
        {
            var exception = await Assert.ThrowsAsync<JsonRpcException>(async () =>
            {
                await using (var connection = new Aria2Connection("localhost", 1000, "abc", new MockHandler()))
                {
                    long gid = await connection.AddUriAsync(new[] { "http://localhost" }, position: 3).ConfigureAwait(false);
                    Assert.Equal(0x1a2b3c4d5e, gid);
                }
            }).ConfigureAwait(false);

            Assert.Equal(-1, exception.Code);
            Assert.Equal("error", exception.Message);
        }
    }
}

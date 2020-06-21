using System.Text.Json;
using ReunionGet.Aria2Rpc.Json;
using Xunit;

namespace ReunionGet.Aria2Rpc.Tests.Json
{
    public class JsonRpcParamsConverterTest
    {
        private class TestParams : JsonRpcParams
        {
            public string? Param2 { get; set; }
            public int Param1 { get; set; }
        }

        private static void TestSerialization(TestParams @params, string json)
        {
            Assert.Equal(json, JsonSerializer.Serialize(@params, new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonRpcParamsConverter()
                }
            }));
        }

        [Fact]
        public void TestFull()
        {
            TestSerialization(new TestParams
            {
                Token = "token",
                Param2 = "abc",
                Param1 = 114514
            }, "[\"token\",\"abc\",114514]");
        }

        [Fact]
        public void TestTokenNull()
        {
            TestSerialization(new TestParams
            {
                Param2 = "abc",
                Param1 = 114514
            }, "[\"abc\",114514]");
        }

        [Fact]
        public void TestParamNull()
        {
            TestSerialization(new TestParams
            {
                Token = "token",
                Param1 = 114514
            }, "[\"token\",null,114514]");
        }
    }
}

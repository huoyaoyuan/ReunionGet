﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReunionGet.Aria2Rpc.Json;
using ReunionGet.Aria2Rpc.Json.Converters;
using Xunit;

namespace ReunionGet.Aria2Rpc.Tests.Json
{
    public class JsonRpcParamsConverterTest
    {
        private class TestParams : RpcParams<bool>
        {
            protected internal override string MethodName => "test.testMethod";
            public string? Param2 { get; set; }
            public int? Param1 { get; set; }
        }

        private static void TestSerialization<T>(T @params, string json)
        {
            Assert.Equal(json, JsonSerializer.Serialize(@params, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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
            }, "[\"token\"]");
        }

        [Fact]
        public void TestParamNullableStruct()
        {
            TestSerialization(new TestParams
            {
                Token = "token",
                Param2 = "abc"
            }, "[\"token\",\"abc\"]");
        }

        [Fact]
        public void TestBatch()
        {
            var param1 = new TestParams
            {
                Param2 = "abc",
                Param1 = 114514
            };
            var param2 = new TestParams
            {
                Param2 = "def",
                Param1 = 1919810
            };
            var batch = new RpcBatchParams<bool>(param1, param2)
            {
                Token = "token"
            };
            TestSerialization(batch,
                "[\"token\",[{\"methodName\":\"test.testMethod\",\"params\":[\"abc\",114514]},{\"methodName\":\"test.testMethod\",\"params\":[\"def\",1919810]}]]");
        }

        private class TestParams2 : RpcParams<bool>
        {
            protected internal override string MethodName => "test.testMethod";

            [JsonConverter(typeof(Base64MemoryConverter))]
            public ReadOnlyMemory<byte> Bytes { get; set; }
        }

        [Fact]
        public void TestNestedConverterAttribute()
        {
            TestSerialization(new TestParams2
            {
                Bytes = new byte[] { 1, 2, 3, 4, 5 }
            }, "[\"AQIDBAU=\"]");
        }
    }
}

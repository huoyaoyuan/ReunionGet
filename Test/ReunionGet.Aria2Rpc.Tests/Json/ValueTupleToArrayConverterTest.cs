using System;
using System.Collections.Generic;
using System.Text.Json;
using ReunionGet.Aria2Rpc.Json.Converters;
using Xunit;

namespace ReunionGet.Aria2Rpc.Test.Json
{
    public class ValueTupleToArrayConverterTest
    {
        private class Class1<TA, TB> : IEquatable<Class1<TA, TB>?>
        {
            public TA A { get; set; } = default!;
            public TB B { get; set; } = default!;

            public override bool Equals(object? obj) => Equals(obj as Class1<TA, TB>);
            public bool Equals(Class1<TA, TB>? other) => other != null && EqualityComparer<TA>.Default.Equals(A, other.A) && EqualityComparer<TB>.Default.Equals(B, other.B);
            public override int GetHashCode() => 0;
        }

        public static IEnumerable<object[]> Data => new[]
        {
            new object[] { (1, 2), "[1,2]" },
            new object[] { (1, "2"), "[1,\"2\"]" },
            new object[] { new Class1<(int, int), (bool, bool)> { A = (1, 2), B = (true, false) }, "{\"A\":[1,2],\"B\":[true,false]}" },
        };

        [Theory]
        [MemberData(nameof(Data))]
        public void TestSerialize(object obj, string json)
        {
            Assert.Equal(json, JsonSerializer.Serialize(obj, obj.GetType(), new JsonSerializerOptions
            {
                Converters =
                {
                    new ValueTupleToArrayConverter()
                }
            }));
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void TestDeserialize(object obj, string json)
        {
            Assert.Equal(obj, JsonSerializer.Deserialize(json, obj.GetType(), new JsonSerializerOptions
            {
                Converters =
                {
                    new ValueTupleToArrayConverter()
                }
            })!);
        }
    }
}

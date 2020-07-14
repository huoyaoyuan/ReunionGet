using System.Text;
using BenchmarkDotNet.Attributes;
using ReunionGet.Parser;

namespace ReunionGet.Benchmarks
{
    public class Base32Benchmark
    {
        private byte[] _source = null!;
        private string _utf16 = null!;
        private byte[] _utf8 = null!;

        [GlobalSetup]
        public void Setup()
        {
            _source = Encoding.UTF8.GetBytes("a quick brown fox jumps over the lazy dog.");
            _utf16 = EncodeUtf16(_source);
            _utf8 = EncodeUtf8(_source);
        }

        private static string EncodeUtf16(byte[] bytes)
            => string.Create(Base32.GetMaxEncodedToUtf8Length(bytes.Length), bytes,
                (span, b) => Base32.EncodeToUtf16(bytes, span, out _, out _));

        private static byte[] EncodeUtf8(byte[] bytes)
        {
            byte[] result = new byte[Base32.GetMaxEncodedToUtf8Length(bytes.Length)];
            _ = Base32.EncodeToUtf8(bytes, result, out _, out _);
            return result;
        }

        [Benchmark]
        public string EncodeUtf16() => EncodeUtf16(_source);

        [Benchmark]
        public byte[] EncodeUtf8() => EncodeUtf8(_source);

        [Benchmark]
        public byte[] DecodeUtf16()
        {
            string source = _utf16;
            byte[] result = new byte[Base32.GetMaxDecodedFromUtf8Length(source.Length)];
            _ = Base32.DecodeFromUtf16(source, result, out _, out _);
            return result;
        }

        [Benchmark]
        public byte[] DecodeUtf8()
        {
            byte[] source = _utf8;
            byte[] result = new byte[Base32.GetMaxDecodedFromUtf8Length(source.Length)];
            _ = Base32.DecodeFromUtf8(source, result, out _, out _);
            return result;
        }
    }
}

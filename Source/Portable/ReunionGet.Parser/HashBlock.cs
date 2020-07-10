using System;
using System.Buffers;

namespace ReunionGet.Parser
{
    public readonly struct HashBlock : IEquatable<HashBlock>
    {
        // TODO: consider non-defaultable struct

        private readonly byte[] _hash;

        public ReadOnlySpan<byte> Hash => _hash ?? throw new InvalidOperationException($"Using uninitialized {nameof(HashBlock)} instance.");

        /// <summary>
        /// Constructs <see cref="HashBlock"/> by taking ownership of <paramref name="array"/>.
        /// </summary>
        /// <param name="array">The array to take ownership of.</param>
        internal HashBlock(byte[] array) => _hash = array;

        public override bool Equals(object? obj) => obj is HashBlock hash && Equals(hash);
        public bool Equals(HashBlock other) => Hash.SequenceEqual(other.Hash);
        public override int GetHashCode()
        {
            HashCode hash = default;
            foreach (byte b in Hash)
                hash.Add(b);
            return hash.ToHashCode();
        }

        public static bool operator ==(HashBlock left, HashBlock right) => left.Equals(right);
        public static bool operator !=(HashBlock left, HashBlock right) => !(left == right);

        /// <summary>
        /// Get uppercase Hex representation of this <see cref="HashBlock"/>.
        /// </summary>
        /// <returns>Hex representation string.</returns>
        public override string ToString()
            => _hash is null
            ? string.Empty
            : string.Create(_hash.Length * 2, _hash, (span, array) =>
            {
                for (int i = 0; i < array.Length; i++)
                    _ = array[i].TryFormat(span.Slice(i * 2), out _, "X2");
            });

        /// <summary>
        /// Get lowercase Hex representation of this <see cref="HashBlock"/>.
        /// </summary>
        /// <returns>Hex representation string.</returns>
        public string ToStringLower()
            => _hash is null
            ? string.Empty
            : string.Create(_hash.Length * 2, _hash, (span, array) =>
            {
                for (int i = 0; i < array.Length; i++)
                    _ = array[i].TryFormat(span.Slice(i * 2), out _, "x2");
            });

        public string ToBase32() => string.Create(
            Base32.GetMaxEncodedToUtf8Length(Hash.Length),
            this,
            (span, self) => _ = self.TryFormatBase32(span, out _));

        public bool TryFormatBase32(Span<char> destination, out int bytesWritten)
            => Base32.EncodeToUtf16(Hash, destination, out int bytesConsumed, out bytesWritten) == OperationStatus.Done
            && bytesConsumed == Hash.Length;
    }
}

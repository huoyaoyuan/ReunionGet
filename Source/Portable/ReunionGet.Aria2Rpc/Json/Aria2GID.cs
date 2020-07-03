using System;
using System.Globalization;

namespace ReunionGet.Aria2Rpc.Json
{
    public struct Aria2GID : IEquatable<Aria2GID>
    {
        public long Value { get; }

        public Aria2GID(long value) => Value = value;
        public static implicit operator Aria2GID(long value) => new Aria2GID(value);
        public static implicit operator long(Aria2GID gid) => gid.Value;

        public static bool operator ==(Aria2GID left, Aria2GID right) => left.Equals(right);
        public static bool operator !=(Aria2GID left, Aria2GID right) => !(left == right);

        public override bool Equals(object? obj) => obj is Aria2GID gID && Equals(gID);
        public bool Equals(Aria2GID other) => Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString("x", NumberFormatInfo.InvariantInfo);
    }
}

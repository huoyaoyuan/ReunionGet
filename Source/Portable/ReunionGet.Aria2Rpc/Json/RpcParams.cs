using System.Reflection;

namespace ReunionGet.Aria2Rpc.Json
{
    /// <summary>
    /// The abstraction of rpc params used for serialization.
    /// Don't inherit from this directly. Inherit from <see cref="RpcParams{TResponse}"/>.
    /// </summary>
    public abstract class RpcParams
    {
        protected internal abstract string MethodName { get; }

        protected internal virtual bool ShutsDown => false;

        internal string? Token { get; set; }

        internal static PropertyInfo TokenProperty = typeof(RpcParams).GetProperty(nameof(Token), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private protected RpcParams()
        {
            // Prevents external inheritance
        }
    }

    /// <summary>
    /// Associate a rpc parameter type to its and response.
    /// </summary>
    /// <typeparam name="TResponse">The type of response of this rpc request.
    /// Use <see cref="bool"/> if accepts "OK".</typeparam>
    public abstract class RpcParams<TResponse> : RpcParams
    {
        // No additional member
    }
}

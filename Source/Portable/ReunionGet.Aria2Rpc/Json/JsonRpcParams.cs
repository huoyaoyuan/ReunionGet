using System.Reflection;

namespace ReunionGet.Aria2Rpc.Json
{
    /// <summary>
    /// The abstraction of rpc params used for serialization.
    /// Don't inherit from this directly. Inherit from <see cref="JsonRpcParams{TResponse}"/>.
    /// </summary>
    public abstract class JsonRpcParams
    {
        protected internal abstract string MethodName { get; }

        internal string? Token { get; set; }

        internal static PropertyInfo TokenProperty = typeof(JsonRpcParams).GetProperty(nameof(Token), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private protected JsonRpcParams()
        {
            // Prevents external inheritance
        }
    }

    /// <summary>
    /// Associate a rpc parameter type to its and response.
    /// </summary>
    /// <typeparam name="TResponse">The type of response of this rpc request.
    /// Use <see cref="bool"/> if accepts "OK".</typeparam>
    public abstract class JsonRpcParams<TResponse> : JsonRpcParams
    {
        // No additional member
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json
{
    internal class JsonRpcParamsConverter : JsonConverterFactory
    {
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => (JsonConverter)Activator.CreateInstance(typeof(JsonRpcParamsConverter<>).MakeGenericType(typeToConvert))!;

        public override bool CanConvert(Type typeToConvert) => typeof(JsonRpcParams).IsAssignableFrom(typeToConvert);

        internal static MethodInfo GetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))
            ?? throw new NotSupportedException($"Cannot get method handle of {nameof(Type)}.{nameof(Type.GetTypeFromHandle)}");
        internal static MethodInfo GetConverter = typeof(JsonSerializerOptions).GetMethod(nameof(JsonSerializerOptions.GetConverter))
            ?? throw new NotSupportedException($"Cannot get method handle of {nameof(JsonSerializerOptions)}.{nameof(JsonSerializerOptions.GetConverter)}");
        internal static MethodInfo WriteStartArray = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteStartArray), Array.Empty<Type>())
            ?? throw new NotSupportedException($"Cannot get method handle of {nameof(Utf8JsonWriter)}.{nameof(Utf8JsonWriter.WriteStartArray)}");
        internal static MethodInfo WriteEndArray = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteEndArray))
            ?? throw new NotSupportedException($"Cannot get method handle of {nameof(Utf8JsonWriter)}.{nameof(Utf8JsonWriter.WriteEndArray)}");
        internal static MethodInfo WriteStringValue = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteStringValue), new[] { typeof(string) })
            ?? throw new NotSupportedException($"Cannot get method handle of {nameof(Utf8JsonWriter)}.{nameof(Utf8JsonWriter.WriteStringValue)}");
    }

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

    internal class JsonRpcParamsConverter<T> : JsonConverter<T>
        where T : JsonRpcParams
    {
        private readonly Action<Utf8JsonWriter, T, JsonSerializerOptions> _writeDelegate;

        public JsonRpcParamsConverter()
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var writeMethod = new DynamicMethod("WriteDelegate", typeof(void),
                new[] { typeof(Utf8JsonWriter), typeof(T), typeof(JsonSerializerOptions) });
            var writeBuilder = writeMethod.GetILGenerator();

            var jmpLabel = writeBuilder.DefineLabel();
            var strLocal = writeBuilder.DeclareLocal(typeof(string));

            writeBuilder.Emit(OpCodes.Ldarg_0);
            writeBuilder.EmitCall(OpCodes.Callvirt, JsonRpcParamsConverter.WriteStartArray, null);

            writeBuilder.Emit(OpCodes.Ldarg_1);
            writeBuilder.EmitCall(OpCodes.Callvirt, JsonRpcParams.TokenProperty.GetMethod!, null);
            writeBuilder.Emit(OpCodes.Stloc, strLocal);
            writeBuilder.Emit(OpCodes.Ldloc, strLocal);
            writeBuilder.Emit(OpCodes.Brfalse_S, jmpLabel);

            writeBuilder.Emit(OpCodes.Ldarg_0);
            writeBuilder.Emit(OpCodes.Ldloc, strLocal);
            writeBuilder.EmitCall(OpCodes.Callvirt, JsonRpcParamsConverter.WriteStringValue, null);

            writeBuilder.MarkLabel(jmpLabel);
            foreach (var p in properties)
            {
                if (p == JsonRpcParams.TokenProperty)
                    continue;

                if (p.GetMethod is null)
                    continue;

                writeBuilder.Emit(OpCodes.Ldarg_2);
                writeBuilder.Emit(OpCodes.Ldtoken, p.PropertyType);
                writeBuilder.EmitCall(OpCodes.Call, JsonRpcParamsConverter.GetTypeFromHandle, null);
                writeBuilder.EmitCall(OpCodes.Callvirt, JsonRpcParamsConverter.GetConverter, null);

                writeBuilder.Emit(OpCodes.Ldarg_0);
                writeBuilder.Emit(OpCodes.Ldarg_1);
                writeBuilder.EmitCall(OpCodes.Callvirt, p.GetMethod, null);
                writeBuilder.Emit(OpCodes.Ldarg_2);
                writeBuilder.EmitCall(OpCodes.Callvirt, typeof(JsonConverter<>).MakeGenericType(p.PropertyType).GetMethod(nameof(JsonConverter<T>.Write))!, null);
            }
            writeBuilder.Emit(OpCodes.Ldarg_0);
            writeBuilder.EmitCall(OpCodes.Callvirt, JsonRpcParamsConverter.WriteEndArray, null);
            writeBuilder.Emit(OpCodes.Ret);

            _writeDelegate = (Action<Utf8JsonWriter, T, JsonSerializerOptions>)writeMethod.CreateDelegate(typeof(Action<Utf8JsonWriter, T, JsonSerializerOptions>));
        }

        [DoesNotReturn]
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("Json rpc params are only meant to be serialized.");

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => _writeDelegate(writer, value, options);
    }
}

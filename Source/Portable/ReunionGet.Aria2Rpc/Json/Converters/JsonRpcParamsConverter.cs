using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json.Converters
{
    using WriteAction = Action<Utf8JsonWriter, RpcParams, JsonSerializerOptions>;

    internal class JsonRpcParamsConverter : JsonConverterFactory
    {
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => new JsonRpcParamsConverterImpl();

        public override bool CanConvert(Type typeToConvert) => typeof(RpcParams).IsAssignableFrom(typeToConvert);
    }

    internal class JsonRpcParamsConverterImpl : JsonConverter<RpcParams>
    {

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

        private readonly Dictionary<Type, WriteAction> _writeDelegates
            = new Dictionary<Type, WriteAction>();

        [DoesNotReturn]
        public override RpcParams Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("Json rpc params are only meant to be serialized.");

        public override void Write(Utf8JsonWriter writer, RpcParams value, JsonSerializerOptions options)
        {
            var valueType = value.GetType();

            if (!_writeDelegates.TryGetValue(valueType, out var writeDelegate))
            {
                writeDelegate = CreateWriteDelegate(valueType);
                _writeDelegates[valueType] = writeDelegate;
            }

            writeDelegate(writer, value, options);
        }

        private class JsonConverterClosure
        {

        }

        private static WriteAction CreateWriteDelegate(Type valueType)
        {
            var properties = valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var writeMethod = new DynamicMethod("WriteDelegate", typeof(void),
                new[]
                {
                    typeof(JsonConverterClosure),
                    typeof(Utf8JsonWriter),
                    typeof(RpcParams),
                    typeof(JsonSerializerOptions),
                });
            var writeBuilder = writeMethod.GetILGenerator();

            var skipTokenLabel = writeBuilder.DefineLabel();
            var endLabel = writeBuilder.DefineLabel();
            var cleanStackLabel = writeBuilder.DefineLabel();

            var typedLocal = writeBuilder.DeclareLocal(valueType);
            var strLocal = writeBuilder.DeclareLocal(typeof(string));

            writeBuilder.Emit(OpCodes.Ldarg_1);
            writeBuilder.EmitCall(OpCodes.Callvirt, WriteStartArray, null);

            writeBuilder.Emit(OpCodes.Ldarg_2);
            writeBuilder.Emit(OpCodes.Castclass, valueType);
            writeBuilder.Emit(OpCodes.Stloc, typedLocal);

            writeBuilder.Emit(OpCodes.Ldloc, typedLocal);
            writeBuilder.EmitCall(OpCodes.Callvirt, RpcParams.TokenProperty.GetMethod!, null);
            writeBuilder.Emit(OpCodes.Stloc, strLocal);
            writeBuilder.Emit(OpCodes.Ldloc, strLocal);
            writeBuilder.Emit(OpCodes.Brfalse_S, skipTokenLabel);

            writeBuilder.Emit(OpCodes.Ldarg_1);
            writeBuilder.Emit(OpCodes.Ldloc, strLocal);
            writeBuilder.EmitCall(OpCodes.Callvirt, WriteStringValue, null);

            writeBuilder.MarkLabel(skipTokenLabel);
            foreach (var p in properties)
            {
                if (p == RpcParams.TokenProperty)
                    continue;

                if (p.GetMethod is null)
                    continue;

                writeBuilder.Emit(OpCodes.Ldarg_3);
                writeBuilder.Emit(OpCodes.Ldtoken, p.PropertyType);
                writeBuilder.EmitCall(OpCodes.Call, GetTypeFromHandle, null);
                writeBuilder.EmitCall(OpCodes.Callvirt, GetConverter, null);

                writeBuilder.Emit(OpCodes.Ldarg_1);
                writeBuilder.Emit(OpCodes.Ldloc, typedLocal);
                writeBuilder.EmitCall(OpCodes.Callvirt, p.GetMethod, null);

                if (!p.PropertyType.IsValueType)
                {
                    writeBuilder.Emit(OpCodes.Dup);
                    writeBuilder.Emit(OpCodes.Brfalse_S, cleanStackLabel);
                }
                else if (Nullable.GetUnderlyingType(p.PropertyType) is object)
                {
                    writeBuilder.Emit(OpCodes.Dup);
                    var local = writeBuilder.DeclareLocal(p.PropertyType);
                    writeBuilder.Emit(OpCodes.Stloc, local);
                    writeBuilder.Emit(OpCodes.Ldloca, local);
                    writeBuilder.EmitCall(OpCodes.Call, p.PropertyType.GetProperty(nameof(Nullable<int>.HasValue))!.GetMethod!, null);
                    writeBuilder.Emit(OpCodes.Brfalse_S, cleanStackLabel);
                }

                writeBuilder.Emit(OpCodes.Ldarg_3);
                writeBuilder.EmitCall(OpCodes.Callvirt, typeof(JsonConverter<>).MakeGenericType(p.PropertyType).GetMethod(nameof(JsonConverter<RpcParams>.Write))!, null);
            }
            writeBuilder.Emit(OpCodes.Br_S, endLabel);
            writeBuilder.MarkLabel(cleanStackLabel);
            writeBuilder.Emit(OpCodes.Pop);
            writeBuilder.Emit(OpCodes.Pop);
            writeBuilder.Emit(OpCodes.Pop);
            writeBuilder.MarkLabel(endLabel);
            writeBuilder.Emit(OpCodes.Ldarg_1);
            writeBuilder.EmitCall(OpCodes.Callvirt, WriteEndArray, null);
            writeBuilder.Emit(OpCodes.Ret);

            return (WriteAction)writeMethod.CreateDelegate(typeof(WriteAction), new JsonConverterClosure());
        }
    }
}

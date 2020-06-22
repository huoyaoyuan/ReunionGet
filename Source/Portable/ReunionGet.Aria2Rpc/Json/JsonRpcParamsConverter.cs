﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json
{
    using WriteAction = Action<Utf8JsonWriter, JsonRpcParams, JsonSerializerOptions>;

    internal class JsonRpcParamsConverter : JsonConverterFactory
    {
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => new JsonRpcParamsConverterImpl();

        public override bool CanConvert(Type typeToConvert) => typeof(JsonRpcParams).IsAssignableFrom(typeToConvert);
    }

    internal class JsonRpcParamsConverterImpl : JsonConverter<JsonRpcParams>
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
        public override JsonRpcParams Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("Json rpc params are only meant to be serialized.");

        public override void Write(Utf8JsonWriter writer, JsonRpcParams value, JsonSerializerOptions options)
        {
            var valueType = value.GetType();

            if (!_writeDelegates.TryGetValue(valueType, out var writeDelegate))
            {
                writeDelegate = CreateWriteDelegate(valueType);
                _writeDelegates[valueType] = writeDelegate;
            }

            writeDelegate(writer, value, options);
        }

        private static WriteAction CreateWriteDelegate(Type valueType)
        {
            var properties = valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var writeMethod = new DynamicMethod("WriteDelegate", typeof(void),
                new[] { typeof(Utf8JsonWriter), typeof(JsonRpcParams), typeof(JsonSerializerOptions) });
            var writeBuilder = writeMethod.GetILGenerator();

            var jmpLabel = writeBuilder.DefineLabel();
            var typedLocal = writeBuilder.DeclareLocal(valueType);
            var strLocal = writeBuilder.DeclareLocal(typeof(string));

            writeBuilder.Emit(OpCodes.Ldarg_0);
            writeBuilder.EmitCall(OpCodes.Callvirt, WriteStartArray, null);

            writeBuilder.Emit(OpCodes.Ldarg_1);
            writeBuilder.Emit(OpCodes.Castclass, valueType);
            writeBuilder.Emit(OpCodes.Stloc, typedLocal);

            writeBuilder.Emit(OpCodes.Ldloc, typedLocal);
            writeBuilder.EmitCall(OpCodes.Callvirt, JsonRpcParams.TokenProperty.GetMethod!, null);
            writeBuilder.Emit(OpCodes.Stloc, strLocal);
            writeBuilder.Emit(OpCodes.Ldloc, strLocal);
            writeBuilder.Emit(OpCodes.Brfalse_S, jmpLabel);

            writeBuilder.Emit(OpCodes.Ldarg_0);
            writeBuilder.Emit(OpCodes.Ldloc, strLocal);
            writeBuilder.EmitCall(OpCodes.Callvirt, WriteStringValue, null);

            writeBuilder.MarkLabel(jmpLabel);
            foreach (var p in properties)
            {
                if (p == JsonRpcParams.TokenProperty)
                    continue;

                if (p.GetMethod is null)
                    continue;

                writeBuilder.Emit(OpCodes.Ldarg_2);
                writeBuilder.Emit(OpCodes.Ldtoken, p.PropertyType);
                writeBuilder.EmitCall(OpCodes.Call, GetTypeFromHandle, null);
                writeBuilder.EmitCall(OpCodes.Callvirt, GetConverter, null);

                writeBuilder.Emit(OpCodes.Ldarg_0);
                writeBuilder.Emit(OpCodes.Ldloc, typedLocal);
                writeBuilder.EmitCall(OpCodes.Callvirt, p.GetMethod, null);
                writeBuilder.Emit(OpCodes.Ldarg_2);
                writeBuilder.EmitCall(OpCodes.Callvirt, typeof(JsonConverter<>).MakeGenericType(p.PropertyType).GetMethod(nameof(JsonConverter<JsonRpcParams>.Write))!, null);
            }
            writeBuilder.Emit(OpCodes.Ldarg_0);
            writeBuilder.EmitCall(OpCodes.Callvirt, WriteEndArray, null);
            writeBuilder.Emit(OpCodes.Ret);

            return (WriteAction)writeMethod.CreateDelegate(typeof(WriteAction));
        }
    }
}

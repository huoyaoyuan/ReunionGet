using System;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReunionGet.Aria2Rpc.Json
{
    public class ValueTupleToArrayConverter : JsonConverterFactory
    {
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => (JsonConverter)Activator.CreateInstance(typeof(ValueTupleConverter<>).MakeGenericType(typeToConvert))!;

        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
                return false;

            var definition = typeToConvert.GetGenericTypeDefinition();

            return definition == typeof(ValueTuple<>)
                || definition == typeof(ValueTuple<,>)
                || definition == typeof(ValueTuple<,,>)
                || definition == typeof(ValueTuple<,,,>)
                || definition == typeof(ValueTuple<,,,,>)
                || definition == typeof(ValueTuple<,,,,,>)
                || definition == typeof(ValueTuple<,,,,,,>)
                || definition == typeof(ValueTuple<,,,,,,,>);
        }
    }

#pragma warning disable CA1812 // Instantialized by reflection
    internal class ValueTupleConverter<T> : JsonConverter<T>
#pragma warning restore CA1812
    {
        private delegate T ReadDelegate(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);
        private delegate void WriteDelegate(Utf8JsonWriter writer, T value, JsonSerializerOptions options);

        private readonly ReadDelegate _readDelegate;
        private readonly WriteDelegate _writeDelegate;

        public ValueTupleConverter()
        {
            var fields = typeof(T).GetFields();

            var getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))
                ?? throw new NotSupportedException($"Cannot get method handle of {nameof(Type)}.{nameof(Type.GetTypeFromHandle)}");
            var getConverter = typeof(JsonSerializerOptions).GetMethod(nameof(JsonSerializerOptions.GetConverter))
                ?? throw new NotSupportedException($"Cannot get method handle of {nameof(JsonSerializerOptions)}.{nameof(JsonSerializerOptions.GetConverter)}");
            var readerRead = typeof(Utf8JsonReader).GetMethod(nameof(Utf8JsonReader.Read))
                ?? throw new NotSupportedException($"Cannot get method handle of {nameof(Utf8JsonReader)}.{nameof(Utf8JsonReader.Read)}");
            var writeStartArray = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteStartArray), Array.Empty<Type>())
                ?? throw new NotSupportedException($"Cannot get method handle of {nameof(Utf8JsonWriter)}.{nameof(Utf8JsonWriter.WriteStartArray)}");
            var writeEndArray = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteEndArray))
                ?? throw new NotSupportedException($"Cannot get method handle of {nameof(Utf8JsonWriter)}.{nameof(Utf8JsonWriter.WriteEndArray)}");

            var readMethod = new DynamicMethod("ReadDelegate", typeof(T),
                new[] { typeof(Utf8JsonReader).MakeByRefType(), typeof(Type), typeof(JsonSerializerOptions) });
            var readBuilder = readMethod.GetILGenerator();

            readBuilder.Emit(OpCodes.Ldarg_0);
            readBuilder.EmitCall(OpCodes.Call, readerRead, null);
            readBuilder.Emit(OpCodes.Pop);
            foreach (var f in fields)
            {
                readBuilder.Emit(OpCodes.Ldarg_2);
                readBuilder.Emit(OpCodes.Ldtoken, f.FieldType);
                readBuilder.EmitCall(OpCodes.Call, getTypeFromHandle, null);
                readBuilder.EmitCall(OpCodes.Callvirt, getConverter, null);

                readBuilder.Emit(OpCodes.Ldarg_0);
                readBuilder.Emit(OpCodes.Ldtoken, f.FieldType);
                readBuilder.EmitCall(OpCodes.Call, getTypeFromHandle, null);
                readBuilder.Emit(OpCodes.Ldarg_2);
                readBuilder.EmitCall(OpCodes.Callvirt, typeof(JsonConverter<>).MakeGenericType(f.FieldType).GetMethod(nameof(JsonConverter<T>.Read))!, null);

                readBuilder.Emit(OpCodes.Ldarg_0);
                readBuilder.EmitCall(OpCodes.Call, readerRead, null);
                readBuilder.Emit(OpCodes.Pop);
            }
            readBuilder.Emit(OpCodes.Newobj, typeof(T).GetConstructors()[0]);
            readBuilder.Emit(OpCodes.Ret);
            _readDelegate = (ReadDelegate)readMethod.CreateDelegate(typeof(ReadDelegate));


            var writeMethod = new DynamicMethod("WriteDelegate", typeof(void),
                new[] { typeof(Utf8JsonWriter), typeof(T), typeof(JsonSerializerOptions) },
                typeof(ValueTupleConverter<>).Module);
            var writeBuilder = writeMethod.GetILGenerator();

            writeBuilder.Emit(OpCodes.Ldarg_0);
            writeBuilder.EmitCall(OpCodes.Callvirt, writeStartArray, null);
            foreach (var f in fields)
            {
                writeBuilder.Emit(OpCodes.Ldarg_2);
                writeBuilder.Emit(OpCodes.Ldtoken, f.FieldType);
                writeBuilder.EmitCall(OpCodes.Call, getTypeFromHandle, null);
                writeBuilder.EmitCall(OpCodes.Callvirt, getConverter, null);

                writeBuilder.Emit(OpCodes.Ldarg_0);
                writeBuilder.Emit(OpCodes.Ldarg_1);
                writeBuilder.Emit(OpCodes.Ldfld, f);
                writeBuilder.Emit(OpCodes.Ldarg_2);
                writeBuilder.EmitCall(OpCodes.Callvirt, typeof(JsonConverter<>).MakeGenericType(f.FieldType).GetMethod(nameof(JsonConverter<T>.Write))!, null);
            }
            writeBuilder.Emit(OpCodes.Ldarg_0);
            writeBuilder.EmitCall(OpCodes.Callvirt, writeEndArray, null);
            writeBuilder.Emit(OpCodes.Ret);

            _writeDelegate = (WriteDelegate)writeMethod.CreateDelegate(typeof(WriteDelegate));
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => _readDelegate(ref reader, typeToConvert, options);

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => _writeDelegate(writer, value, options);
    }
}

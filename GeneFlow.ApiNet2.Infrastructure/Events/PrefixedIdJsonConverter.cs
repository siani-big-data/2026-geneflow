using System.Text.Json;
using System.Text.Json.Serialization;
using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Infrastructure.Events;

/// <summary>
/// Serializes any <see cref="PrefixedId{TId}"/> subtype as its canonical string form
/// (e.g. <c>"U00000004"</c>) instead of the default object shape <c>{ "value": 4 }</c>
/// produced by System.Text.Json.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="RedisEventBusPublisher"/> so the activity projector — and any
/// downstream consumer reading raw event payloads — can match identifiers against the
/// same string the rest of the platform uses (database columns, repositories, logs,
/// handler queries).
/// </para>
/// <para>
/// This converter only writes. The publisher never deserializes its own payloads, so
/// <see cref="JsonConverter{T}.Read(ref Utf8JsonReader, Type, JsonSerializerOptions)"/>
/// is intentionally a no-op.
/// </para>
/// </remarks>
public sealed class PrefixedIdJsonConverter : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        var t = typeToConvert;
        while (t is not null && t != typeof(object))
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(PrefixedId<>))
            {
                return true;
            }
            t = t.BaseType;
        }
        return false;
    }

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(PrefixedIdStringConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }

    private sealed class PrefixedIdStringConverter<TId> : JsonConverter<TId>
        where TId : class
    {
        public override TId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Event payloads are never deserialized back into domain types by the
            // publisher. Skip without consuming to keep the reader positioned correctly
            // for any framework code that may attempt this path.
            reader.Skip();
            return null;
        }

        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.ToString());
        }
    }
}

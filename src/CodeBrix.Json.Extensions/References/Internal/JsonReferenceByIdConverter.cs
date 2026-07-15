using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBrix.Json.Extensions.References.Internal;

/// <summary>
/// A member-level converter that writes a referenceable entity as just its
/// <see cref="IJsonReferenceable{TId}.JsonReferenceId"/> and, on read, resolves that identifier back to the
/// live instance via the operation's <see cref="JsonReferenceRegistry"/>. Installed on
/// <see cref="JsonReferenceByIdAttribute"/> members by <see cref="ReferenceByIdModifier"/>.
/// </summary>
/// <typeparam name="TEntity">The referenceable entity type of the member.</typeparam>
/// <typeparam name="TId">The identifier type exposed by <typeparamref name="TEntity"/>.</typeparam>
internal sealed class JsonReferenceByIdConverter<TEntity, TId> : JsonConverter<TEntity>
    where TEntity : class
{
    private readonly JsonReferenceRegistry _registry;

    internal JsonReferenceByIdConverter(JsonReferenceRegistry registry)
    {
        _registry = registry;
    }

    public override TEntity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var id = JsonSerializer.Deserialize<TId>(ref reader, options);

        if (id == null)
        {
            return null;
        }

        if (_registry != null && _registry.TryResolve(typeof(TEntity), id, out var entity))
        {
            return (TEntity)entity;
        }

        throw new JsonException(
            $"Could not resolve the [JsonReferenceById] identifier '{id}' to a registered '{typeof(TEntity)}' " +
            "instance. Register the target before deserializing (two-phase apply), or use " +
            "JsonReferenceRegistry.ResolveOrDefer for forward references.");
    }

    public override void Write(Utf8JsonWriter writer, TEntity value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();

            return;
        }

        var id = ((IJsonReferenceable<TId>)value).JsonReferenceId;
        JsonSerializer.Serialize(writer, id, options);
    }
}

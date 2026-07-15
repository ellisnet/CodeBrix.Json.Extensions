using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeBrix.Json.Extensions.Polymorphism.Internal;

namespace CodeBrix.Json.Extensions.References.Internal;

/// <summary>
/// A reference-aware <see cref="JsonConverter{T}"/> for a type marked with
/// <see cref="JsonReferenceableAttribute"/>. It writes the first occurrence of an instance with a
/// <c>"$id"</c> and later occurrences as <c>{ "$ref": "&lt;id&gt;" }</c>, and restores object identity
/// (including cycles) on read by creating and registering the instance before populating its members.
/// <para>
/// When <typeparamref name="T"/> also declares a discriminator (via the polymorphism attributes), the
/// concrete type is resolved through the shared <see cref="DiscriminatorMap"/> before the object contract is
/// read, so referenceable and polymorphic behavior compose on a single node.
/// </para>
/// </summary>
/// <typeparam name="T">The referenceable base or concrete type.</typeparam>
internal sealed class ReferenceAwareConverter<T> : JsonConverter<T>
    where T : class
{
    private const string IdPropertyName = "$id";
    private const string RefPropertyName = "$ref";

    private readonly ReferenceScope _scope;
    private readonly JsonSerializerOptions _metadataOptions;

    internal ReferenceAwareConverter(ReferenceScope scope, JsonSerializerOptions metadataOptions)
    {
        _scope = scope;
        _metadataOptions = metadataOptions;
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                $"Expected a JSON object to deserialize referenceable type '{typeof(T)}', " +
                $"but found {root.ValueKind}.");
        }

        if (root.TryGetProperty(RefPropertyName, out var refElement))
        {
            var referenceId = refElement.GetString();

            if (referenceId != null && _scope.TryGetReadObject(referenceId, out var existing))
            {
                return (T)existing;
            }

            throw new JsonException(
                $"Encountered a \"{RefPropertyName}\" of '{referenceId}' that does not match any earlier " +
                $"\"{IdPropertyName}\" while deserializing '{typeof(T)}'.");
        }

        var concreteType = DiscriminatorMap.HasDiscriminator(typeof(T))
            ? DiscriminatorMap.Get(typeof(T)).ResolveTargetType(root, options)
            : typeof(T);

        var typeInfo = _metadataOptions.GetTypeInfo(concreteType);

        if (typeInfo.CreateObject == null)
        {
            throw new JsonException(
                $"Cannot construct referenceable type '{concreteType}': System.Text.Json found no usable " +
                "parameterless constructor. Reference-preserving types must be creatable and expose settable " +
                "members so that cycles can be restored.");
        }

        var instance = typeInfo.CreateObject();

        if (root.TryGetProperty(IdPropertyName, out var idElement))
        {
            var id = idElement.GetString();

            if (id != null)
            {
                _scope.AddForRead(id, instance);
            }
        }

        foreach (var property in typeInfo.Properties)
        {
            if (property.Set == null)
            {
                continue;
            }

            if (!TryGetMember(root, property.Name, options, out var valueElement))
            {
                continue;
            }

            var value = valueElement.Deserialize(property.PropertyType, options);
            property.Set(instance, value);
        }

        return (T)instance;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();

            return;
        }

        if (_scope.TryGetWriteId(value, out var existingId))
        {
            writer.WriteStartObject();
            writer.WriteString(RefPropertyName, existingId);
            writer.WriteEndObject();

            return;
        }

        var runtimeType = value.GetType();

        if (DiscriminatorMap.HasDiscriminator(typeof(T)) && runtimeType == typeof(T))
        {
            throw new JsonException(
                $"Cannot serialize an instance whose runtime type is the polymorphic base type '{typeof(T)}' " +
                "itself. Serialize a derived type instead (for example an 'UnknownXyz' subclass).");
        }

        var id = _scope.AddForWrite(value);

        writer.WriteStartObject();
        writer.WriteString(IdPropertyName, id);

        var typeInfo = _metadataOptions.GetTypeInfo(runtimeType);

        foreach (var property in typeInfo.Properties)
        {
            if (property.Get == null)
            {
                continue;
            }

            var propertyValue = property.Get(value);

            if (property.ShouldSerialize != null && !property.ShouldSerialize(value, propertyValue))
            {
                continue;
            }

            writer.WritePropertyName(property.Name);
            JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
        }

        writer.WriteEndObject();
    }

    private static bool TryGetMember(JsonElement root, string name, JsonSerializerOptions options, out JsonElement value)
    {
        if (root.TryGetProperty(name, out value))
        {
            return true;
        }

        if (options.PropertyNameCaseInsensitive)
        {
            foreach (var candidate in root.EnumerateObject())
            {
                if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = candidate.Value;

                    return true;
                }
            }
        }

        value = default;

        return false;
    }
}

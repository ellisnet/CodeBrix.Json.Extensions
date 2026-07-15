using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeBrix.Json.Extensions.Polymorphism.Internal;

namespace CodeBrix.Json.Extensions.Polymorphism;

/// <summary>
/// A <see cref="JsonConverter{T}"/> that deserializes a polymorphic base class or interface by reading the
/// discriminator property declared with <see cref="JsonDiscriminatorAttribute"/> on <typeparamref name="T"/>,
/// then dispatching to the concrete type mapped by a matching <see cref="JsonKnownTypeAttribute"/> — or to
/// the <see cref="JsonFallbackTypeAttribute"/> type when the discriminator is missing or unmatched.
/// <para>
/// Instances are normally created for you by <see cref="FallbackTypeConverterFactory"/>; apply the factory
/// with <c>[JsonConverter(typeof(FallbackTypeConverterFactory))]</c> on the base type.
/// </para>
/// </summary>
/// <typeparam name="T">
/// The polymorphic base class or interface. It must declare <see cref="JsonDiscriminatorAttribute"/>.
/// </typeparam>
public class FallbackTypeConverter<T> : JsonConverter<T>
    where T : class
{
    private DiscriminatorMap _map;

    private DiscriminatorMap Map => _map ??= DiscriminatorMap.Get(typeof(T));

    /// <summary>
    /// Reads a JSON object, resolves the concrete type from its discriminator property, and deserializes
    /// the object as that concrete type.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <param name="typeToConvert">The declared type being converted (always <typeparamref name="T"/>).</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The deserialized instance.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the value is not a JSON object, or when the discriminator is missing or unmatched and no
    /// fallback type is declared.
    /// </exception>
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                $"Expected a JSON object to deserialize polymorphic type '{typeof(T)}', " +
                $"but found {root.ValueKind}.");
        }

        var targetType = Map.ResolveTargetType(root, options);

        return (T)root.Deserialize(targetType, options);
    }

    /// <summary>
    /// Serializes <paramref name="value"/> using its runtime type's normal serialization contract.
    /// </summary>
    /// <param name="writer">The writer to serialize to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active serializer options.</param>
    /// <exception cref="JsonException">
    /// Thrown when the runtime type of <paramref name="value"/> is exactly <typeparamref name="T"/> —
    /// serializing the discriminator-carrying base type itself would recurse endlessly, so only derived
    /// types can be serialized.
    /// </exception>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var runtimeType = value.GetType();

        if (runtimeType == typeof(T))
        {
            throw new JsonException(
                $"Cannot serialize an instance whose runtime type is the polymorphic base type '{typeof(T)}' " +
                "itself. Serialize a derived type instead (for example an 'UnknownXyz' subclass).");
        }

        JsonSerializer.Serialize(writer, value, runtimeType, options);
    }
}

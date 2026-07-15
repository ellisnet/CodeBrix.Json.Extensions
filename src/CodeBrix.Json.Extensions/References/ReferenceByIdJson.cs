using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using CodeBrix.Json.Extensions.References.Internal;

namespace CodeBrix.Json.Extensions.References;

/// <summary>
/// Serializes and deserializes graphs that reference shared entities by identifier through members marked
/// with <see cref="JsonReferenceByIdAttribute"/>. On write each such member is emitted as just its
/// <see cref="IJsonReferenceable{TId}.JsonReferenceId"/>; on read the identifier is resolved back to a live
/// instance supplied through a <see cref="JsonReferenceRegistry"/>.
/// <para>
/// This is the explicit, stable-id counterpart to the <c>$id</c>/<c>$ref</c> handling in
/// <see cref="ReferenceJson"/>. Deserialization follows a "two-phase apply": populate the registry with the
/// authoritative entities (the owning collections) first, then deserialize the referencing graph. The
/// supplied <see cref="JsonSerializerOptions"/> is treated as a settings template and copied, so it is never
/// mutated.
/// </para>
/// </summary>
public static class ReferenceByIdJson
{
    /// <summary>
    /// Serializes <paramref name="value"/> to a JSON string, writing each <see cref="JsonReferenceByIdAttribute"/>
    /// member as its identifier only.
    /// </summary>
    /// <typeparam name="TValue">The declared type of the value being serialized.</typeparam>
    /// <param name="value">The value (or graph root) to serialize.</param>
    /// <param name="options">An optional settings template; a private copy is used for the operation.</param>
    /// <returns>The JSON representation of <paramref name="value"/>.</returns>
    public static string Serialize<TValue>(TValue value, JsonSerializerOptions options = null)
        => JsonSerializer.Serialize(value, BuildOptions(null, options));

    /// <summary>
    /// Serializes <paramref name="value"/> to a UTF-8 byte array, writing each
    /// <see cref="JsonReferenceByIdAttribute"/> member as its identifier only.
    /// </summary>
    /// <typeparam name="TValue">The declared type of the value being serialized.</typeparam>
    /// <param name="value">The value (or graph root) to serialize.</param>
    /// <param name="options">An optional settings template; a private copy is used for the operation.</param>
    /// <returns>The UTF-8 encoded JSON representation of <paramref name="value"/>.</returns>
    public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, JsonSerializerOptions options = null)
        => JsonSerializer.SerializeToUtf8Bytes(value, BuildOptions(null, options));

    /// <summary>
    /// Deserializes <paramref name="json"/> to <typeparamref name="TValue"/>, resolving each
    /// <see cref="JsonReferenceByIdAttribute"/> member against <paramref name="registry"/>.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize into.</typeparam>
    /// <param name="json">The JSON to deserialize.</param>
    /// <param name="registry">The registry the referenced entities were registered in.</param>
    /// <param name="options">An optional settings template; a private copy is used for the operation.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="registry"/> is <c>null</c>.</exception>
    public static TValue Deserialize<TValue>(string json, JsonReferenceRegistry registry,
        JsonSerializerOptions options = null)
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        return JsonSerializer.Deserialize<TValue>(json, BuildOptions(registry, options));
    }

    /// <summary>
    /// Deserializes UTF-8 encoded <paramref name="utf8Json"/> to <typeparamref name="TValue"/>, resolving each
    /// <see cref="JsonReferenceByIdAttribute"/> member against <paramref name="registry"/>.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize into.</typeparam>
    /// <param name="utf8Json">The UTF-8 encoded JSON to deserialize.</param>
    /// <param name="registry">The registry the referenced entities were registered in.</param>
    /// <param name="options">An optional settings template; a private copy is used for the operation.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="registry"/> is <c>null</c>.</exception>
    public static TValue Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonReferenceRegistry registry,
        JsonSerializerOptions options = null)
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        return JsonSerializer.Deserialize<TValue>(utf8Json, BuildOptions(registry, options));
    }

    private static JsonSerializerOptions BuildOptions(JsonReferenceRegistry registry, JsonSerializerOptions baseOptions)
    {
        var options = baseOptions == null
            ? new JsonSerializerOptions()
            : new JsonSerializerOptions(baseOptions);

        IJsonTypeInfoResolver baseResolver = options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
        options.TypeInfoResolver = baseResolver.WithAddedModifier(ReferenceByIdModifier.Create(registry));

        return options;
    }
}

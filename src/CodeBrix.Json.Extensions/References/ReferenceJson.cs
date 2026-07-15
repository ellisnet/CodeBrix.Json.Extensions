using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using CodeBrix.Json.Extensions.References.Internal;

namespace CodeBrix.Json.Extensions.References;

/// <summary>
/// Serializes and deserializes object graphs while preserving the identity of every instance whose type is
/// marked with <see cref="JsonReferenceableAttribute"/>. The first occurrence of a referenceable instance is
/// written with a <c>"$id"</c> and later occurrences as <c>{ "$ref": "&lt;id&gt;" }</c>; on read the
/// identifiers are restored to shared instances, so shared references and cycles round-trip.
/// <para>
/// Each call runs as one self-contained operation: a fresh reference scope is created and the supplied
/// <see cref="JsonSerializerOptions"/> (if any) is used only as a settings template — its naming policy,
/// case handling and converters are honored, but the reference machinery is added to a private copy, so the
/// template is never mutated and can be reused freely. Do not pass options previously returned by this type.
/// </para>
/// <para>
/// This is the reference-handling counterpart to the discriminator support in
/// <c>CodeBrix.Json.Extensions.Polymorphism</c>; the two compose, so a graph of referenceable, polymorphic
/// types round-trips through these methods.
/// </para>
/// </summary>
public static class ReferenceJson
{
    /// <summary>
    /// Serializes <paramref name="value"/> to a JSON string, preserving referenceable instance identity.
    /// </summary>
    /// <typeparam name="TValue">The declared type of the value being serialized.</typeparam>
    /// <param name="value">The value (or graph root) to serialize.</param>
    /// <param name="options">An optional settings template; a private copy is used for the operation.</param>
    /// <returns>The JSON representation of <paramref name="value"/>.</returns>
    public static string Serialize<TValue>(TValue value, JsonSerializerOptions options = null)
        => JsonSerializer.Serialize(value, BuildOperationOptions(options));

    /// <summary>
    /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to a JSON string, preserving
    /// referenceable instance identity.
    /// </summary>
    /// <param name="value">The value (or graph root) to serialize.</param>
    /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
    /// <param name="options">An optional settings template; a private copy is used for the operation.</param>
    /// <returns>The JSON representation of <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inputType"/> is <c>null</c>.</exception>
    public static string Serialize(object value, Type inputType, JsonSerializerOptions options = null)
    {
        if (inputType == null)
        {
            throw new ArgumentNullException(nameof(inputType));
        }

        return JsonSerializer.Serialize(value, inputType, BuildOperationOptions(options));
    }

    /// <summary>
    /// Serializes <paramref name="value"/> to a UTF-8 byte array, preserving referenceable instance identity.
    /// </summary>
    /// <typeparam name="TValue">The declared type of the value being serialized.</typeparam>
    /// <param name="value">The value (or graph root) to serialize.</param>
    /// <param name="options">An optional settings template; a private copy is used for the operation.</param>
    /// <returns>The UTF-8 encoded JSON representation of <paramref name="value"/>.</returns>
    public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, JsonSerializerOptions options = null)
        => JsonSerializer.SerializeToUtf8Bytes(value, BuildOperationOptions(options));

    /// <summary>
    /// Deserializes <paramref name="json"/> to <typeparamref name="TValue"/>, restoring referenceable
    /// instance identity (including shared references and cycles).
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize into.</typeparam>
    /// <param name="json">The JSON to deserialize.</param>
    /// <param name="options">An optional settings template; a private copy is used for the operation.</param>
    /// <returns>The deserialized value.</returns>
    public static TValue Deserialize<TValue>(string json, JsonSerializerOptions options = null)
        => JsonSerializer.Deserialize<TValue>(json, BuildOperationOptions(options));

    /// <summary>
    /// Deserializes UTF-8 encoded <paramref name="utf8Json"/> to <typeparamref name="TValue"/>, restoring
    /// referenceable instance identity (including shared references and cycles).
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize into.</typeparam>
    /// <param name="utf8Json">The UTF-8 encoded JSON to deserialize.</param>
    /// <param name="options">An optional settings template; a private copy is used for the operation.</param>
    /// <returns>The deserialized value.</returns>
    public static TValue Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options = null)
        => JsonSerializer.Deserialize<TValue>(utf8Json, BuildOperationOptions(options));

    /// <summary>
    /// Deserializes <paramref name="json"/> to <paramref name="returnType"/>, restoring referenceable
    /// instance identity (including shared references and cycles).
    /// </summary>
    /// <param name="json">The JSON to deserialize.</param>
    /// <param name="returnType">The type to deserialize into.</param>
    /// <param name="options">An optional settings template; a private copy is used for the operation.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="returnType"/> is <c>null</c>.</exception>
    public static object Deserialize(string json, Type returnType, JsonSerializerOptions options = null)
    {
        if (returnType == null)
        {
            throw new ArgumentNullException(nameof(returnType));
        }

        return JsonSerializer.Deserialize(json, returnType, BuildOperationOptions(options));
    }

    private static JsonSerializerOptions BuildOperationOptions(JsonSerializerOptions baseOptions)
    {
        var scope = new ReferenceScope();
        var metadataOptions = baseOptions == null
            ? new JsonSerializerOptions()
            : new JsonSerializerOptions(baseOptions);

        // The metadata options are consulted directly via GetTypeInfo (never through JsonSerializer), so
        // their resolver is not auto-configured. Fall back to the reflection resolver when none was supplied.
        if (metadataOptions.TypeInfoResolver == null)
        {
            metadataOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        }

        var operation = baseOptions == null
            ? new JsonSerializerOptions()
            : new JsonSerializerOptions(baseOptions);

        operation.Converters.Add(new ReferenceAwareConverterFactory(scope, metadataOptions));

        return operation;
    }
}

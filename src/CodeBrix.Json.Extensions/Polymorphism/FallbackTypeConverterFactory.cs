using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBrix.Json.Extensions.Polymorphism;

/// <summary>
/// A <see cref="JsonConverterFactory"/> that creates <see cref="FallbackTypeConverter{T}"/> instances for
/// classes and interfaces that declare <see cref="JsonDiscriminatorAttribute"/>.
/// <para>
/// Apply with <c>[JsonConverter(typeof(FallbackTypeConverterFactory))]</c> on each polymorphic base type,
/// or add one instance to <c>JsonSerializerOptions.Converters</c> to cover every attributed type at once.
/// </para>
/// </summary>
public class FallbackTypeConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether <paramref name="typeToConvert"/> is a class or interface that declares
    /// <see cref="JsonDiscriminatorAttribute"/> (directly, not inherited).
    /// </summary>
    /// <param name="typeToConvert">The type being considered for conversion.</param>
    /// <returns><c>true</c> when this factory can create a converter for the type.</returns>
    public override bool CanConvert(Type typeToConvert)
        => !typeToConvert.IsValueType
           && Attribute.IsDefined(typeToConvert, typeof(JsonDiscriminatorAttribute), inherit: false);

    /// <summary>
    /// Creates the <see cref="FallbackTypeConverter{T}"/> for <paramref name="typeToConvert"/>.
    /// </summary>
    /// <param name="typeToConvert">The polymorphic base class or interface.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The created converter.</returns>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(
            typeof(FallbackTypeConverter<>).MakeGenericType(typeToConvert));
}

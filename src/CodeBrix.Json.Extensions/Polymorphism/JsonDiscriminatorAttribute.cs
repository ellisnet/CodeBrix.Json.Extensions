using System;

namespace CodeBrix.Json.Extensions.Polymorphism;

/// <summary>
/// Declares that a class or interface is deserialized polymorphically by inspecting the value of a
/// discriminator property in the incoming JSON object. The discriminator value is matched against the
/// mappings declared with <see cref="JsonKnownTypeAttribute"/>; when no mapping matches, the type declared
/// with <see cref="JsonFallbackTypeAttribute"/> (if any) is used instead.
/// <para>
/// Apply this attribute together with <c>[JsonConverter(typeof(FallbackTypeConverterFactory))]</c> (or by
/// adding a <see cref="FallbackTypeConverterFactory"/> instance to <c>JsonSerializerOptions.Converters</c>)
/// on the polymorphic base class or interface.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public sealed class JsonDiscriminatorAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="JsonDiscriminatorAttribute"/>.
    /// </summary>
    /// <param name="propertyName">
    /// The JSON property name (exactly as it appears in the JSON payload) whose value selects the concrete
    /// type to deserialize.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="propertyName"/> is <c>null</c>, empty, or white-space.
    /// </exception>
    public JsonDiscriminatorAttribute(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("The discriminator property name must be a non-empty string.",
                nameof(propertyName));
        }

        PropertyName = propertyName;
    }

    /// <summary>
    /// The JSON property name whose value selects the concrete type to deserialize.
    /// </summary>
    public string PropertyName { get; }
}

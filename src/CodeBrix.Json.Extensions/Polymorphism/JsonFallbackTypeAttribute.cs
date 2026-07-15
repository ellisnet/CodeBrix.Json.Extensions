using System;

namespace CodeBrix.Json.Extensions.Polymorphism;

/// <summary>
/// Declares the catch-all type to instantiate when a JSON object is deserialized as the attributed base
/// class or interface, but its discriminator property is missing, is <c>null</c>, or holds a value with no
/// matching <see cref="JsonKnownTypeAttribute"/> mapping. Apply alongside
/// <see cref="JsonDiscriminatorAttribute"/>.
/// <para>
/// When no fallback type is declared, an unmatched discriminator causes a
/// <see cref="System.Text.Json.JsonException"/> instead.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public sealed class JsonFallbackTypeAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="JsonFallbackTypeAttribute"/>.
    /// </summary>
    /// <param name="fallbackType">
    /// The type to deserialize when the discriminator does not match any known type. It must be assignable
    /// to the attributed base type, and must not be the attributed base type itself.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fallbackType"/> is <c>null</c>.</exception>
    public JsonFallbackTypeAttribute(Type fallbackType)
    {
        FallbackType = fallbackType ?? throw new ArgumentNullException(nameof(fallbackType));
    }

    /// <summary>
    /// The type to deserialize when the discriminator does not match any known type.
    /// </summary>
    public Type FallbackType { get; }
}

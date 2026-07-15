using System;

namespace CodeBrix.Json.Extensions.Polymorphism;

/// <summary>
/// Maps one discriminator value to the concrete type that should be instantiated when a JSON object with
/// that discriminator value is deserialized as the attributed base class or interface. Apply once per
/// known discriminator value, alongside <see cref="JsonDiscriminatorAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public sealed class JsonKnownTypeAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="JsonKnownTypeAttribute"/>.
    /// </summary>
    /// <param name="knownType">
    /// The type to deserialize when the discriminator property equals <paramref name="discriminatorValue"/>.
    /// It must be assignable to the attributed base type, and must not be the attributed base type itself.
    /// </param>
    /// <param name="discriminatorValue">
    /// The discriminator value (compared ordinally against the JSON string value of the discriminator
    /// property) that selects <paramref name="knownType"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="knownType"/> or <paramref name="discriminatorValue"/> is <c>null</c>.
    /// </exception>
    public JsonKnownTypeAttribute(Type knownType, string discriminatorValue)
    {
        KnownType = knownType ?? throw new ArgumentNullException(nameof(knownType));
        DiscriminatorValue = discriminatorValue ?? throw new ArgumentNullException(nameof(discriminatorValue));
    }

    /// <summary>
    /// The type to deserialize when the discriminator property equals <see cref="DiscriminatorValue"/>.
    /// </summary>
    public Type KnownType { get; }

    /// <summary>
    /// The discriminator value that selects <see cref="KnownType"/>.
    /// </summary>
    public string DiscriminatorValue { get; }
}

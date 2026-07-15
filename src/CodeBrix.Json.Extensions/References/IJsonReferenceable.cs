namespace CodeBrix.Json.Extensions.References;

/// <summary>
/// Implemented by an entity that exposes a stable identifier of type <typeparamref name="TId"/>. Members
/// annotated with <see cref="JsonReferenceByIdAttribute"/> are serialized as just this identifier and
/// resolved back to the live instance through a <see cref="JsonReferenceRegistry"/> on read.
/// </summary>
/// <typeparam name="TId">
/// The identifier type (for example <see cref="string"/>, <see cref="System.Guid"/>, or an integer).
/// </typeparam>
public interface IJsonReferenceable<out TId>
{
    /// <summary>
    /// The stable identifier that uniquely identifies this entity within its <see cref="JsonReferenceRegistry"/>.
    /// </summary>
    TId JsonReferenceId { get; }
}

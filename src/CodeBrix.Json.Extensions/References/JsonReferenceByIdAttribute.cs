using System;

namespace CodeBrix.Json.Extensions.References;

/// <summary>
/// Marks a property or field whose value is a shared entity that should be serialized as <b>just its
/// identifier</b> (see <see cref="IJsonReferenceable{TId}"/>) rather than inlined, and resolved back to the
/// live instance on read through a <see cref="JsonReferenceRegistry"/>.
/// <para>
/// This is the explicit, human-readable alternative to the <c>$id</c>/<c>$ref</c> handling of
/// <see cref="JsonReferenceableAttribute"/>: use it when the referenced entity already has a stable
/// identifier and its authoritative copy lives elsewhere (an owning collection). The attribute takes effect
/// only when the graph is (de)serialized through <see cref="ReferenceByIdJson"/> (or options configured by
/// it); a plain <c>JsonSerializer</c> call ignores it and inlines the member as usual.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public sealed class JsonReferenceByIdAttribute : Attribute
{
}

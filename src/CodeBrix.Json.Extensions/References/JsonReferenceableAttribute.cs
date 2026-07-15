using System;

namespace CodeBrix.Json.Extensions.References;

/// <summary>
/// Marks a class or interface as reference-tracked. When a graph is serialized through
/// <see cref="ReferenceJson"/>, the first occurrence of each instance of an attributed type is written with
/// a <c>"$id"</c> and any later occurrence of that same instance is written as
/// <c>{ "$ref": "&lt;id&gt;" }</c>; on read the identifiers are resolved back to a single shared instance,
/// preserving object identity and cycles.
/// <para>
/// This is the System.Text.Json analog of Newtonsoft.Json's <c>[JsonObject(IsReference = true)]</c>, but
/// opt-in per type (like Newtonsoft) rather than global (like <c>ReferenceHandler.Preserve</c>). Apply it to
/// every type whose identity should be preserved; it composes with the polymorphism attributes, so a type
/// may be both referenceable and discriminated.
/// </para>
/// <para>
/// A referenceable type must be constructible by System.Text.Json (a usable parameterless constructor) and
/// expose settable members, because cycles are restored by creating the instance first and populating it
/// afterwards.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public sealed class JsonReferenceableAttribute : Attribute
{
}

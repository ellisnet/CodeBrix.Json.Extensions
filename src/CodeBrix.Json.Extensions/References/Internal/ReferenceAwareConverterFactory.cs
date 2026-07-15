using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBrix.Json.Extensions.References.Internal;

/// <summary>
/// Creates <see cref="ReferenceAwareConverter{T}"/> instances for types that declare
/// <see cref="JsonReferenceableAttribute"/>. One factory is created per <see cref="ReferenceJson"/>
/// operation, capturing that operation's <see cref="ReferenceScope"/> and the metadata options used to read
/// object contracts, so every referenceable type encountered in the graph shares the same scope.
/// </summary>
internal sealed class ReferenceAwareConverterFactory : JsonConverterFactory
{
    private readonly ReferenceScope _scope;
    private readonly JsonSerializerOptions _metadataOptions;

    internal ReferenceAwareConverterFactory(ReferenceScope scope, JsonSerializerOptions metadataOptions)
    {
        _scope = scope;
        _metadataOptions = metadataOptions;
    }

    public override bool CanConvert(Type typeToConvert)
        => !typeToConvert.IsValueType
           && Attribute.IsDefined(typeToConvert, typeof(JsonReferenceableAttribute), inherit: false);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(
            typeof(ReferenceAwareConverter<>).MakeGenericType(typeToConvert),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { _scope, _metadataOptions },
            culture: null);
}

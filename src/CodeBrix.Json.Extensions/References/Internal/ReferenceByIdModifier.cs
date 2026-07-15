using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace CodeBrix.Json.Extensions.References.Internal;

/// <summary>
/// Builds the <see cref="JsonTypeInfo"/> modifier that installs a
/// <see cref="JsonReferenceByIdConverter{TEntity, TId}"/> on every property or field annotated with
/// <see cref="JsonReferenceByIdAttribute"/>, wiring each to the supplied <see cref="JsonReferenceRegistry"/>.
/// </summary>
internal static class ReferenceByIdModifier
{
    internal static Action<JsonTypeInfo> Create(JsonReferenceRegistry registry)
        => typeInfo =>
        {
            foreach (var property in typeInfo.Properties)
            {
                if (property.AttributeProvider is not ICustomAttributeProvider provider
                    || !provider.IsDefined(typeof(JsonReferenceByIdAttribute), inherit: true))
                {
                    continue;
                }

                var entityType = property.PropertyType;
                var idType = FindReferenceIdType(entityType);

                if (idType == null)
                {
                    throw new JsonException(
                        $"The [JsonReferenceById] member '{typeInfo.Type}.{property.Name}' has type '{entityType}', " +
                        "which does not implement IJsonReferenceable<TId>.");
                }

                var converterType = typeof(JsonReferenceByIdConverter<,>).MakeGenericType(entityType, idType);

                property.CustomConverter = (JsonConverter)Activator.CreateInstance(
                    converterType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { registry },
                    culture: null);
            }
        };

    private static Type FindReferenceIdType(Type entityType)
    {
        if (entityType.IsGenericType && entityType.GetGenericTypeDefinition() == typeof(IJsonReferenceable<>))
        {
            return entityType.GetGenericArguments()[0];
        }

        foreach (var interfaceType in entityType.GetInterfaces())
        {
            if (interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(IJsonReferenceable<>))
            {
                return interfaceType.GetGenericArguments()[0];
            }
        }

        return null;
    }
}

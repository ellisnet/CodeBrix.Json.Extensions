using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace CodeBrix.Json.Extensions.Polymorphism.Internal;

internal sealed class DiscriminatorMap
{
    private static readonly ConcurrentDictionary<Type, DiscriminatorMap> Cache = new();

    private DiscriminatorMap(Type baseType, string propertyName, Dictionary<string, Type> knownTypes, Type fallbackType)
    {
        BaseType = baseType;
        PropertyName = propertyName;
        KnownTypes = knownTypes;
        FallbackType = fallbackType;
    }

    internal Type BaseType { get; }

    internal string PropertyName { get; }

    internal IReadOnlyDictionary<string, Type> KnownTypes { get; }

    internal Type FallbackType { get; }

    /// <summary>
    /// Reports whether <paramref name="baseType"/> declares <see cref="JsonDiscriminatorAttribute"/>
    /// (directly, not inherited) and therefore participates in discriminator-driven dispatch.
    /// </summary>
    internal static bool HasDiscriminator(Type baseType)
        => Attribute.IsDefined(baseType, typeof(JsonDiscriminatorAttribute), inherit: false);

    internal static DiscriminatorMap Get(Type baseType) => Cache.GetOrAdd(baseType, Build);

    private static DiscriminatorMap Build(Type baseType)
    {
        var discriminator = baseType.GetCustomAttribute<JsonDiscriminatorAttribute>(inherit: false);

        if (discriminator == null)
        {
            throw new InvalidOperationException(
                $"Type '{baseType}' does not declare [JsonDiscriminator]. Polymorphic deserialization via " +
                "FallbackTypeConverter requires a [JsonDiscriminator(\"<property>\")] attribute on the base " +
                "class or interface.");
        }

        var knownTypes = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var attribute in baseType.GetCustomAttributes<JsonKnownTypeAttribute>(inherit: false))
        {
            ValidateTargetType(baseType, attribute.KnownType, "known");

            if (!knownTypes.TryAdd(attribute.DiscriminatorValue, attribute.KnownType))
            {
                throw new InvalidOperationException(
                    $"Type '{baseType}' declares more than one [JsonKnownType] mapping for discriminator " +
                    $"value '{attribute.DiscriminatorValue}'.");
            }
        }

        var fallbackType = baseType.GetCustomAttribute<JsonFallbackTypeAttribute>(inherit: false)?.FallbackType;

        if (fallbackType != null)
        {
            ValidateTargetType(baseType, fallbackType, "fallback");
        }

        return new DiscriminatorMap(baseType, discriminator.PropertyName, knownTypes, fallbackType);
    }

    /// <summary>
    /// Resolves the concrete type to deserialize for <paramref name="root"/> by reading its discriminator
    /// property and matching it against the known-type mappings (falling back where declared). The caller
    /// must have already verified that <paramref name="root"/> is a JSON object.
    /// </summary>
    internal Type ResolveTargetType(JsonElement root, JsonSerializerOptions options)
    {
        string discriminatorValue = null;

        if (TryGetDiscriminatorProperty(root, options, out var property))
        {
            discriminatorValue = property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Null => null,
                _ => property.GetRawText(),
            };
        }

        if (discriminatorValue != null && KnownTypes.TryGetValue(discriminatorValue, out var knownType))
        {
            return knownType;
        }

        if (FallbackType != null)
        {
            return FallbackType;
        }

        throw new JsonException(discriminatorValue == null
            ? $"The JSON object has no '{PropertyName}' discriminator property, and type '{BaseType}' " +
              "declares no [JsonFallbackType]."
            : $"The discriminator value '{discriminatorValue}' does not match any [JsonKnownType] mapping " +
              $"of type '{BaseType}', and no [JsonFallbackType] is declared.");
    }

    private bool TryGetDiscriminatorProperty(JsonElement root, JsonSerializerOptions options, out JsonElement property)
    {
        if (root.TryGetProperty(PropertyName, out property))
        {
            return true;
        }

        if (options.PropertyNameCaseInsensitive)
        {
            foreach (var candidate in root.EnumerateObject())
            {
                if (string.Equals(candidate.Name, PropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;

                    return true;
                }
            }
        }

        property = default;

        return false;
    }

    private static void ValidateTargetType(Type baseType, Type targetType, string role)
    {
        if (targetType == baseType)
        {
            throw new InvalidOperationException(
                $"Type '{baseType}' declares itself as a {role} type, which would recurse endlessly. " +
                "Declare a derived type instead (for example an 'UnknownXyz' subclass).");
        }

        if (!baseType.IsAssignableFrom(targetType))
        {
            throw new InvalidOperationException(
                $"Type '{targetType}' is declared as a {role} type of '{baseType}' but is not assignable to it.");
        }

        var dispatchesFurther = Attribute.IsDefined(targetType, typeof(JsonDiscriminatorAttribute), inherit: false);

        if (!dispatchesFurther && (targetType.IsAbstract || targetType.IsInterface))
        {
            throw new InvalidOperationException(
                $"Type '{targetType}' is declared as a {role} type of '{baseType}' but is not instantiable " +
                "and does not declare its own [JsonDiscriminator] to dispatch further.");
        }
    }
}

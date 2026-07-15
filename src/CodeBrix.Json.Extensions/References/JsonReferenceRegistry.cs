using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace CodeBrix.Json.Extensions.References;

/// <summary>
/// An identifier-to-instance map used to resolve members annotated with
/// <see cref="JsonReferenceByIdAttribute"/> back to their live entities on read. Populate it with the
/// authoritative entities (typically the owning collections) before — or as — the graph that references them
/// by id is deserialized; this "two-phase apply" is how forward references are handled. For references whose
/// target is not yet present, <see cref="ResolveOrDefer"/> records a fixup that runs when the target is later
/// registered.
/// <para>
/// A registry is a plain object owned by the caller: create one, register entities, then pass it to
/// <see cref="ReferenceByIdJson"/> when deserializing. It is not thread-safe for concurrent mutation.
/// </para>
/// </summary>
public sealed class JsonReferenceRegistry
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo> IdProperties = new();

    private readonly Dictionary<(Type Type, object Id), object> _entities = new();
    private readonly List<(Type Type, object Id, Action<object> Apply)> _pending = new();

    /// <summary>
    /// Registers <paramref name="entity"/> under its <see cref="IJsonReferenceable{TId}.JsonReferenceId"/>,
    /// then runs any deferred fixups (see <see cref="ResolveOrDefer"/>) now satisfied by it.
    /// </summary>
    /// <param name="entity">The entity to register. Must implement <see cref="IJsonReferenceable{TId}"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="entity"/> does not implement <see cref="IJsonReferenceable{TId}"/>.
    /// </exception>
    public void Register(object entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var type = entity.GetType();
        var id = GetReferenceId(entity, type);
        _entities[(type, id)] = entity;

        RunPending(type, id, entity);
    }

    /// <summary>
    /// Resolves the entity registered for <paramref name="id"/> that is assignable to <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The (declared) type the reference is being resolved as.</param>
    /// <param name="id">The identifier to resolve.</param>
    /// <param name="entity">The resolved entity when the method returns <c>true</c>; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> when a matching entity is registered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <c>null</c>.</exception>
    public bool TryResolve(Type type, object id, out object entity)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (id != null && _entities.TryGetValue((type, id), out entity))
        {
            return true;
        }

        foreach (var pair in _entities)
        {
            if (Equals(pair.Key.Id, id) && type.IsAssignableFrom(pair.Key.Type))
            {
                entity = pair.Value;

                return true;
            }
        }

        entity = null;

        return false;
    }

    /// <summary>
    /// Applies <paramref name="apply"/> to the entity for <paramref name="id"/> immediately if it is already
    /// registered; otherwise records it as a deferred fixup that runs when a matching entity is later
    /// registered via <see cref="Register"/>. This is the explicit path for forward references.
    /// </summary>
    /// <param name="type">The (declared) type the reference is being resolved as.</param>
    /// <param name="id">The identifier to resolve.</param>
    /// <param name="apply">The action invoked with the resolved entity.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="type"/> or <paramref name="apply"/> is <c>null</c>.
    /// </exception>
    public void ResolveOrDefer(Type type, object id, Action<object> apply)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (apply == null)
        {
            throw new ArgumentNullException(nameof(apply));
        }

        if (TryResolve(type, id, out var entity))
        {
            apply(entity);

            return;
        }

        _pending.Add((type, id, apply));
    }

    private void RunPending(Type registeredType, object id, object entity)
    {
        for (var index = _pending.Count - 1; index >= 0; index--)
        {
            var pending = _pending[index];

            if (Equals(pending.Id, id) && pending.Type.IsAssignableFrom(registeredType))
            {
                _pending.RemoveAt(index);
                pending.Apply(entity);
            }
        }
    }

    private static object GetReferenceId(object entity, Type type)
    {
        var property = IdProperties.GetOrAdd(type, FindReferenceIdProperty);

        if (property == null)
        {
            throw new ArgumentException(
                $"Type '{type}' does not implement IJsonReferenceable<TId>, so it has no reference id.",
                nameof(entity));
        }

        return property.GetValue(entity);
    }

    private static PropertyInfo FindReferenceIdProperty(Type type)
    {
        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(IJsonReferenceable<>))
            {
                return interfaceType.GetProperty(nameof(IJsonReferenceable<object>.JsonReferenceId));
            }
        }

        return null;
    }
}

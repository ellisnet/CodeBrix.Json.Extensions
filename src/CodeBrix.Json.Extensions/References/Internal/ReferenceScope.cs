using System;
using System.Collections.Generic;
using System.Globalization;

namespace CodeBrix.Json.Extensions.References.Internal;

/// <summary>
/// A per-operation registry mapping instances to string identifiers while writing, and identifiers back to
/// instances while reading. One scope is created per top-level <see cref="ReferenceJson"/> operation and is
/// shared by every reference-aware converter instance that participates in it, so <c>$id</c>/<c>$ref</c>
/// numbering stays consistent across the whole object graph. It is not thread-safe: a single
/// System.Text.Json operation runs synchronously on one thread.
/// </summary>
internal sealed class ReferenceScope
{
    private readonly Dictionary<object, string> _writeIds = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, object> _readObjects = new(StringComparer.Ordinal);
    private int _lastId;

    /// <summary>
    /// Reports whether <paramref name="value"/> has already been written in this operation and, if so,
    /// returns the identifier previously assigned to it.
    /// </summary>
    internal bool TryGetWriteId(object value, out string id) => _writeIds.TryGetValue(value, out id);

    /// <summary>
    /// Assigns and records the next identifier for <paramref name="value"/> on first write.
    /// </summary>
    internal string AddForWrite(object value)
    {
        var id = (++_lastId).ToString(CultureInfo.InvariantCulture);
        _writeIds.Add(value, id);

        return id;
    }

    /// <summary>
    /// Records the instance created for <paramref name="id"/> on read, before its members are populated, so
    /// that references back to it (including cycles) resolve to the same instance.
    /// </summary>
    internal void AddForRead(string id, object value) => _readObjects[id] = value;

    /// <summary>
    /// Resolves the instance previously recorded for <paramref name="id"/> on read.
    /// </summary>
    internal bool TryGetReadObject(string id, out object value) => _readObjects.TryGetValue(id, out value);
}

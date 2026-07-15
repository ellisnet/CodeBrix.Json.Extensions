using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CodeBrix.Json.Extensions.References;

namespace CodeBrix.Json.Extensions.Tests;

// A string-keyed entity whose authoritative copy lives in an owning collection.
public class Scene : IJsonReferenceable<string>
{
    public string Id { get; set; }

    public string Name { get; set; }

    [JsonIgnore]
    public string JsonReferenceId => Id;
}

public class SpecialScene : Scene
{
    public bool Featured { get; set; }
}

// A holder that references a Scene by id rather than inlining it.
public class SceneRef
{
    [JsonReferenceById]
    public Scene Target { get; set; }

    public string Label { get; set; }
}

public class SceneRefList
{
    public List<SceneRef> Refs { get; set; }
}

// A Guid-keyed entity, to exercise a non-string identifier type.
public class GuidEntity : IJsonReferenceable<Guid>
{
    public Guid Ref { get; set; }

    public string Name { get; set; }

    [JsonIgnore]
    public Guid JsonReferenceId => Ref;
}

public class GuidHolder
{
    [JsonReferenceById]
    public GuidEntity Entity { get; set; }
}

// An int-keyed entity, to exercise a numeric identifier type.
public class IntEntity : IJsonReferenceable<int>
{
    public int Number { get; set; }

    [JsonIgnore]
    public int JsonReferenceId => Number;
}

public class IntHolder
{
    [JsonReferenceById]
    public IntEntity Entity { get; set; }
}

// A [JsonReferenceById] member whose type does NOT implement IJsonReferenceable<TId> — configuring the
// modifier for this member must fail with a clear JsonException.
public class BadByIdHolder
{
    [JsonReferenceById]
    public PlainTag NotReferenceable { get; set; }
}

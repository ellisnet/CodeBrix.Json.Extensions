# CodeBrix.Json.Extensions

A fully managed, cross-platform set of `System.Text.Json` extensions for .NET. It fills two of the gaps
that make a Newtonsoft.Json → System.Text.Json (STJ) migration hard, using only public STJ surface:

* **Polymorphism** (namespace `CodeBrix.Json.Extensions.Polymorphism`) — attribute-driven polymorphic
  deserialization: declare a discriminator property on a base class or interface, map known discriminator
  values to concrete derived types, and (optionally) declare a catch-all fallback type so unrecognized
  values deserialize gracefully instead of throwing. Replaces `TypeNameHandling`.
* **Reference handling** (namespace `CodeBrix.Json.Extensions.References`) — preserve object identity and
  cycles across a graph, two ways: opt-in `$id`/`$ref` preservation (the STJ analog of Newtonsoft's
  `[JsonObject(IsReference = true)]`), and explicit serialize-by-identifier for entities that already have
  a stable id. Replaces `PreserveReferencesHandling`.

CodeBrix.Json.Extensions has no dependencies other than .NET, and is provided as a .NET 10 library and
associated `CodeBrix.Json.Extensions.MitLicenseForever` NuGet package.

CodeBrix.Json.Extensions supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## Sample Code

### Polymorphic deserialization with a fallback (`CodeBrix.Json.Extensions.Polymorphism`)

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeBrix.Json.Extensions.Polymorphism;

[JsonConverter(typeof(FallbackTypeConverterFactory))]
[JsonDiscriminator("type")]
[JsonKnownType(typeof(Circle), "circle")]
[JsonKnownType(typeof(Square), "square")]
[JsonFallbackType(typeof(UnknownShape))]
public interface IShape
{
    [JsonPropertyName("type")]
    string Type { get; set; }
}

public class Circle : IShape
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("radius")]
    public double Radius { get; set; }
}

public class UnknownShape : IShape
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
}

// "circle" resolves to Circle...
var circle = JsonSerializer.Deserialize<IShape>("{\"type\":\"circle\",\"radius\":2.5}");

// ...and an unrecognized discriminator resolves to the fallback type instead of throwing.
var unknown = JsonSerializer.Deserialize<IShape>("{\"type\":\"hexagon\"}"); // UnknownShape
```

### Preserve identity and cycles with `$id`/`$ref` (`CodeBrix.Json.Extensions.References`)

```csharp
using CodeBrix.Json.Extensions.References;

[JsonReferenceable]
public class Node
{
    public string Name { get; set; }
    public Node Link { get; set; }
}

var a = new Node { Name = "a" };
var b = new Node { Name = "b" };
a.Link = b;
b.Link = a;                     // a cycle

var json = ReferenceJson.Serialize(a);
var back = ReferenceJson.Deserialize<Node>(json);

bool identityPreserved = ReferenceEquals(back, back.Link.Link);   // true
```

`[JsonReferenceable]` is opt-in per type and composes with the polymorphism attributes, so a type can be
both referenceable and discriminated.

### Serialize a shared entity by its identifier (`CodeBrix.Json.Extensions.References`)

```csharp
using CodeBrix.Json.Extensions.References;

public class Scene : IJsonReferenceable<string>
{
    public string Id { get; set; }
    public string Name { get; set; }
    [JsonIgnore] public string JsonReferenceId => Id;
}

public class SceneRef
{
    [JsonReferenceById] public Scene Target { get; set; }   // written as just its id
}

// Read follows a two-phase apply: register the owning entities, then resolve the references.
var registry = new JsonReferenceRegistry();
registry.Register(scene);
var back = ReferenceByIdJson.Deserialize<SceneRef>(json, registry);
```

## License

The project is licensed under the MIT License. see: https://en.wikipedia.org/wiki/MIT_License

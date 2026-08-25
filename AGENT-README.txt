================================================================================
AGENT-README: CodeBrix.Json.Extensions
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Json.Extensions.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Json.Extensions is a fully managed, dependency-free set of extensions
for the .NET System.Text.Json (STJ) library. It fills the two gaps that make a
Newtonsoft.Json -> STJ migration hard, using only public, documented STJ surface
(no reflection into STJ internals):

  1. POLYMORPHISM (namespace CodeBrix.Json.Extensions.Polymorphism)
     Attribute-driven polymorphic deserialization. Annotate a base class or
     interface with a discriminator property and a set of known concrete types;
     incoming JSON is dispatched to the matching concrete type based on the value
     of that discriminator property. An optional fallback type lets unrecognized
     (or missing, or null) discriminator values deserialize to a known "unknown"
     type instead of throwing. Replaces Newtonsoft's TypeNameHandling.

  2. REFERENCE HANDLING (namespace CodeBrix.Json.Extensions.References)
     Preserve object identity and cycles across a graph, two complementary ways:
       * Feature A - opt-in "$id"/"$ref" preservation (the STJ analog of
         Newtonsoft's [JsonObject(IsReference = true)], but per-type opt-in
         rather than global like STJ's ReferenceHandler.Preserve).
       * Feature B - explicit serialize-by-identifier for entities that already
         expose a stable id.
     Replaces Newtonsoft's PreserveReferencesHandling.

The two capabilities COMPOSE: a type may be both referenceable and discriminated,
and a single node can carry both a "$id"/"$ref" envelope and a discriminator.

Target framework: .NET 10 or later. The library builds only against the .NET
base class libraries and adds no external NuGet dependencies of its own. It is
original code, not a port or fork of another library.

There are exactly 11 public types, all named and documented below.

================================================================================

INSTALLATION
============
NuGet PackageId: CodeBrix.Json.Extensions.MitLicenseForever
NuGet dependencies: none (beyond the .NET base class libraries)
License: MIT (SPDX: MIT)
Requirements: .NET 10 or later. Fully managed - no native libraries, no
platform restrictions.

    dotnet add package CodeBrix.Json.Extensions.MitLicenseForever

Note that the NuGet PACKAGE id carries the ".MitLicenseForever" suffix, but the
ASSEMBLY and the NAMESPACES you import do not: they are named
CodeBrix.Json.Extensions.

================================================================================

KEY NAMESPACES / USINGS
=======================
The public API lives in two feature namespaces; the root CodeBrix.Json.Extensions
namespace intentionally holds NO public types, so the namespace you import names
the capability you are using.

    using CodeBrix.Json.Extensions.Polymorphism;   // discriminator / fallback
    using CodeBrix.Json.Extensions.References;     // $id/$ref and by-id refs

You will almost always also need:

    using System.Text.Json;                        // JsonSerializer, options
    using System.Text.Json.Serialization;          // [JsonConverter], [JsonIgnore]

Sub-namespaces:
  - CodeBrix.Json.Extensions.Polymorphism            Discriminator attributes +
                                                     converter/factory.
  - CodeBrix.Json.Extensions.Polymorphism.Internal   Implementation detail; do
                                                     not reference from consumer
                                                     code.
  - CodeBrix.Json.Extensions.References              Reference attributes,
                                                     interface, registry, and the
                                                     ReferenceJson /
                                                     ReferenceByIdJson entry
                                                     points.
  - CodeBrix.Json.Extensions.References.Internal     Implementation detail; do
                                                     not reference from consumer
                                                     code.

================================================================================

CORE API REFERENCE - POLYMORPHISM
=================================
Namespace: CodeBrix.Json.Extensions.Polymorphism

1. [JsonDiscriminator("propertyName")]
   Applied to a class or interface (AttributeTargets.Class | Interface,
   Inherited = false). Declares the JSON property whose value selects the
   concrete type. Exposes string PropertyName. The constructor throws
   ArgumentException when the name is null/empty/whitespace.

2. [JsonKnownType(typeof(Derived), "discriminatorValue")]
   Applied to the same base type, once per known discriminator value
   (AttributeTargets.Class | Interface, AllowMultiple = true, Inherited = false).
   Exposes Type KnownType and string DiscriminatorValue. Discriminator VALUES are
   matched ORDINALLY (case-sensitive) against the JSON string value; a numeric or
   other non-string discriminator is matched against its raw JSON text (so
   [JsonKnownType(typeof(NumberedShape), "7")] matches the JSON number 7). The
   discriminator PROPERTY NAME is matched exactly, and additionally
   case-insensitively when JsonSerializerOptions.PropertyNameCaseInsensitive is
   true.

3. [JsonFallbackType(typeof(UnknownDerived))]
   Applied to the base type (AttributeTargets.Class | Interface,
   Inherited = false). The catch-all type used when the discriminator is missing,
   null, or unmatched. Exposes Type FallbackType. When NO fallback type is
   declared, an unmatched discriminator throws JsonException.

4. FallbackTypeConverterFactory : JsonConverterFactory
   The entry point that wires the attributes into STJ. Register it either per
   type with [JsonConverter(typeof(FallbackTypeConverterFactory))] on the base
   type, or globally by adding one instance to
   JsonSerializerOptions.Converters. Its CanConvert accepts a type only when the
   type is NOT a value type and declares [JsonDiscriminator] directly (not
   inherited).

5. FallbackTypeConverter<T> : JsonConverter<T> where T : class
   The converter the factory produces. Read parses the object, resolves the
   concrete type from the discriminator, and deserializes as that type. Write
   serializes the runtime type's normal contract; serializing an instance whose
   runtime type is exactly the base type T throws JsonException.

Multi-level dispatch is supported: a [JsonKnownType] target may itself be an
abstract class or interface as long as IT declares its own [JsonDiscriminator],
in which case dispatch continues from there. A non-instantiable known/fallback
type that does NOT declare its own discriminator is a configuration error
(InvalidOperationException).

Once the attributes are in place, ordinary JsonSerializer.Deserialize<TBase>(...)
calls dispatch correctly - no special entry point is needed for polymorphism.

================================================================================

CORE API REFERENCE - REFERENCES, FEATURE A ($id / $ref)
=======================================================
Namespace: CodeBrix.Json.Extensions.References

[JsonReferenceable]  (AttributeTargets.Class | Interface, Inherited = false)
   Marks a type as reference-tracked. The first occurrence of an instance is
   written with a "$id"; later occurrences of the SAME instance are written as
   { "$ref": "<id>" } and restored to one shared instance on read. Identity is
   REFERENCE identity, so two equal-but-distinct instances get two "$id"s.
   Opt-in per type, and NOT inherited: every derived type whose identity should
   be preserved must carry the attribute itself. Composes with the polymorphism
   attributes: apply the discriminator attributes ([JsonDiscriminator] /
   [JsonKnownType] / [JsonFallbackType]) but do NOT also attach
   [JsonConverter(typeof(FallbackTypeConverterFactory))] - ReferenceJson's own
   converter performs the dispatch, and attaching the polymorphism converter
   would bypass reference handling.

ReferenceJson  (static entry point)
   string  Serialize<TValue>(TValue value, JsonSerializerOptions options = null)
   string  Serialize(object value, Type inputType, JsonSerializerOptions options = null)
   byte[]  SerializeToUtf8Bytes<TValue>(TValue value, JsonSerializerOptions options = null)
   TValue  Deserialize<TValue>(string json, JsonSerializerOptions options = null)
   TValue  Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options = null)
   object  Deserialize(string json, Type returnType, JsonSerializerOptions options = null)

   Each call is one self-contained operation with a fresh reference scope, and
   "$id" numbering restarts at "1" in every operation. Any supplied
   JsonSerializerOptions is treated as a settings TEMPLATE and copied, so it is
   never mutated; do not pass options previously returned by this type. When the
   supplied options carry no TypeInfoResolver, the reflection-based
   DefaultJsonTypeInfoResolver is used to read object contracts.

   Feature A works ONLY through ReferenceJson. A plain JsonSerializer call
   ignores [JsonReferenceable] and inlines every occurrence (and will throw or
   loop on a cycle).

Requirements for a referenceable type: it must be constructible by STJ (a usable
parameterless constructor) and expose SETTABLE members, because cycles are
restored by creating the instance first and populating it afterwards. A type
that cannot be constructed this way throws a clear JsonException on read, and
get-only members are skipped when reading.

Members that are not themselves referenceable serialize inline as usual, and
[JsonIgnore]d members stay ignored inside the "$id" envelope.

Wire format (matches Newtonsoft's shape):
    First occurrence:  { "$id": "5", "type": "sprite", ...members... }
    Later occurrence:  { "$ref": "5" }

================================================================================

CORE API REFERENCE - REFERENCES, FEATURE B ([JsonReferenceById])
================================================================
Namespace: CodeBrix.Json.Extensions.References

For graphs whose shared entities already have stable ids: a member is serialized
as JUST its identifier and resolved back to the live instance on read.

IJsonReferenceable<out TId>
   TId JsonReferenceId { get; }   // the entity's stable identifier

   Implement it on the entity type. JsonReferenceId is an ordinary member as far
   as STJ is concerned, so mark it [JsonIgnore] unless you want the id written
   twice (once as the entity's own property and once as this member).

[JsonReferenceById]  (AttributeTargets.Property | Field, Inherited = true)
   Marks a member whose value is an IJsonReferenceable<TId> entity to be written
   as just its id (not inlined) and resolved on read. Takes effect only through
   ReferenceByIdJson (or options it configures); a plain JsonSerializer call
   ignores it and inlines the member as usual. The wiring walks the type's STJ
   property list, so an annotated FIELD is only picked up when STJ surfaces it -
   that is, with JsonSerializerOptions.IncludeFields = true or [JsonInclude].

JsonReferenceRegistry  (id -> instance map; caller-owned, sealed)
   void  Register(object entity)                          // by its IJsonReferenceable id
   bool  TryResolve(Type type, object id, out object e)   // exact, then assignable
   void  ResolveOrDefer(Type type, object id, Action<object> apply)  // forward refs

   Register also runs any deferred fixups that the newly registered entity now
   satisfies. TryResolve first looks for an exact (type, id) match and then falls
   back to any registered entity with that id whose type is assignable to the
   requested type - which is how a reference declared as a base type resolves to
   a registered derived instance. A registry is a plain object you own; it is not
   thread-safe for concurrent mutation.

ReferenceByIdJson  (static entry point)
   string  Serialize<TValue>(TValue value, JsonSerializerOptions options = null)
   byte[]  SerializeToUtf8Bytes<TValue>(TValue value, JsonSerializerOptions options = null)
   TValue  Deserialize<TValue>(string json, JsonReferenceRegistry registry, JsonSerializerOptions options = null)
   TValue  Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonReferenceRegistry registry, JsonSerializerOptions options = null)

   Serialize needs no registry (ids are read off the entities). Deserialize
   REQUIRES one; passing null throws ArgumentNullException. Supplied options are
   copied, and the copy's TypeInfoResolver gets the by-id modifier layered on
   top (falling back to DefaultJsonTypeInfoResolver when you supply none).

Read uses a "two-phase apply": populate the registry with the authoritative
entities (the owning collections) FIRST, then deserialize the referencing graph.
An identifier that is not registered by the time its member is read throws
JsonException - the read path does not defer on your behalf. ResolveOrDefer is
the caller-facing escape hatch for genuine forward references: call it yourself
to record a fixup that runs when the target is registered later.

Identifier types are whatever your entity exposes; string, int and Guid ids are
all exercised by the test suite. The id is written and read with the ordinary STJ
contract for that type.

================================================================================

ERROR MODEL
===========
  - JsonException             The JSON value is not an object where one is
                              required; a discriminator is missing/unmatched and
                              no fallback is declared; a base-type instance is
                              written; a "$ref" names an unknown id; a
                              referenceable type cannot be constructed; a
                              [JsonReferenceById] member's type does not
                              implement IJsonReferenceable<TId>; or a by-id
                              identifier cannot be resolved.
  - InvalidOperationException A polymorphic base declares no [JsonDiscriminator];
                              a known/fallback type maps to the base type itself,
                              is not assignable, is declared twice, or is
                              abstract/an interface without its own discriminator.
  - ArgumentNullException     A required argument (inputType, returnType,
                              registry, entity, type, apply) is null.
  - ArgumentException         A [JsonDiscriminator] property name is
                              null/empty/whitespace; or an entity registered with
                              JsonReferenceRegistry does not implement
                              IJsonReferenceable<TId>.

Every message names the offending type and, where relevant, tells you what to do
instead - read the exception text before guessing.

================================================================================

CHOOSING A REFERENCE STRATEGY
=============================
  Feature A ($id/$ref)         Feature B ([JsonReferenceById])
  --------------------------   --------------------------------
  Mechanical Newtonsoft port   Clean redesign with existing stable ids
  Newtonsoft-shaped sentinels  Plain, human-readable id values
  Whole graph in one document  Owning sets can be split / registered separately
  No model requirement         Entity must expose a stable id
  Cycles round-trip            Targets must exist in the registry

Pick one per relationship for clarity; both are opt-in and add no cost to types
that use neither.

================================================================================

COMPLETE EXAMPLES
=================

Example 1: Polymorphic deserialization with a fallback type
------------------------------------------------------------
    using System;
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

    public class Square : IShape
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("side")]
        public double Side { get; set; }
    }

    public class UnknownShape : IShape
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public static class ShapeDemo
    {
        public static void Run()
        {
            // "circle" resolves to Circle.
            IShape circle = JsonSerializer.Deserialize<IShape>(
                "{\"type\":\"circle\",\"radius\":2.5}");
            Console.WriteLine(((Circle)circle).Radius);          // 2.5

            // An unrecognized discriminator resolves to the fallback type
            // instead of throwing.
            IShape unknown = JsonSerializer.Deserialize<IShape>(
                "{\"type\":\"hexagon\"}");
            Console.WriteLine(unknown.GetType().Name);           // UnknownShape

            // Writing uses the runtime type's normal contract.
            string json = JsonSerializer.Serialize<IShape>(
                new Square { Type = "square", Side = 4 });
            Console.WriteLine(json);      // {"type":"square","side":4}
        }
    }

Registering the factory globally instead of per type - use this when you cannot
put [JsonConverter] on the base type (for example, it lives in another
assembly):

    var options = new JsonSerializerOptions();
    options.Converters.Add(new FallbackTypeConverterFactory());

    IShape shape = JsonSerializer.Deserialize<IShape>(json, options);

Example 2: Preserve identity and cycles with $id / $ref
--------------------------------------------------------
    using System;
    using System.Collections.Generic;
    using CodeBrix.Json.Extensions.References;

    [JsonReferenceable]
    public class Node
    {
        public string Name { get; set; }

        public Node Link { get; set; }

        public List<Node> Friends { get; set; }
    }

    public static class NodeDemo
    {
        public static void Run()
        {
            var a = new Node { Name = "a" };
            var b = new Node { Name = "b" };
            a.Link = b;
            b.Link = a;                          // a cycle

            string json = ReferenceJson.Serialize(a);
            // {"$id":"1","Name":"a","Link":{"$id":"2","Name":"b",
            //  "Link":{"$ref":"1"},"Friends":null},"Friends":null}

            Node back = ReferenceJson.Deserialize<Node>(json);

            Console.WriteLine(ReferenceEquals(back, back.Link.Link));   // True

            // A shared instance appearing twice round-trips to ONE instance.
            var shared = new Node { Name = "hero" };
            var list = new List<Node> { shared, shared };

            List<Node> backList = ReferenceJson.Deserialize<List<Node>>(
                ReferenceJson.Serialize(list));

            Console.WriteLine(ReferenceEquals(backList[0], backList[1])); // True
        }
    }

Passing options (they are copied, never mutated):

    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    string json = ReferenceJson.Serialize(a, options);
    Node back = ReferenceJson.Deserialize<Node>(json, options);

Example 3: Serialize a shared entity by its identifier (two-phase apply)
-------------------------------------------------------------------------
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using CodeBrix.Json.Extensions.References;

    public class Scene : IJsonReferenceable<string>
    {
        public string Id { get; set; }

        public string Name { get; set; }

        [JsonIgnore]
        public string JsonReferenceId => Id;
    }

    public class SceneRef
    {
        [JsonReferenceById]
        public Scene Target { get; set; }        // written as just its id

        public string Label { get; set; }
    }

    public class SceneRefList
    {
        public List<SceneRef> Refs { get; set; }
    }

    public static class SceneDemo
    {
        public static void Run()
        {
            // The authoritative entities live in their own collection.
            var scenes = new List<Scene>
            {
                new Scene { Id = "s1", Name = "One" },
                new Scene { Id = "s2", Name = "Two" },
            };

            // The referencing document names them by id only.
            string refsJson = ReferenceByIdJson.Serialize(new SceneRefList
            {
                Refs = new List<SceneRef>
                {
                    new SceneRef { Target = scenes[1], Label = "points-to-two" },
                    new SceneRef { Target = scenes[0], Label = "points-to-one" },
                },
            });
            // {"Refs":[{"Target":"s2","Label":"points-to-two"},
            //          {"Target":"s1","Label":"points-to-one"}]}

            // Phase 1: register the owning entities.
            var registry = new JsonReferenceRegistry();

            foreach (var scene in scenes)
            {
                registry.Register(scene);
            }

            // Phase 2: deserialize the referencing graph.
            SceneRefList back = ReferenceByIdJson.Deserialize<SceneRefList>(
                refsJson, registry);

            Console.WriteLine(ReferenceEquals(back.Refs[0].Target, scenes[1]));  // True
            Console.WriteLine(back.Refs[1].Target.Name);                         // One
        }
    }

Handling a genuine forward reference yourself:

    var registry = new JsonReferenceRegistry();

    // Record what to do when scene "s9" eventually shows up...
    registry.ResolveOrDefer(typeof(Scene), "s9", entity => holder.Target = (Scene)entity);

    // ...and the fixup runs at registration time.
    registry.Register(new Scene { Id = "s9", Name = "Late" });

Example 4: A type that is BOTH polymorphic and referenceable
--------------------------------------------------------------
    using System;
    using System.Text.Json.Serialization;
    using CodeBrix.Json.Extensions.Polymorphism;
    using CodeBrix.Json.Extensions.References;

    // Note: the discriminator ATTRIBUTES are present, but NOT
    // [JsonConverter(typeof(FallbackTypeConverterFactory))] - ReferenceJson's own
    // converter does the dispatch, and attaching the polymorphism converter here
    // would bypass reference handling.
    [JsonReferenceable]
    [JsonDiscriminator("kind")]
    [JsonKnownType(typeof(Car), "car")]
    [JsonKnownType(typeof(Truck), "truck")]
    [JsonFallbackType(typeof(UnknownVehicle))]
    public class Vehicle
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        public string Name { get; set; }

        public Vehicle Tows { get; set; }
    }

    [JsonReferenceable]
    public class Car : Vehicle
    {
        public int Doors { get; set; }
    }

    [JsonReferenceable]
    public class Truck : Vehicle
    {
        public double Payload { get; set; }
    }

    [JsonReferenceable]
    public class UnknownVehicle : Vehicle
    {
    }

    public static class VehicleDemo
    {
        public static void Run()
        {
            // Set the discriminator VALUE yourself - it is an ordinary member.
            var truck = new Truck { Kind = "truck", Name = "big", Payload = 2.5 };
            var car = new Car { Kind = "car", Name = "little", Doors = 4 };
            car.Tows = truck;
            truck.Tows = car;                    // a cycle across two types

            string json = ReferenceJson.Serialize<Vehicle>(car);
            Vehicle back = ReferenceJson.Deserialize<Vehicle>(json);

            Console.WriteLine(back.GetType().Name);                     // Car
            Console.WriteLine(back.Tows.GetType().Name);                // Truck
            Console.WriteLine(ReferenceEquals(back.Tows.Tows, back));   // True
        }
    }

================================================================================

MINIMUM VIABLE PROJECT
======================

    dotnet new console -n MyJsonApp --framework net10.0
    cd MyJsonApp
    dotnet add package CodeBrix.Json.Extensions.MitLicenseForever

Program.cs:

    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using CodeBrix.Json.Extensions.Polymorphism;

    [JsonConverter(typeof(FallbackTypeConverterFactory))]
    [JsonDiscriminator("type")]
    [JsonKnownType(typeof(TextNote), "text")]
    [JsonFallbackType(typeof(UnknownNote))]
    public class Note
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class TextNote : Note
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class UnknownNote : Note
    {
    }

    public static class Program
    {
        public static void Main()
        {
            Note known = JsonSerializer.Deserialize<Note>(
                "{\"type\":\"text\",\"text\":\"hello\"}");
            Console.WriteLine(((TextNote)known).Text);        // hello

            Note unknown = JsonSerializer.Deserialize<Note>("{\"type\":\"video\"}");
            Console.WriteLine(unknown.GetType().Name);        // UnknownNote
        }
    }

Build and run:

    dotnet build
    dotnet run

================================================================================

PERFORMANCE TIPS
================

1. The per-base-type discriminator map (property name, known types, fallback) is
   built once and cached, so repeated deserialization of the same base type does
   not re-read attributes. Validation errors therefore surface on the FIRST use
   of a base type, not on every call.

2. Polymorphic Read buffers the JSON object into a JsonDocument so it can look at
   the discriminator before choosing a type. That is one extra parse of the
   object's own text per polymorphic node - fine for normal payloads, but do not
   put a discriminated base type on a hot path over very large objects when a
   non-polymorphic model would do.

3. ReferenceJson builds a fresh copy of the options (plus a sibling metadata
   options object) on EVERY call. For a tight loop, serialize one bigger document
   rather than many small ones.

4. Prefer the ReadOnlySpan<byte> / SerializeToUtf8Bytes overloads when you
   already have UTF-8 bytes; they avoid a string round-trip.

5. Reference identity uses reference equality with an ordinary dictionary, and
   the reference scope is per operation - memory is proportional to the number of
   distinct referenceable instances in one document.

6. JsonReferenceRegistry.TryResolve is O(1) for an exact (type, id) hit and falls
   back to a linear scan when the requested type is a base type of the registered
   one. Register entities under the type you will reference them as when a
   registry gets large.

7. Types that use none of these attributes pay nothing: the converters' CanConvert
   checks are attribute lookups, and plain JsonSerializer paths are untouched.

================================================================================

COMMON PITFALLS TO AVOID
========================

1. DO NOT confuse the package id with the namespace. Package:
   CodeBrix.Json.Extensions.MitLicenseForever. Namespaces:
   CodeBrix.Json.Extensions.Polymorphism and .References.

2. DO NOT attach [JsonConverter(typeof(FallbackTypeConverterFactory))] to a type
   that is ALSO [JsonReferenceable]. ReferenceJson's own converter performs the
   discriminator dispatch; the polymorphism converter would bypass reference
   handling. Attributes yes, converter no.

3. DO NOT expect a plain JsonSerializer call to honor [JsonReferenceable] or
   [JsonReferenceById]. Feature A needs ReferenceJson; Feature B needs
   ReferenceByIdJson. Only the polymorphism attributes work through plain
   JsonSerializer.

4. DO NOT reuse options returned or configured by ReferenceJson /
   ReferenceByIdJson. Options you pass in are a TEMPLATE that gets copied; pass
   your own plain JsonSerializerOptions each time.

5. DO NOT rely on case-insensitive discriminator VALUES. Values are matched with
   StringComparer.Ordinal. Only the discriminator property NAME gets a
   case-insensitive fallback, and only when PropertyNameCaseInsensitive is true.

6. DO NOT make a referenceable type immutable. It needs a usable parameterless
   constructor and settable members - the instance is created first and populated
   afterwards so cycles can point back at it. Get-only members are skipped on
   read, and a constructor-only type throws JsonException.

7. DO NOT forget that [JsonReferenceable] is Inherited = false. Put it on every
   derived type whose identity should be preserved, not just the base.

8. DO NOT expect the library to write the discriminator value for you. On a
   referenceable-plus-discriminated type the discriminator is an ordinary member:
   set it (Kind = "truck") before serializing, or the value will be null on the
   wire and the fallback type will be chosen on read.

9. DO NOT forget [JsonIgnore] on IJsonReferenceable<TId>.JsonReferenceId. It is a
   normal public member and will otherwise appear in the entity's own JSON in
   addition to its real id property.

10. DO NOT deserialize a by-id document before registering its targets. The read
    path throws JsonException on an unresolved id; it does not defer for you.
    Register the owning collection first (two-phase apply), or call
    JsonReferenceRegistry.ResolveOrDefer yourself for real forward references.

11. DO NOT pass null as the registry to ReferenceByIdJson.Deserialize - it throws
    ArgumentNullException. Serialize, by contrast, takes no registry at all.

12. DO NOT annotate a [JsonReferenceById] FIELD and expect it to work by default.
    The wiring walks the STJ property list, so a field needs
    JsonSerializerOptions.IncludeFields = true (or [JsonInclude]).

13. DO NOT point [JsonReferenceById] at a type that does not implement
    IJsonReferenceable<TId> - configuring that member throws JsonException.

14. DO NOT declare a known or fallback type that is abstract or an interface
    unless it declares its own [JsonDiscriminator] to dispatch further, and never
    declare the base type as its own known/fallback type. Both are
    InvalidOperationException.

15. DO NOT serialize an instance whose runtime type is exactly the discriminated
    base type - it throws JsonException. That is what the fallback type exists
    for: serialize an UnknownXyz subclass instead.

16. DO NOT share one JsonReferenceRegistry across threads that mutate it
    concurrently; it is not thread-safe for concurrent mutation.

================================================================================

WHAT THIS PACKAGE DOES NOT DO
=============================

  - It does NOT support polymorphism or $id/$ref on VALUE types. Both converter
    factories reject value types, so structs and record structs are out; use
    classes (or record classes with settable members).
  - It does NOT support reference preservation for types without a usable
    parameterless constructor or without settable members - constructor-only and
    init-only shapes throw or silently skip members on read.
  - It does NOT replace STJ's own [JsonPolymorphic] / [JsonDerivedType]; it is an
    alternative with a fallback type and a Newtonsoft-shaped wire format. Do not
    mix the two on one type.
  - It does NOT write the discriminator value for you on serialize - the
    discriminator is your own model property.
  - It does NOT provide a source-generated JsonSerializerContext, and its
    reference paths fall back to the reflection-based DefaultJsonTypeInfoResolver
    when you supply no resolver. Treat it as a reflection-based library; it is
    not designed for trimmed / AOT-only applications.
  - It does NOT defer unresolved by-id references automatically; deferral is a
    call you make on JsonReferenceRegistry.
  - It does NOT make Feature A and Feature B interchangeable inside one member:
    a member is either inlined with a "$id" envelope or written as a bare id.
  - It does NOT add any general-purpose converters (dates, enums, dictionaries,
    numbers) - use STJ's own converters for those.
  - It does NOT touch STJ internals or use reflection into them, and it does NOT
    change behavior for types that carry none of its attributes.

================================================================================

WORKING EXAMPLES ON GITHUB
==========================

The CodeBrix.Json.Extensions.Tests project contains compilable, working examples
of every feature above. If this file is not enough for a specific task, read the
relevant test file:

    https://github.com/ellisnet/CodeBrix.Json.Extensions/tree/main/tests/CodeBrix.Json.Extensions.Tests

  Polymorphic model shapes used by the tests (interface base, abstract base,
  concrete base, numeric discriminator, two-level dispatch):
    -> https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/tests/CodeBrix.Json.Extensions.Tests/TestShapes.cs

  Polymorphic Read/Write dispatch matrix (known values, fallback paths,
  missing/null/numeric discriminators, multi-level dispatch, error paths):
    -> https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/tests/CodeBrix.Json.Extensions.Tests/FallbackTypeConverterTests.cs

  Factory registration (per type and globally) and CanConvert behavior:
    -> https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/tests/CodeBrix.Json.Extensions.Tests/FallbackTypeConverterFactoryTests.cs

  Attribute construction and validation:
    -> https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/tests/CodeBrix.Json.Extensions.Tests/JsonDiscriminatorAttributeTests.cs
    -> https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/tests/CodeBrix.Json.Extensions.Tests/JsonKnownTypeAttributeTests.cs
    -> https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/tests/CodeBrix.Json.Extensions.Tests/JsonFallbackTypeAttributeTests.cs

  Discriminator resolution rules (ordinal value matching, case-insensitive
  property-name fallback, invalid configurations):
    -> https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/tests/CodeBrix.Json.Extensions.Tests/DiscriminatorMapTests.cs

  Feature A model shapes (referenceable node, cycles, polymorphic +
  referenceable composition, non-constructible type, ignored members):
    -> https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/tests/CodeBrix.Json.Extensions.Tests/ReferenceShapes.cs

  Feature A round trips (shared references, cycles, self-loops, diamonds,
  camelCase/case-insensitive options, error paths):
    -> https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/tests/CodeBrix.Json.Extensions.Tests/ReferenceJsonTests.cs

  Feature B model shapes (string, Guid and int identifiers; a bad by-id member):
    -> https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/tests/CodeBrix.Json.Extensions.Tests/ByIdShapes.cs

  Feature B write/read, two-phase apply, deferred fixups, registry validation:
    -> https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/tests/CodeBrix.Json.Extensions.Tests/ReferenceByIdJsonTests.cs

HOW TO USE: to fetch the raw text of any file above, replace
"https://github.com/ellisnet/CodeBrix.Json.Extensions/blob/main/" with
"https://raw.githubusercontent.com/ellisnet/CodeBrix.Json.Extensions/main/".

================================================================================

QUICK REFERENCE CARD
====================

--- POLYMORPHISM (using CodeBrix.Json.Extensions.Polymorphism) ---
[JsonDiscriminator("propertyName")]        class/interface, Inherited = false
    .PropertyName                          string
[JsonKnownType(typeof(D), "value")]        AllowMultiple = true
    .KnownType / .DiscriminatorValue       Type / string
[JsonFallbackType(typeof(UnknownD))]       catch-all for missing/null/unmatched
    .FallbackType                          Type
FallbackTypeConverterFactory               : JsonConverterFactory
    apply via [JsonConverter(...)] on the base type,
    or options.Converters.Add(new FallbackTypeConverterFactory())
FallbackTypeConverter<T> where T : class   : JsonConverter<T>
Entry point                                plain JsonSerializer.Deserialize<TBase>
Value matching                             ordinal (case-sensitive)
Property-name matching                     exact, plus case-insensitive when
                                           options.PropertyNameCaseInsensitive

--- FEATURE A: $id / $ref (using CodeBrix.Json.Extensions.References) ---
[JsonReferenceable]                        class/interface, Inherited = false
ReferenceJson.Serialize<TValue>(value, options = null)                -> string
ReferenceJson.Serialize(value, inputType, options = null)             -> string
ReferenceJson.SerializeToUtf8Bytes<TValue>(value, options = null)     -> byte[]
ReferenceJson.Deserialize<TValue>(json, options = null)               -> TValue
ReferenceJson.Deserialize<TValue>(utf8Json, options = null)           -> TValue
ReferenceJson.Deserialize(json, returnType, options = null)           -> object
Wire format                                {"$id":"1", ...} / {"$ref":"1"}
Requires                                   parameterless ctor + settable members

--- FEATURE B: by identifier (using CodeBrix.Json.Extensions.References) ---
IJsonReferenceable<out TId>                TId JsonReferenceId { get; }
[JsonReferenceById]                        property/field, Inherited = true
JsonReferenceRegistry                      sealed
    .Register(entity)                                                 -> void
    .TryResolve(type, id, out entity)                                 -> bool
    .ResolveOrDefer(type, id, apply)                                  -> void
ReferenceByIdJson.Serialize<TValue>(value, options = null)            -> string
ReferenceByIdJson.SerializeToUtf8Bytes<TValue>(value, options = null) -> byte[]
ReferenceByIdJson.Deserialize<TValue>(json, registry, options = null) -> TValue
ReferenceByIdJson.Deserialize<TValue>(utf8Json, registry, options = null)
                                                                      -> TValue
Read order                                 register targets, THEN deserialize

--- EXCEPTIONS ---
JsonException               shape/dispatch/reference-resolution failures
InvalidOperationException   invalid polymorphic configuration
ArgumentNullException       null inputType / returnType / registry / entity /
                            type / apply
ArgumentException           empty discriminator name; entity without
                            IJsonReferenceable<TId>

================================================================================

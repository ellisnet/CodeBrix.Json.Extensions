================================================================================
AGENT-README: CodeBrix.Json.Extensions
A Comprehensive Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
CodeBrix.Json.Extensions is a fully managed, dependency-free set of extensions
for the .NET System.Text.Json (STJ) library, targeting .NET 10.0 and later. It
fills the two gaps that make a Newtonsoft.Json -> STJ migration hard, using only
public, documented STJ surface (no reflection into STJ internals):

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

The library builds only against the .NET base class libraries. It adds no
external NuGet dependencies of its own.


INSTALLATION
------------
NuGet Package: CodeBrix.Json.Extensions.MitLicenseForever
Dependencies: none (beyond the .NET 10.0 base class libraries)

    dotnet add package CodeBrix.Json.Extensions.MitLicenseForever

Target framework: .NET 10.0 or higher.

Note that the NuGet PACKAGE id carries the ".MitLicenseForever" suffix, but the
NAMESPACES you import do not.


KEY NAMESPACES
--------------
The public API lives in two feature namespaces; the root CodeBrix.Json.Extensions
namespace intentionally holds NO public types, so the namespace you import names
the capability you are using.

    using CodeBrix.Json.Extensions.Polymorphism;   // discriminator / fallback
    using CodeBrix.Json.Extensions.References;      // $id/$ref and by-id refs

Sub-namespaces:
  - CodeBrix.Json.Extensions.Polymorphism            Discriminator attributes +
                                                     converter/factory.
  - CodeBrix.Json.Extensions.Polymorphism.Internal   Implementation detail.
  - CodeBrix.Json.Extensions.References              Reference attributes,
                                                     interface, registry, and the
                                                     ReferenceJson / ReferenceByIdJson
                                                     entry points.
  - CodeBrix.Json.Extensions.References.Internal      Implementation detail.


CORE API REFERENCE - POLYMORPHISM
---------------------------------
Namespace: CodeBrix.Json.Extensions.Polymorphism

1. [JsonDiscriminator("propertyName")]
   Applied to a class or interface (Inherited = false). Declares the JSON
   property whose value selects the concrete type. Exposes string PropertyName.
   The constructor throws ArgumentException when the name is null/empty/whitespace.

2. [JsonKnownType(typeof(Derived), "discriminatorValue")]
   Applied to the same base type, once per known discriminator value
   (AllowMultiple = true, Inherited = false). Exposes Type KnownType and
   string DiscriminatorValue. Discriminator values are matched ORDINALLY
   (case-sensitive) against the JSON string value; a numeric or other non-string
   discriminator is matched against its raw JSON text.

3. [JsonFallbackType(typeof(UnknownDerived))]
   Applied to the base type (Inherited = false). The catch-all type used when the
   discriminator is missing, null, or unmatched. Exposes Type FallbackType. When
   NO fallback type is declared, an unmatched discriminator throws JsonException.

4. FallbackTypeConverterFactory : JsonConverterFactory
   The entry point that wires the attributes into STJ. Register it either per type
   with [JsonConverter(typeof(FallbackTypeConverterFactory))] on the base type, or
   globally by adding one instance to JsonSerializerOptions.Converters.

5. FallbackTypeConverter<T> : JsonConverter<T> where T : class
   The converter the factory produces. Read parses the object, resolves the
   concrete type from the discriminator, and deserializes as that type. Write
   serializes the runtime type's normal contract; serializing an instance whose
   runtime type is exactly the base type T throws JsonException.

The polymorphism public surface and behavior are UNCHANGED from the library's
first iteration; only its namespace moved from the root to .Polymorphism.


CORE API REFERENCE - REFERENCES, FEATURE A ($id / $ref)
-------------------------------------------------------
Namespace: CodeBrix.Json.Extensions.References

[JsonReferenceable]  (class/interface, Inherited = false)
   Marks a type as reference-tracked. The first occurrence of an instance is
   written with a "$id"; later occurrences of the SAME instance are written as
   { "$ref": "<id>" } and restored to one shared instance on read. Opt-in per
   type. Composes with the polymorphism attributes: apply the discriminator
   attributes ([JsonDiscriminator]/[JsonKnownType]/[JsonFallbackType]) but do NOT
   also attach [JsonConverter(typeof(FallbackTypeConverterFactory))] - ReferenceJson's
   own converter performs the dispatch, and attaching the polymorphism converter
   would bypass reference handling.

ReferenceJson  (static entry point)
   string  Serialize<TValue>(TValue value, JsonSerializerOptions options = null)
   string  Serialize(object value, Type inputType, JsonSerializerOptions options = null)
   byte[]  SerializeToUtf8Bytes<TValue>(TValue value, JsonSerializerOptions options = null)
   TValue  Deserialize<TValue>(string json, JsonSerializerOptions options = null)
   TValue  Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options = null)
   object  Deserialize(string json, Type returnType, JsonSerializerOptions options = null)

   Each call is one self-contained operation with a fresh reference scope. Any
   supplied JsonSerializerOptions is treated as a settings TEMPLATE and copied, so
   it is never mutated; do not pass options previously returned by this type.

Requirements for a referenceable type: it must be constructible by STJ (a usable
parameterless constructor) and expose settable members, because cycles are
restored by creating the instance first and populating it afterwards. A type that
cannot be constructed this way throws a clear JsonException on read.

Wire format (matches Newtonsoft's shape):
    First occurrence:  { "$id": "5", "type": "sprite", ...members... }
    Later occurrence:  { "$ref": "5" }


CORE API REFERENCE - REFERENCES, FEATURE B ([JsonReferenceById])
----------------------------------------------------------------
Namespace: CodeBrix.Json.Extensions.References

For graphs whose shared entities already have stable ids: a member is serialized
as JUST its identifier and resolved back to the live instance on read.

IJsonReferenceable<out TId>
   TId JsonReferenceId { get; }   // the entity's stable identifier

[JsonReferenceById]  (property/field, Inherited = true)
   Marks a member whose value is an IJsonReferenceable<TId> entity to be written
   as just its id (not inlined) and resolved on read. Takes effect only through
   ReferenceByIdJson (or options it configures); a plain JsonSerializer call
   ignores it and inlines the member as usual.

JsonReferenceRegistry  (id -> instance map; caller-owned)
   void  Register(object entity)                          // by its IJsonReferenceable id
   bool  TryResolve(Type type, object id, out object e)   // exact, then assignable
   void  ResolveOrDefer(Type type, object id, Action<object> apply)  // forward refs

ReferenceByIdJson  (static entry point)
   string  Serialize<TValue>(TValue value, JsonSerializerOptions options = null)
   byte[]  SerializeToUtf8Bytes<TValue>(TValue value, JsonSerializerOptions options = null)
   TValue  Deserialize<TValue>(string json, JsonReferenceRegistry registry, JsonSerializerOptions options = null)
   TValue  Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonReferenceRegistry registry, JsonSerializerOptions options = null)

Read uses a "two-phase apply": populate the registry with the authoritative
entities (the owning collections) first, THEN deserialize the referencing graph,
so forward references resolve. For a reference whose target is not yet present,
ResolveOrDefer records a fixup that runs when the target is later registered. An
identifier with no registered (and no deferred) target throws JsonException.


ERROR MODEL
  - JsonException             The JSON value is not an object where one is
                              required; a discriminator is missing/unmatched and
                              no fallback is declared; a base-type instance is
                              written; a "$ref" names an unknown id; a referenceable
                              type cannot be constructed; or a by-id identifier
                              cannot be resolved.
  - InvalidOperationException A polymorphic base declares no [JsonDiscriminator];
                              a known/fallback type maps to the base type itself,
                              is not assignable, is declared twice, or is
                              abstract/an interface without its own discriminator.
  - ArgumentNullException     A required argument (inputType, returnType, registry,
                              entity, type, apply) is null.
  - ArgumentException         An entity registered with JsonReferenceRegistry does
                              not implement IJsonReferenceable<TId>.


CHOOSING A REFERENCE STRATEGY
  Feature A ($id/$ref)         Feature B ([JsonReferenceById])
  --------------------------   --------------------------------
  Mechanical Newtonsoft port   Clean redesign with existing stable ids
  Newtonsoft-shaped sentinels  Plain, human-readable id values
  Whole graph in one document  Owning sets can be split / registered separately
  No model requirement         Entity must expose a stable id
Pick one per relationship for clarity; both are opt-in and add no cost to types
that use neither.


CODING CONVENTIONS (CodeBrix family)
------------------------------------
This repository follows the CodeBrix family conventions. When modifying it:

  - Target framework is net10.0 only. Never multi-target or add older TFMs.
  - Nullable reference types are OFF. Do NOT add <Nullable>enable</Nullable>,
    do NOT write "?" on reference types (string?, MyClass?), and do NOT use the
    null-forgiveness "!" operator. Value-type nullables (int?, bool?, enum?) are
    fine - they are Nullable<T>.
  - Implicit usings are OFF and there are NO "global using" directives. Every
    file declares its own usings, fully qualified, in one contiguous block at
    the top (System.* first, then others, alphabetical within each group).
  - Namespaces are file-scoped (namespace X;), never block-scoped. Files are
    grouped into Polymorphism/ and References/ folders (with Internal/ subfolders);
    the public entry-point types live in the matching feature sub-namespace.
  - <GenerateDocumentationFile> is true, so every public (and protected-on-
    unsealed) member carries an XML doc comment. Fix CS1591 by writing the
    comment at source - never suppress warnings with <NoWarn> or #pragma.
  - No project-level warning suppression of any kind.
  - Tests use xUnit v3 with SilverAssertions (fluent x.Should().Be(y) form).


ARCHITECTURE
------------
    src/CodeBrix.Json.Extensions/
      Polymorphism/
        JsonDiscriminatorAttribute.cs    Discriminator-property declaration.
        JsonKnownTypeAttribute.cs        Value -> concrete-type mapping.
        JsonFallbackTypeAttribute.cs     Catch-all fallback-type declaration.
        FallbackTypeConverterFactory.cs  JsonConverterFactory entry point.
        FallbackTypeConverter.cs         JsonConverter<T> that dispatches.
        Internal/
          DiscriminatorMap.cs            Cached, validated per-base-type map of
                                         property name + known types + fallback,
                                         and the shared ResolveTargetType dispatch
                                         (used by both the polymorphism and the
                                         reference-aware converters).
      References/
        JsonReferenceableAttribute.cs    [JsonReferenceable] opt-in marker.
        ReferenceJson.cs                 $id/$ref serialize/deserialize entry point.
        IJsonReferenceable.cs            Stable-id interface for by-id references.
        JsonReferenceByIdAttribute.cs    [JsonReferenceById] member marker.
        JsonReferenceRegistry.cs         id -> instance map + deferred fixups.
        ReferenceByIdJson.cs             By-id serialize/deserialize entry point.
        Internal/
          ReferenceScope.cs              Per-operation $id/$ref registry.
          ReferenceAwareConverter.cs     $id/$ref converter (register-before-populate).
          ReferenceAwareConverterFactory.cs  Builds it per operation with the scope.
          JsonReferenceByIdConverter.cs  Member converter (writes/reads the id).
          ReferenceByIdModifier.cs       JsonTypeInfo modifier that installs it.
      InternalsVisibleTo.cs              Grants internals to the .Tests project.

How the reference-aware converter avoids STJ internals: STJ hands converters the
options but not the operation's reference registry. ReferenceJson therefore runs
each call as a self-contained operation whose per-operation ReferenceScope is
reached only through options-owned converter instances (no thread-static, no STJ
internals). Because a [JsonReferenceable] type carries a custom converter, its
object contract cannot be read from the operation options; a sibling "metadata"
options WITHOUT the reference factory supplies the contract (Properties / Get /
Set / CreateObject) while the operation options serialize member VALUES so nested
references stay tracked. Read creates and registers the instance BEFORE populating
its members, which is what makes cycles round-trip.


TESTING
-------
Tests live in tests/CodeBrix.Json.Extensions.Tests (xUnit v3 + SilverAssertions).
They cover: each public attribute's construction/validation; the polymorphism
factory and converter's full Read/Write dispatch matrix (known values, fallback
paths, missing/null/numeric discriminators, case-insensitive matching, multi-level
dispatch, and every invalid-configuration error path); the reference-aware round
trip of shared references, cycles, self-loops, diamonds, polymorphic + referenceable
composition, ignored members, camelCase/case-insensitive options, and error paths;
and the by-id write/read, two-phase apply, deferred fixups, Guid/int/string id
types, and registry validation. Internal helpers (DiscriminatorMap) are exercised
directly via InternalsVisibleTo. Shared test types live in TestShapes.cs (polymorphism),
ReferenceShapes.cs (Feature A), and ByIdShapes.cs (Feature B).

    dotnet test CodeBrix.Json.Extensions.slnx

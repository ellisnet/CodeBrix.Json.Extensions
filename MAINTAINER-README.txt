================================================================================
MAINTAINER-README: CodeBrix.Json.Extensions
Notes for people and agents MAINTAINING this repository — not for package consumers
================================================================================

If you are consuming the NuGet package, stop reading and open AGENT-README.txt
instead. This file is about the repository itself.

PURPOSE AND SCOPE
=================
This repository produces exactly one NuGet package:

    CodeBrix.Json.Extensions.MitLicenseForever
        Assembly / root namespace: CodeBrix.Json.Extensions
        Consumer documentation:    AGENT-README.txt (repo root)
        Project:                   src/CodeBrix.Json.Extensions/CodeBrix.Json.Extensions.csproj

The package has two feature areas - Polymorphism and References - and 11 public
types in total. There are no sibling packages and no native assets.

REPOSITORY LAYOUT
=================

    CodeBrix.Json.Extensions.slnx     Solution (Solution Items + Tests folder).
    icon-codebrix-128.png             Package icon; packed from the repo root.
    LICENSE                           MIT.
    THIRD-PARTY-NOTICES.txt           Packed into the nupkg. Records that no
                                      third-party source is incorporated.
    README.md                         Human-facing overview; also the nuspec readme.
    AGENT-README.txt                  Consumer documentation; packed into the nupkg.
    MAINTAINER-README.txt             This file (not packed).
    EXTRAS-README.txt                 Non-package content (not packed).
    README-INDEX.txt                  Map of the README files (not packed).

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

    tests/CodeBrix.Json.Extensions.Tests/
        Single test project; shared model shapes in TestShapes.cs (polymorphism),
        ReferenceShapes.cs (Feature A) and ByIdShapes.cs (Feature B).

ARCHITECTURE NOTE - HOW THE REFERENCE CONVERTER AVOIDS STJ INTERNALS
--------------------------------------------------------------------
System.Text.Json hands converters the options but not the operation's reference
registry. ReferenceJson therefore runs each call as a self-contained operation
whose per-operation ReferenceScope is reached only through options-owned
converter instances (no thread-static, no STJ internals). Because a
[JsonReferenceable] type carries a custom converter, its object contract cannot
be read from the operation options; a sibling "metadata" options object WITHOUT
the reference factory supplies the contract (Properties / Get / Set /
CreateObject) while the operation options serialize member VALUES so nested
references stay tracked. Read creates and registers the instance BEFORE
populating its members, which is what makes cycles round-trip.

Keep that separation intact. In particular: the metadata options must never get
the ReferenceAwareConverterFactory added to it, or contract lookup will recurse.

BUILDING
========

    dotnet restore CodeBrix.Json.Extensions.slnx
    dotnet build CodeBrix.Json.Extensions.slnx

Target framework is net10.0 only. The library has zero PackageReference
dependencies; "no dependencies beyond .NET" is a documented consumer-facing
property, so keep it that way.

GenerateDocumentationFile is true for the library project, so every public (and
protected-on-unsealed) member must carry an XML doc comment. Fix CS1591 by
writing the comment at source - never suppress with <NoWarn> or #pragma.

TESTING
=======

    dotnet test CodeBrix.Json.Extensions.slnx

xUnit v3 with SilverAssertions (fluent x.Should().Be(y) form). No opt-in
environment variables, no fixtures, no external tools.

Coverage today: each public attribute's construction/validation; the
polymorphism factory and converter's full Read/Write dispatch matrix (known
values, fallback paths, missing/null/numeric discriminators, case-insensitive
property-name matching, multi-level dispatch, and every invalid-configuration
error path); the reference-aware round trip of shared references, cycles,
self-loops, diamonds, polymorphic + referenceable composition, ignored members,
camelCase/case-insensitive options, and error paths; and the by-id write/read,
two-phase apply, deferred fixups, Guid/int/string id types, and registry
validation. Internal helpers (DiscriminatorMap) are exercised directly through
InternalsVisibleTo.

PACKAGING AND PUBLISHING
========================
Packing is driven entirely from
src/CodeBrix.Json.Extensions/CodeBrix.Json.Extensions.csproj:

  - GeneratePackageOnBuild is true, so every build produces a fresh .nupkg.
  - PackageId is CodeBrix.Json.Extensions.MitLicenseForever; Product/Title are
    CodeBrix.Json.Extensions; AssemblyName and RootNamespace are
    CodeBrix.Json.Extensions.
  - PackageLicenseExpression is MIT and PackageRequireLicenseAcceptance is true.
  - Files packed from the repo root: icon-codebrix-128.png (PackageIcon),
    README.md (PackageReadmeFile), AGENT-README.txt and THIRD-PARTY-NOTICES.txt.
    AGENT-README.txt is the ONLY README variant that ships in the package;
    MAINTAINER-README.txt, EXTRAS-README.txt and README-INDEX.txt do not.

VERSIONING SCHEME: date-stamped and auto-incrementing, 1.<x>.<y>.<z>, where x is
whole years since the _VersionBaseYear property, y is the UTC day of year
(1-based), and z is the UTC minute of day (0-1439). Every build yields a new,
strictly increasing version, and two builds within the same UTC minute produce
the same version - so do not publish twice inside one minute. This is not
SemVer: major is pinned and minor encodes the year, so neither signals API
compatibility. Re-baseline by changing _VersionBaseYear in the csproj.

Never write version numbers into AGENT-README.txt; they go stale immediately
under this scheme.

PROVENANCE AND VENDORED SOURCES
===============================
None. This library is original code; it incorporates no third-party source and
builds only against the .NET base class libraries (System.Text.Json).
THIRD-PARTY-NOTICES.txt records exactly that, and must be kept accurate if a
dependency is ever added.

CODING CONVENTIONS
==================
This repository follows the CodeBrix family conventions:

  - Target framework is net10.0 only. Never multi-target or add older TFMs.
  - Nullable reference types are OFF. Do NOT add <Nullable>enable</Nullable>,
    do NOT write "?" on reference types (string?, MyClass?), and do NOT use the
    null-forgiveness "!" operator. Value-type nullables (int?, bool?, enum?) are
    fine - they are Nullable<T>.
  - Implicit usings are OFF and there are NO "global using" directives. Every
    file declares its own usings, fully qualified, in one contiguous block at
    the top (System.* first, then others, alphabetical within each group).
  - Namespaces are file-scoped (namespace X;), never block-scoped. Files are
    grouped into Polymorphism/ and References/ folders (with Internal/
    subfolders); the public entry-point types live in the matching feature
    sub-namespace, and the root namespace deliberately holds no public types.
  - <GenerateDocumentationFile> is true, so every public (and protected-on-
    unsealed) member carries an XML doc comment.
  - No project-level warning suppression of any kind.
  - Tests use xUnit v3 with SilverAssertions, //Arrange //Act //Assert comment
    blocks, and snake_case test method names.

NOTES
=====
  - Documentation split: AGENT-README.txt is consumer-only (what the package
    does, how to reference it, API, examples and pitfalls). Build/test/pack/
    versioning, architecture-internals and contributor conventions belong here;
    non-package content belongs in EXTRAS-README.txt.
  - AGENT-README.txt links to test files by GitHub URL under
    tests/CodeBrix.Json.Extensions.Tests/. If a test file is renamed or moved,
    fix those links in the same change.
  - The public surface is small (11 types) and fully documented in
    AGENT-README.txt. Any new public type must be added there in the same change,
    with its signature.

================================================================================

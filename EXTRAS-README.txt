================================================================================
EXTRAS-README: CodeBrix.Json.Extensions
Samples, tools and other content in this repository that is not part of a NuGet package
================================================================================

This repository contains no sample applications, demo projects, tools or
optional test-data downloads. It builds one library and one test project, and
nothing else.

TEST PROJECT
============
The only non-package content is the test project:

    tests/CodeBrix.Json.Extensions.Tests/

It is an xUnit v3 project (with SilverAssertions) covering both feature areas:
the polymorphism attributes, factory and converter, and both reference
strategies ($id/$ref and by-identifier). Its files double as the worked examples
that AGENT-README.txt links to under "WORKING EXAMPLES ON GITHUB", and its three
shape files are the readable model catalogue:

    TestShapes.cs        Polymorphic bases and derived types, including a numeric
                         discriminator and a two-level dispatch chain.
    ReferenceShapes.cs   Referenceable nodes, cycles, a non-constructible type,
                         ignored members, and the polymorphic + referenceable
                         composition.
    ByIdShapes.cs        Entities keyed by string, Guid and int, plus a
                         deliberately invalid by-id member.

How to run it:

    dotnet test CodeBrix.Json.Extensions.slnx

No environment variables, downloads, fixtures or external tools are required.

================================================================================

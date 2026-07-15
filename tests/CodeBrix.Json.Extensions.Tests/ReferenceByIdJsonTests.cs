using System;
using System.Collections.Generic;
using System.Text.Json;
using CodeBrix.Json.Extensions.References;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Json.Extensions.Tests;

public class ReferenceByIdJsonTests
{
    [Fact]
    public void By_id_member_is_written_as_the_identifier_not_inlined()
    {
        //Arrange
        var scene = new Scene { Id = "s1", Name = "Secret" };

        //Act
        var json = ReferenceByIdJson.Serialize(new SceneRef { Target = scene, Label = "L" });

        //Assert
        json.Should().Contain("\"Target\":\"s1\"");
        json.Should().NotContain("Secret");
    }

    [Fact]
    public void By_id_member_round_trips_to_the_registered_instance()
    {
        //Arrange
        var scene = new Scene { Id = "s1", Name = "First" };
        var registry = new JsonReferenceRegistry();
        registry.Register(scene);
        var json = ReferenceByIdJson.Serialize(new SceneRef { Target = scene, Label = "L" });

        //Act
        var back = ReferenceByIdJson.Deserialize<SceneRef>(json, registry);

        //Assert
        back.Target.Should().BeSameAs(scene);
        back.Label.Should().Be("L");
    }

    [Fact]
    public void Two_by_id_members_to_the_same_entity_resolve_to_one_instance()
    {
        //Arrange
        var scene = new Scene { Id = "s1", Name = "First" };
        var registry = new JsonReferenceRegistry();
        registry.Register(scene);
        var refs = new List<SceneRef>
        {
            new SceneRef { Target = scene, Label = "a" },
            new SceneRef { Target = scene, Label = "b" },
        };
        var json = ReferenceByIdJson.Serialize(refs);

        //Act
        var back = ReferenceByIdJson.Deserialize<List<SceneRef>>(json, registry);

        //Assert
        back[0].Target.Should().BeSameAs(back[1].Target);
        back[0].Target.Should().BeSameAs(scene);
    }

    [Fact]
    public void Two_phase_apply_resolves_references_after_owning_collection_is_registered()
    {
        //Arrange — the referencing part is authored/serialized independently of the owning collection.
        var scenes = new List<Scene>
        {
            new Scene { Id = "s1", Name = "One" },
            new Scene { Id = "s2", Name = "Two" },
        };
        var refsJson = ReferenceByIdJson.Serialize(new SceneRefList
        {
            Refs = new List<SceneRef>
            {
                new SceneRef { Target = scenes[1], Label = "points-to-two" },
                new SceneRef { Target = scenes[0], Label = "points-to-one" },
            },
        });

        //Act — phase 1: register the owning collection; phase 2: resolve the references.
        var registry = new JsonReferenceRegistry();
        foreach (var scene in scenes)
        {
            registry.Register(scene);
        }
        var back = ReferenceByIdJson.Deserialize<SceneRefList>(refsJson, registry);

        //Assert
        back.Refs[0].Target.Name.Should().Be("Two");
        back.Refs[1].Target.Name.Should().Be("One");
    }

    [Fact]
    public void Null_by_id_member_round_trips_as_null()
    {
        //Arrange
        var json = ReferenceByIdJson.Serialize(new SceneRef { Target = null, Label = "x" });

        //Act
        var back = ReferenceByIdJson.Deserialize<SceneRef>(json, new JsonReferenceRegistry());

        //Assert
        back.Target.Should().BeNull();
    }

    [Fact]
    public void Unresolved_identifier_throws_JsonException()
    {
        //Arrange
        const string json = "{\"Target\":\"missing\",\"Label\":\"x\"}";

        //Act
        Action act = () => ReferenceByIdJson.Deserialize<SceneRef>(json, new JsonReferenceRegistry());

        //Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Guid_identifier_type_round_trips()
    {
        //Arrange
        var id = Guid.NewGuid();
        var entity = new GuidEntity { Ref = id, Name = "G" };
        var registry = new JsonReferenceRegistry();
        registry.Register(entity);
        var json = ReferenceByIdJson.Serialize(new GuidHolder { Entity = entity });

        //Act
        var back = ReferenceByIdJson.Deserialize<GuidHolder>(json, registry);

        //Assert
        back.Entity.Should().BeSameAs(entity);
    }

    [Fact]
    public void Int_identifier_type_round_trips()
    {
        //Arrange
        var entity = new IntEntity { Number = 42 };
        var registry = new JsonReferenceRegistry();
        registry.Register(entity);
        var json = ReferenceByIdJson.Serialize(new IntHolder { Entity = entity });

        //Act
        var back = ReferenceByIdJson.Deserialize<IntHolder>(json, registry);

        //Assert
        back.Entity.Should().BeSameAs(entity);
    }

    [Fact]
    public void TryResolve_matches_a_derived_entity_through_its_base_type()
    {
        //Arrange
        var special = new SpecialScene { Id = "sp", Name = "S", Featured = true };
        var registry = new JsonReferenceRegistry();
        registry.Register(special);

        //Act
        var found = registry.TryResolve(typeof(Scene), "sp", out var entity);

        //Assert
        found.Should().BeTrue();
        entity.Should().BeSameAs(special);
    }

    [Fact]
    public void ResolveOrDefer_runs_the_fixup_when_the_target_is_registered_later()
    {
        //Arrange
        var registry = new JsonReferenceRegistry();
        Scene captured = null;
        registry.ResolveOrDefer(typeof(Scene), "s1", entity => captured = (Scene)entity);

        //Act
        captured.Should().BeNull();
        registry.Register(new Scene { Id = "s1", Name = "Late" });

        //Assert
        captured.Should().NotBeNull();
        captured.Name.Should().Be("Late");
    }

    [Fact]
    public void ResolveOrDefer_runs_immediately_when_the_target_is_already_registered()
    {
        //Arrange
        var registry = new JsonReferenceRegistry();
        var scene = new Scene { Id = "s1", Name = "Now" };
        registry.Register(scene);
        Scene captured = null;

        //Act
        registry.ResolveOrDefer(typeof(Scene), "s1", entity => captured = (Scene)entity);

        //Assert
        captured.Should().BeSameAs(scene);
    }

    [Fact]
    public void Register_throws_on_null_entity()
    {
        //Arrange
        Action act = () => new JsonReferenceRegistry().Register(null);

        //Act + Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_throws_when_entity_is_not_referenceable()
    {
        //Arrange
        Action act = () => new JsonReferenceRegistry().Register(new PlainTag { Text = "x" });

        //Act + Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deserialize_throws_on_null_registry()
    {
        //Arrange
        Action act = () => ReferenceByIdJson.Deserialize<SceneRef>("{}", null);

        //Act + Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryResolve_returns_false_for_unregistered_identifier()
        => new JsonReferenceRegistry().TryResolve(typeof(Scene), "nope", out _).Should().BeFalse();

    [Fact]
    public void Utf8_bytes_round_trip_resolves_by_id()
    {
        //Arrange
        var scene = new Scene { Id = "s1", Name = "First" };
        var registry = new JsonReferenceRegistry();
        registry.Register(scene);
        var bytes = ReferenceByIdJson.SerializeToUtf8Bytes(new SceneRef { Target = scene, Label = "L" });

        //Act
        var back = ReferenceByIdJson.Deserialize<SceneRef>(bytes, registry);

        //Assert
        back.Target.Should().BeSameAs(scene);
    }

    [Fact]
    public void TryResolve_throws_on_null_type()
    {
        //Arrange
        Action act = () => new JsonReferenceRegistry().TryResolve(null, "x", out _);

        //Act + Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ResolveOrDefer_throws_on_null_type()
    {
        //Arrange
        Action act = () => new JsonReferenceRegistry().ResolveOrDefer(null, "x", _ => { });

        //Act + Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ResolveOrDefer_throws_on_null_apply()
    {
        //Arrange
        Action act = () => new JsonReferenceRegistry().ResolveOrDefer(typeof(Scene), "x", null);

        //Act + Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void By_id_member_whose_type_is_not_referenceable_throws()
    {
        //Arrange
        Action act = () => ReferenceByIdJson.Serialize(
            new BadByIdHolder { NotReferenceable = new PlainTag { Text = "x" } });

        //Act + Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Honors_a_supplied_options_template()
    {
        //Arrange
        var options = new JsonSerializerOptions { WriteIndented = true };
        var scene = new Scene { Id = "s1", Name = "First" };
        var registry = new JsonReferenceRegistry();
        registry.Register(scene);
        var json = ReferenceByIdJson.Serialize(new SceneRef { Target = scene, Label = "L" }, options);

        //Act
        var back = ReferenceByIdJson.Deserialize<SceneRef>(json, registry, options);

        //Assert
        json.Should().Contain("\n");
        back.Target.Should().BeSameAs(scene);
    }

    [Fact]
    public void Distinct_entities_resolve_to_distinct_instances()
    {
        //Arrange
        var s1 = new Scene { Id = "s1", Name = "One" };
        var s2 = new Scene { Id = "s2", Name = "Two" };
        var registry = new JsonReferenceRegistry();
        registry.Register(s1);
        registry.Register(s2);
        var refs = new List<SceneRef>
        {
            new SceneRef { Target = s1, Label = "a" },
            new SceneRef { Target = s2, Label = "b" },
        };

        //Act
        var back = ReferenceByIdJson.Deserialize<List<SceneRef>>(ReferenceByIdJson.Serialize(refs), registry);

        //Assert
        back[0].Target.Should().BeSameAs(s1);
        back[1].Target.Should().BeSameAs(s2);
    }
}

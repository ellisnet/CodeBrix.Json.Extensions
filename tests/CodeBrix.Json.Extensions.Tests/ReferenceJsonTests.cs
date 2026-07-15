using System;
using System.Collections.Generic;
using System.Text.Json;
using CodeBrix.Json.Extensions.References;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Json.Extensions.Tests;

public class ReferenceJsonTests
{
    [Fact]
    public void Shared_reference_in_a_list_round_trips_to_one_instance()
    {
        //Arrange
        var shared = new RefNode { Name = "hero" };
        var list = new List<RefNode> { shared, shared };

        //Act
        var json = ReferenceJson.Serialize(list);
        var back = ReferenceJson.Deserialize<List<RefNode>>(json);

        //Assert
        back[0].Should().BeSameAs(back[1]);
        back[0].Name.Should().Be("hero");
    }

    [Fact]
    public void Second_occurrence_is_written_as_ref_not_inlined()
    {
        //Arrange
        var shared = new RefNode { Name = "hero" };

        //Act
        var json = ReferenceJson.Serialize(new List<RefNode> { shared, shared });

        //Assert
        json.Should().Contain("\"$id\":\"1\"");
        json.Should().Contain("\"$ref\":\"1\"");
    }

    [Fact]
    public void Mutual_cycle_round_trips_with_preserved_identity()
    {
        //Arrange
        var a = new RefNode { Name = "a" };
        var b = new RefNode { Name = "b" };
        a.Link = b;
        b.Link = a;

        //Act
        var json = ReferenceJson.Serialize(a);
        var back = ReferenceJson.Deserialize<RefNode>(json);

        //Assert
        back.Link.Name.Should().Be("b");
        back.Link.Link.Should().BeSameAs(back);
    }

    [Fact]
    public void Self_loop_round_trips_with_preserved_identity()
    {
        //Arrange
        var c = new RefNode { Name = "c" };
        c.Link = c;

        //Act
        var back = ReferenceJson.Deserialize<RefNode>(ReferenceJson.Serialize(c));

        //Assert
        back.Link.Should().BeSameAs(back);
    }

    [Fact]
    public void Diamond_shared_grandchild_resolves_to_single_instance()
    {
        //Arrange
        var grandchild = new RefNode { Name = "leaf" };
        var left = new RefNode { Name = "left", Link = grandchild };
        var right = new RefNode { Name = "right", Link = grandchild };
        var root = new RefNode { Name = "root", Friends = new List<RefNode> { left, right } };

        //Act
        var back = ReferenceJson.Deserialize<RefNode>(ReferenceJson.Serialize(root));

        //Assert
        back.Friends[0].Link.Should().BeSameAs(back.Friends[1].Link);
        back.Friends[0].Link.Name.Should().Be("leaf");
    }

    [Fact]
    public void Non_referenceable_nested_object_is_inlined_without_id()
    {
        //Arrange
        var node = new TaggedNode { Name = "n", Tag = new PlainTag { Text = "hi" } };

        //Act
        var json = ReferenceJson.Serialize(node);
        var back = ReferenceJson.Deserialize<TaggedNode>(json);

        //Assert
        json.Should().Contain("\"Text\":\"hi\"");
        back.Tag.Text.Should().Be("hi");
    }

    [Fact]
    public void Polymorphic_referenceable_round_trips_concrete_type_and_identity()
    {
        //Arrange
        var car = new Car { Kind = "car", Name = "hero-car", Doors = 2 };
        car.Tows = car;

        //Act
        var json = ReferenceJson.Serialize<Vehicle>(car);
        var back = ReferenceJson.Deserialize<Vehicle>(json);

        //Assert
        var backCar = back.Should().BeOfType<Car>().Subject;
        backCar.Doors.Should().Be(2);
        backCar.Tows.Should().BeSameAs(backCar);
    }

    [Fact]
    public void Polymorphic_referenceable_list_dispatches_each_element()
    {
        //Arrange
        var vehicles = new List<Vehicle>
        {
            new Car { Kind = "car", Name = "c", Doors = 4 },
            new Truck { Kind = "truck", Name = "t", Payload = 1.5 },
        };

        //Act
        var back = ReferenceJson.Deserialize<List<Vehicle>>(ReferenceJson.Serialize(vehicles));

        //Assert
        back[0].Should().BeOfType<Car>();
        back[1].Should().BeOfType<Truck>();
    }

    [Fact]
    public void Polymorphic_referenceable_falls_back_on_unknown_kind()
    {
        //Arrange
        var json = ReferenceJson.Serialize<Vehicle>(new Car { Kind = "spaceship", Name = "x", Doors = 0 });

        //Act
        var back = ReferenceJson.Deserialize<Vehicle>(json);

        //Assert
        back.Should().BeOfType<UnknownVehicle>();
    }

    [Fact]
    public void Camel_case_naming_policy_is_honored_and_round_trips()
    {
        //Arrange
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var node = new RefNode { Name = "n" };

        //Act
        var json = ReferenceJson.Serialize(node, options);
        var back = ReferenceJson.Deserialize<RefNode>(json, options);

        //Assert
        json.Should().Contain("\"name\":\"n\"");
        back.Name.Should().Be("n");
    }

    [Fact]
    public void Case_insensitive_option_matches_differently_cased_members()
    {
        //Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        const string json = "{\"$id\":\"1\",\"NAME\":\"n\",\"link\":null,\"friends\":null}";

        //Act
        var back = ReferenceJson.Deserialize<RefNode>(json, options);

        //Assert
        back.Name.Should().Be("n");
    }

    [Fact]
    public void Null_root_serializes_and_deserializes_as_null()
    {
        ReferenceJson.Serialize<RefNode>(null).Should().Be("null");
        ReferenceJson.Deserialize<RefNode>("null").Should().BeNull();
    }

    [Fact]
    public void Null_referenceable_member_round_trips_as_null()
    {
        //Arrange
        var node = new RefNode { Name = "n", Link = null };

        //Act
        var back = ReferenceJson.Deserialize<RefNode>(ReferenceJson.Serialize(node));

        //Assert
        back.Link.Should().BeNull();
    }

    [Fact]
    public void Utf8_bytes_round_trip_preserves_identity()
    {
        //Arrange
        var shared = new RefNode { Name = "hero" };

        //Act
        var bytes = ReferenceJson.SerializeToUtf8Bytes(new List<RefNode> { shared, shared });
        var back = ReferenceJson.Deserialize<List<RefNode>>(bytes);

        //Assert
        back[0].Should().BeSameAs(back[1]);
    }

    [Fact]
    public void Unknown_ref_id_throws_JsonException()
    {
        //Arrange
        const string json = "{\"$ref\":\"99\"}";

        //Act
        System.Action act = () => ReferenceJson.Deserialize<RefNode>(json);

        //Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Referenceable_type_without_parameterless_constructor_throws_on_read()
    {
        //Arrange
        const string json = "{\"$id\":\"1\",\"Name\":\"n\"}";

        //Act
        System.Action act = () => ReferenceJson.Deserialize<RefNoDefaultCtor>(json);

        //Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Each_operation_uses_a_fresh_scope_so_ids_restart()
    {
        //Arrange
        var shared = new RefNode { Name = "hero" };
        var list = new List<RefNode> { shared, shared };

        //Act
        var first = ReferenceJson.Serialize(list);
        var second = ReferenceJson.Serialize(list);

        //Assert
        first.Should().Be(second);
        first.Should().Contain("\"$id\":\"1\"");
    }

    [Fact]
    public void Supplied_options_template_is_not_mutated()
    {
        //Arrange
        var options = new JsonSerializerOptions();
        var before = options.Converters.Count;

        //Act
        ReferenceJson.Serialize(new RefNode { Name = "n" }, options);

        //Assert
        options.Converters.Count.Should().Be(before);
    }

    [Fact]
    public void Non_object_json_for_referenceable_type_throws_JsonException()
    {
        //Arrange
        System.Action act = () => ReferenceJson.Deserialize<RefNode>("\"nope\"");

        //Act + Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Non_generic_serialize_and_deserialize_round_trip_by_type()
    {
        //Arrange
        var shared = new RefNode { Name = "hero" };

        //Act
        var json = ReferenceJson.Serialize(new List<RefNode> { shared, shared }, typeof(List<RefNode>));
        var back = (List<RefNode>)ReferenceJson.Deserialize(json, typeof(List<RefNode>));

        //Assert
        back[0].Should().BeSameAs(back[1]);
    }

    [Fact]
    public void Serialize_throws_on_null_input_type()
    {
        //Arrange
        Action act = () => ReferenceJson.Serialize(new RefNode(), (Type)null);

        //Act + Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Deserialize_throws_on_null_return_type()
    {
        //Arrange
        Action act = () => ReferenceJson.Deserialize("{}", null);

        //Act + Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Distinct_instances_get_distinct_ids_and_resolve_separately()
    {
        //Arrange
        var a = new RefNode { Name = "a" };
        var b = new RefNode { Name = "b" };

        //Act
        var back = ReferenceJson.Deserialize<List<RefNode>>(
            ReferenceJson.Serialize(new List<RefNode> { a, b }));

        //Assert
        back[0].Should().NotBeSameAs(back[1]);
        back[0].Name.Should().Be("a");
        back[1].Name.Should().Be("b");
    }

    [Fact]
    public void Serializing_a_bare_polymorphic_base_instance_throws_JsonException()
    {
        //Arrange
        Action act = () => ReferenceJson.Serialize<Vehicle>(new Vehicle { Kind = "car", Name = "bare" });

        //Act + Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Ignored_member_is_absent_from_the_envelope_and_round_trips()
    {
        //Arrange
        var node = new SecretiveNode { Name = "n", Secret = "shh" };

        //Act
        var json = ReferenceJson.Serialize(node);
        var back = ReferenceJson.Deserialize<SecretiveNode>(json);

        //Assert
        json.Should().NotContain("shh");
        back.Name.Should().Be("n");
        back.Secret.Should().BeNull();
    }
}

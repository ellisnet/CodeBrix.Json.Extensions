using System;
using System.Text.Json;
using CodeBrix.Json.Extensions.Polymorphism.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Json.Extensions.Tests;

public class DiscriminatorMapTests
{
    [Fact]
    public void Get_returns_cached_instance_on_repeat_calls()
        => DiscriminatorMap.Get(typeof(IShape)).Should().BeSameAs(DiscriminatorMap.Get(typeof(IShape)));

    [Fact]
    public void Get_exposes_declared_discriminator_property_name()
        => DiscriminatorMap.Get(typeof(Animal)).PropertyName.Should().Be("kind");

    [Fact]
    public void Get_builds_known_type_dictionary_from_attributes()
    {
        //Arrange + Act
        var map = DiscriminatorMap.Get(typeof(IShape));

        //Assert
        map.KnownTypes["circle"].Should().Be(typeof(Circle));
        map.KnownTypes["square"].Should().Be(typeof(Square));
    }

    [Fact]
    public void Get_exposes_declared_fallback_type()
        => DiscriminatorMap.Get(typeof(IShape)).FallbackType.Should().Be(typeof(UnknownShape));

    [Fact]
    public void Get_leaves_fallback_null_when_not_declared()
        => DiscriminatorMap.Get(typeof(IStrict)).FallbackType.Should().BeNull();

    [Fact]
    public void HasDiscriminator_is_true_for_attributed_type_and_false_otherwise()
    {
        DiscriminatorMap.HasDiscriminator(typeof(IShape)).Should().BeTrue();
        DiscriminatorMap.HasDiscriminator(typeof(PlainUnattributed)).Should().BeFalse();
    }

    [Fact]
    public void ResolveTargetType_maps_known_value_to_concrete_type()
    {
        //Arrange
        using var document = JsonDocument.Parse("{\"type\":\"circle\"}");

        //Act
        var target = DiscriminatorMap.Get(typeof(IShape))
            .ResolveTargetType(document.RootElement, new JsonSerializerOptions());

        //Assert
        target.Should().Be(typeof(Circle));
    }

    [Fact]
    public void ResolveTargetType_uses_fallback_for_unknown_value()
    {
        //Arrange
        using var document = JsonDocument.Parse("{\"type\":\"hexagon\"}");

        //Act
        var target = DiscriminatorMap.Get(typeof(IShape))
            .ResolveTargetType(document.RootElement, new JsonSerializerOptions());

        //Assert
        target.Should().Be(typeof(UnknownShape));
    }

    [Fact]
    public void ResolveTargetType_throws_when_unmatched_and_no_fallback()
    {
        //Arrange
        using var document = JsonDocument.Parse("{\"type\":\"b\"}");

        Action act = () => DiscriminatorMap.Get(typeof(IStrict))
            .ResolveTargetType(document.RootElement, new JsonSerializerOptions());

        //Act + Assert
        act.Should().Throw<JsonException>();
    }
}

using System;
using CodeBrix.Json.Extensions.Polymorphism;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Json.Extensions.Tests;

public class JsonDiscriminatorAttributeTests
{
    [Fact]
    public void PropertyName_round_trips_from_constructor()
        => new JsonDiscriminatorAttribute("type").PropertyName.Should().Be("type");

    [Fact]
    public void Constructor_throws_on_null_property_name()
    {
        //Arrange
        Action act = () => _ = new JsonDiscriminatorAttribute(null);

        //Act + Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_throws_on_whitespace_property_name()
    {
        //Arrange
        Action act = () => _ = new JsonDiscriminatorAttribute("   ");

        //Act + Assert
        act.Should().Throw<ArgumentException>();
    }
}

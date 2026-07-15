using System;
using CodeBrix.Json.Extensions.Polymorphism;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Json.Extensions.Tests;

public class JsonFallbackTypeAttributeTests
{
    [Fact]
    public void FallbackType_round_trips_from_constructor()
        => new JsonFallbackTypeAttribute(typeof(UnknownShape)).FallbackType.Should().Be(typeof(UnknownShape));

    [Fact]
    public void Constructor_throws_on_null_fallback_type()
    {
        //Arrange
        Action act = () => _ = new JsonFallbackTypeAttribute(null);

        //Act + Assert
        act.Should().Throw<ArgumentNullException>();
    }
}

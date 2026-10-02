using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using CodeBrix.Json.Extensions.References;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Json.Extensions.Tests;

public class ResolverTests
{
    [Fact]
    public void supplied_metadata_resolver_preserves_cycles_without_mutating_options()
    {
        //Arrange
        var options = new JsonSerializerOptions { TypeInfoResolver = ResolverTestContext.Default };
        options.MakeReadOnly();
        var node = new RefNode { Name = "Android" };
        node.Link = node;

        //Act
        var json = ReferenceJson.Serialize(node, options);
        var result = ReferenceJson.Deserialize<RefNode>(json, options);

        //Assert
        result.Name.Should().Be("Android");
        result.Link.Should().BeSameAs(result);
        options.TypeInfoResolver.Should().BeSameAs(ResolverTestContext.Default);
        options.Converters.Should().BeEmpty();
    }

    [Fact]
    public void explicit_resolver_is_not_replaced_with_reflection()
    {
        //Arrange
        var options = new JsonSerializerOptions { TypeInfoResolver = JsonTypeInfoResolver.Combine() };

        //Act
        Action action = () => ReferenceJson.Serialize(new RefNode(), options);

        //Assert
        action.Should().Throw<NotSupportedException>();
    }
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(RefNode))]
internal partial class ResolverTestContext : JsonSerializerContext;

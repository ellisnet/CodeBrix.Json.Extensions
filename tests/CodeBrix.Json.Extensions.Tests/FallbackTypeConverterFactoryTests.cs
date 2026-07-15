using System.Text.Json;
using CodeBrix.Json.Extensions.Polymorphism;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Json.Extensions.Tests;

public class FallbackTypeConverterFactoryTests
{
    [Fact]
    public void CanConvert_returns_true_for_attributed_interface()
        => new FallbackTypeConverterFactory().CanConvert(typeof(IShape)).Should().BeTrue();

    [Fact]
    public void CanConvert_returns_true_for_attributed_class()
        => new FallbackTypeConverterFactory().CanConvert(typeof(Note)).Should().BeTrue();

    [Fact]
    public void CanConvert_returns_false_for_unattributed_type()
        => new FallbackTypeConverterFactory().CanConvert(typeof(PlainUnattributed)).Should().BeFalse();

    [Fact]
    public void CanConvert_returns_false_for_derived_type_without_declared_attribute()
        => new FallbackTypeConverterFactory().CanConvert(typeof(TextNote)).Should().BeFalse();

    [Fact]
    public void CanConvert_returns_false_for_value_type()
        => new FallbackTypeConverterFactory().CanConvert(typeof(int)).Should().BeFalse();

    [Fact]
    public void CreateConverter_returns_closed_generic_fallback_converter()
    {
        //Arrange
        var factory = new FallbackTypeConverterFactory();

        //Act
        var converter = factory.CreateConverter(typeof(IShape), new JsonSerializerOptions());

        //Assert
        converter.Should().BeOfType<FallbackTypeConverter<IShape>>();
    }

    [Fact]
    public void Factory_registered_in_options_converts_attributed_types()
    {
        //Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new FallbackTypeConverterFactory());

        //Act
        var animal = JsonSerializer.Deserialize<Animal>("{\"kind\":\"dog\",\"bark\":\"arf\"}", options);

        //Assert
        animal.Should().BeOfType<Dog>();
    }
}

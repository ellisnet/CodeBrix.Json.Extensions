using System;
using System.Collections.Generic;
using System.Text.Json;
using CodeBrix.Json.Extensions.Polymorphism;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Json.Extensions.Tests;

public class FallbackTypeConverterTests
{
    [Fact]
    public void Read_dispatches_known_discriminator_to_mapped_type()
    {
        //Arrange
        const string json = "{\"type\":\"circle\",\"radius\":2.5}";

        //Act
        var shape = JsonSerializer.Deserialize<IShape>(json);

        //Assert
        var circle = shape.Should().BeOfType<Circle>().Subject;
        circle.Radius.Should().Be(2.5);
    }

    [Fact]
    public void Read_dispatches_each_known_value_independently()
    {
        //Arrange
        const string json = "[{\"type\":\"circle\",\"radius\":1},{\"type\":\"square\",\"side\":3}]";

        //Act
        var shapes = JsonSerializer.Deserialize<List<IShape>>(json);

        //Assert
        shapes[0].Should().BeOfType<Circle>();
        shapes[1].Should().BeOfType<Square>();
    }

    [Fact]
    public void Read_falls_back_on_unknown_discriminator_value()
        => JsonSerializer.Deserialize<IShape>("{\"type\":\"hexagon\"}").Should().BeOfType<UnknownShape>();

    [Fact]
    public void Read_falls_back_on_missing_discriminator_property()
        => JsonSerializer.Deserialize<IShape>("{\"radius\":1}").Should().BeOfType<UnknownShape>();

    [Fact]
    public void Read_falls_back_on_null_discriminator_value()
        => JsonSerializer.Deserialize<IShape>("{\"type\":null}").Should().BeOfType<UnknownShape>();

    [Fact]
    public void Read_matches_numeric_discriminator_by_raw_text()
        => JsonSerializer.Deserialize<IShape>("{\"type\":7}").Should().BeOfType<NumberedShape>();

    [Fact]
    public void Read_works_for_abstract_class_base()
    {
        //Arrange
        const string json = "{\"kind\":\"dog\",\"bark\":\"woof\"}";

        //Act
        var animal = JsonSerializer.Deserialize<Animal>(json);

        //Assert
        var dog = animal.Should().BeOfType<Dog>().Subject;
        dog.Bark.Should().Be("woof");
    }

    [Fact]
    public void Read_works_for_concrete_class_base_with_fallback_subclass()
    {
        //Arrange
        const string json = "[{\"type\":\"text\",\"text\":\"hi\"},{\"type\":\"mystery\"}]";

        //Act
        var notes = JsonSerializer.Deserialize<List<Note>>(json);

        //Assert
        notes[0].Should().BeOfType<TextNote>();
        notes[1].Should().BeOfType<UnknownNote>();
    }

    [Fact]
    public void Read_dispatches_two_levels_of_discriminators()
    {
        //Arrange
        const string json = "{\"object\":\"widget\",\"type\":\"button\",\"label\":\"go\"}";

        //Act
        var entity = JsonSerializer.Deserialize<IEntity>(json);

        //Assert
        var button = entity.Should().BeOfType<ButtonWidget>().Subject;
        button.Label.Should().Be("go");
    }

    [Fact]
    public void Read_throws_JsonException_on_unknown_value_without_fallback()
    {
        //Arrange
        Action act = () => JsonSerializer.Deserialize<IStrict>("{\"type\":\"b\"}");

        //Act + Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Read_throws_JsonException_on_missing_property_without_fallback()
    {
        //Arrange
        Action act = () => JsonSerializer.Deserialize<IStrict>("{}");

        //Act + Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Read_throws_JsonException_when_value_is_not_an_object()
    {
        //Arrange
        Action act = () => JsonSerializer.Deserialize<IShape>("\"circle\"");

        //Act + Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Read_returns_null_for_null_token()
        => JsonSerializer.Deserialize<IShape>("null").Should().BeNull();

    [Fact]
    public void Read_finds_discriminator_case_insensitively_when_options_allow()
    {
        //Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        //Act
        var shape = JsonSerializer.Deserialize<IShape>("{\"TYPE\":\"circle\",\"radius\":1}", options);

        //Assert
        shape.Should().BeOfType<Circle>();
    }

    [Fact]
    public void Read_does_not_find_differently_cased_discriminator_by_default()
        => JsonSerializer.Deserialize<IShape>("{\"TYPE\":\"circle\"}").Should().BeOfType<UnknownShape>();

    [Fact]
    public void Read_honors_camel_case_serializer_options_for_payload_properties()
    {
        //Arrange
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        //Act
        var animal = JsonSerializer.Deserialize<Animal>("{\"kind\":\"dog\",\"bark\":\"yip\"}", options);

        //Assert
        ((Dog)animal).Bark.Should().Be("yip");
    }

    [Fact]
    public void Write_serializes_runtime_type_contract()
    {
        //Arrange
        IShape shape = new Circle { Type = "circle", Radius = 4 };

        //Act
        var json = JsonSerializer.Serialize(shape);

        //Assert
        json.Should().Contain("\"radius\":4");
        json.Should().Contain("\"type\":\"circle\"");
    }

    [Fact]
    public void Write_round_trips_derived_instances()
    {
        //Arrange
        IShape original = new Square { Type = "square", Side = 9 };

        //Act
        var json = JsonSerializer.Serialize(original);
        var reread = JsonSerializer.Deserialize<IShape>(json);

        //Assert
        var square = reread.Should().BeOfType<Square>().Subject;
        square.Side.Should().Be(9);
    }

    [Fact]
    public void Write_serializes_null_base_reference_as_null_literal()
        => JsonSerializer.Serialize<IShape>(null).Should().Be("null");

    [Fact]
    public void Write_throws_JsonException_for_instance_of_the_base_type_itself()
    {
        //Arrange
        Action act = () => JsonSerializer.Serialize(new Note { Type = "plain" });

        //Act + Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Write_serializes_fallback_subclass_normally()
    {
        //Arrange
        Note note = new UnknownNote { Type = "mystery" };

        //Act
        var json = JsonSerializer.Serialize(note);

        //Assert
        json.Should().Contain("\"type\":\"mystery\"");
    }

    [Fact]
    public void Read_throws_InvalidOperationException_for_self_mapped_known_type()
    {
        //Arrange
        var options = OptionsWithConverter(new FallbackTypeConverter<SelfMappedBase>());
        Action act = () => JsonSerializer.Deserialize<SelfMappedBase>("{\"type\":\"self\"}", options);

        //Act + Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Read_throws_InvalidOperationException_for_self_referencing_fallback()
    {
        //Arrange
        var options = OptionsWithConverter(new FallbackTypeConverter<SelfFallbackBase>());
        Action act = () => JsonSerializer.Deserialize<SelfFallbackBase>("{}", options);

        //Act + Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Read_throws_InvalidOperationException_for_non_assignable_known_type()
    {
        //Arrange
        var options = OptionsWithConverter(new FallbackTypeConverter<NotAssignableBase>());
        Action act = () => JsonSerializer.Deserialize<NotAssignableBase>("{\"type\":\"circle\"}", options);

        //Act + Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Read_throws_InvalidOperationException_for_duplicate_discriminator_values()
    {
        //Arrange
        var options = OptionsWithConverter(new FallbackTypeConverter<DuplicateValueBase>());
        Action act = () => JsonSerializer.Deserialize<DuplicateValueBase>("{\"type\":\"dup\"}", options);

        //Act + Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Read_throws_InvalidOperationException_for_abstract_fallback_without_dispatch()
    {
        //Arrange
        var options = OptionsWithConverter(new FallbackTypeConverter<AbstractFallbackBase>());
        Action act = () => JsonSerializer.Deserialize<AbstractFallbackBase>("{}", options);

        //Act + Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Read_throws_InvalidOperationException_when_base_type_lacks_discriminator_attribute()
    {
        //Arrange
        var options = OptionsWithConverter(new FallbackTypeConverter<PlainUnattributed>());
        Action act = () => JsonSerializer.Deserialize<PlainUnattributed>("{}", options);

        //Act + Assert
        act.Should().Throw<InvalidOperationException>();
    }

    private static JsonSerializerOptions OptionsWithConverter(System.Text.Json.Serialization.JsonConverter converter)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(converter);

        return options;
    }
}

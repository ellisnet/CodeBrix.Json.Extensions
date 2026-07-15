using System.Text.Json.Serialization;
using CodeBrix.Json.Extensions.Polymorphism;

namespace CodeBrix.Json.Extensions.Tests;

[JsonConverter(typeof(FallbackTypeConverterFactory))]
[JsonDiscriminator("type")]
[JsonKnownType(typeof(Circle), "circle")]
[JsonKnownType(typeof(Square), "square")]
[JsonKnownType(typeof(NumberedShape), "7")]
[JsonFallbackType(typeof(UnknownShape))]
public interface IShape
{
    [JsonPropertyName("type")]
    string Type { get; set; }
}

public class Circle : IShape
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("radius")]
    public double Radius { get; set; }
}

public class Square : IShape
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("side")]
    public double Side { get; set; }
}

public class NumberedShape : IShape
{
    // The "type" discriminator for this shape is the JSON NUMBER 7, which cannot bind to a
    // string property — so the property is excluded from (de)serialization.
    [JsonIgnore]
    public string Type { get; set; }
}

public class UnknownShape : IShape
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
}

[JsonConverter(typeof(FallbackTypeConverterFactory))]
[JsonDiscriminator("kind")]
[JsonKnownType(typeof(Dog), "dog")]
[JsonFallbackType(typeof(UnknownAnimal))]
public abstract class Animal
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; }
}

public class Dog : Animal
{
    [JsonPropertyName("bark")]
    public string Bark { get; set; }
}

public class UnknownAnimal : Animal
{
}

// A CONCRETE polymorphic base whose fallback is a do-nothing derived class.
[JsonConverter(typeof(FallbackTypeConverterFactory))]
[JsonDiscriminator("type")]
[JsonKnownType(typeof(TextNote), "text")]
[JsonFallbackType(typeof(UnknownNote))]
public class Note
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
}

public class TextNote : Note
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

public class UnknownNote : Note
{
}

[JsonConverter(typeof(FallbackTypeConverterFactory))]
[JsonDiscriminator("type")]
[JsonKnownType(typeof(StrictThing), "a")]
public interface IStrict
{
    [JsonPropertyName("type")]
    string Type { get; set; }
}

public class StrictThing : IStrict
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
}

// Two-level dispatch: IEntity dispatches on "object" to IWidget, which dispatches on "type".
[JsonConverter(typeof(FallbackTypeConverterFactory))]
[JsonDiscriminator("object")]
[JsonKnownType(typeof(IWidget), "widget")]
public interface IEntity
{
}

[JsonConverter(typeof(FallbackTypeConverterFactory))]
[JsonDiscriminator("type")]
[JsonKnownType(typeof(ButtonWidget), "button")]
public interface IWidget : IEntity
{
}

public class ButtonWidget : IWidget
{
    [JsonPropertyName("label")]
    public string Label { get; set; }
}

// Invalid configurations — exercised via explicit FallbackTypeConverter<T> instances, so no
// [JsonConverter] attribute is applied here.
[JsonDiscriminator("type")]
[JsonKnownType(typeof(SelfMappedBase), "self")]
public class SelfMappedBase
{
}

[JsonDiscriminator("type")]
[JsonFallbackType(typeof(SelfFallbackBase))]
public class SelfFallbackBase
{
}

[JsonDiscriminator("type")]
[JsonKnownType(typeof(Circle), "circle")]
public class NotAssignableBase
{
}

[JsonDiscriminator("type")]
[JsonKnownType(typeof(DuplicateChild), "dup")]
[JsonKnownType(typeof(OtherDuplicateChild), "dup")]
public class DuplicateValueBase
{
}

public class DuplicateChild : DuplicateValueBase
{
}

public class OtherDuplicateChild : DuplicateValueBase
{
}

[JsonDiscriminator("type")]
[JsonFallbackType(typeof(AbstractFallback))]
public class AbstractFallbackBase
{
}

public abstract class AbstractFallback : AbstractFallbackBase
{
}

public class PlainUnattributed
{
}

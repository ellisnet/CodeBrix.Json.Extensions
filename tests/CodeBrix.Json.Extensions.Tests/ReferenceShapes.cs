using System.Collections.Generic;
using System.Text.Json.Serialization;
using CodeBrix.Json.Extensions.Polymorphism;
using CodeBrix.Json.Extensions.References;

namespace CodeBrix.Json.Extensions.Tests;

// Non-polymorphic referenceable node: exercises shared references, cycles and self-loops.
[JsonReferenceable]
public class RefNode
{
    public string Name { get; set; }

    public RefNode Link { get; set; }

    public List<RefNode> Friends { get; set; }
}

// A plain, NON-referenceable payload nested inside a referenceable graph — must serialize inline (no $id).
public class PlainTag
{
    public string Text { get; set; }
}

[JsonReferenceable]
public class TaggedNode
{
    public string Name { get; set; }

    public PlainTag Tag { get; set; }
}

// Polymorphic AND referenceable. Note it carries the discriminator ATTRIBUTES but NOT
// [JsonConverter(typeof(FallbackTypeConverterFactory))]: ReferenceJson's own converter performs the
// discriminator dispatch, so attaching the polymorphism converter would bypass reference handling.
[JsonReferenceable]
[JsonDiscriminator("kind")]
[JsonKnownType(typeof(Car), "car")]
[JsonKnownType(typeof(Truck), "truck")]
[JsonFallbackType(typeof(UnknownVehicle))]
public class Vehicle
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; }

    public string Name { get; set; }

    public Vehicle Tows { get; set; }
}

[JsonReferenceable]
public class Car : Vehicle
{
    public int Doors { get; set; }
}

[JsonReferenceable]
public class Truck : Vehicle
{
    public double Payload { get; set; }
}

[JsonReferenceable]
public class UnknownVehicle : Vehicle
{
}

// Referenceable type with no usable parameterless constructor: deserialization must fail with a clear
// JsonException because the reference machinery creates the instance before populating it.
[JsonReferenceable]
public class RefNoDefaultCtor
{
    public RefNoDefaultCtor(string name) => Name = name;

    public string Name { get; set; }
}

// Referenceable type with an ignored member — it must be absent from the $id envelope on write and round-trip.
[JsonReferenceable]
public class SecretiveNode
{
    public string Name { get; set; }

    [JsonIgnore]
    public string Secret { get; set; }
}

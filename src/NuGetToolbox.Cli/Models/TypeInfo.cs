using System.Text.Json.Serialization;

namespace NuGetToolbox.Cli.Models;

/// <summary>
/// Represents a public type in a NuGet package.
/// </summary>
public class TypeInfo
{
    [JsonPropertyName("namespace")]
    public required string Namespace { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("kind")]
    public required string Kind { get; set; } // class, interface, struct, enum
}

using System.Text.Json.Serialization;

namespace NuGetToolbox.Cli.Models;

/// <summary>
/// Represents a public method with signature and documentation.
/// </summary>
public class MethodInfo
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("method")]
    public required string Method { get; set; }

    [JsonPropertyName("signature")]
    public required string Signature { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("params")]
    public Dictionary<string, string>? Params { get; set; }

    [JsonPropertyName("returns")]
    public string? Returns { get; set; }
}

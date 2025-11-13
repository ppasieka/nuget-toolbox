using System.Text.Json.Serialization;

namespace NuGetToolbox.Cli.Models;

/// <summary>
/// Represents API differences between two package versions.
/// </summary>
public class DiffResult
{
    [JsonPropertyName("packageId")]
    public required string PackageId { get; set; }

    [JsonPropertyName("versionFrom")]
    public required string VersionFrom { get; set; }

    [JsonPropertyName("versionTo")]
    public required string VersionTo { get; set; }

    [JsonPropertyName("tfm")]
    public required string Tfm { get; set; }

    [JsonPropertyName("breaking")]
    public List<DiffItem>? Breaking { get; set; }

    [JsonPropertyName("added")]
    public List<TypeInfo>? Added { get; set; }

    [JsonPropertyName("removed")]
    public List<TypeInfo>? Removed { get; set; }

    [JsonPropertyName("compatible")]
    public bool Compatible { get; set; }
}

/// <summary>
/// Represents a breaking change or modification.
/// </summary>
public class DiffItem
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("method")]
    public required string Method { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

using System.Text.Json.Serialization;

namespace NuGetToolbox.Cli.Models;

/// <summary>
/// Represents NuGet package information.
/// </summary>
public class PackageInfo
{
    [JsonPropertyName("packageId")]
    public required string PackageId { get; set; }

    [JsonPropertyName("resolvedVersion")]
    public required string Version { get; set; }

    [JsonPropertyName("resolved")]
    public required bool Resolved { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("nupkgPath")]
    public string? NupkgPath { get; set; }

    [JsonPropertyName("targetFrameworks")]
    public List<string>? Tfms { get; set; }
}

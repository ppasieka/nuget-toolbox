using System.Text.Json.Serialization;

namespace NuGetToolbox.Cli.Models;

/// <summary>
/// Represents a direct package dependency with target framework information.
/// </summary>
public class DirectDependency
{
    [JsonPropertyName("targetFramework")]
    public string TargetFramework { get; set; } = string.Empty;

    [JsonPropertyName("packageId")]
    public string PackageId { get; set; } = string.Empty;

    [JsonPropertyName("versionRange")]
    public string VersionRange { get; set; } = string.Empty;
}

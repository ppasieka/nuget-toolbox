using Microsoft.Extensions.Logging;
using NuGetToolbox.Cli.Models;

namespace NuGetToolbox.Cli.Services;

/// <summary>
/// Resolves and downloads NuGet packages using NuGet.Protocol V3.
/// </summary>
public class NuGetPackageResolver
{
    private readonly ILogger<NuGetPackageResolver> _logger;

    public NuGetPackageResolver(ILogger<NuGetPackageResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Resolves a package by ID and optional version.
    /// </summary>
    public async Task<PackageInfo> ResolvePackageAsync(
        string packageId,
        string? version = null,
        string? feedUrl = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Downloads a package and returns the path to the cached .nupkg file.
    /// </summary>
    public async Task<string> DownloadPackageAsync(
        string packageId,
        string version,
        string? feedUrl = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

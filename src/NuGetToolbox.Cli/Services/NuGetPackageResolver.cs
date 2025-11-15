using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGetToolbox.Cli.Models;

namespace NuGetToolbox.Cli.Services;

/// <summary>
/// Resolves and downloads NuGet packages using NuGet.Protocol V3.
/// </summary>
public class NuGetPackageResolver
{
    private readonly ILogger<NuGetPackageResolver> _logger;
    private readonly ISettings _settings;

    public NuGetPackageResolver(ILogger<NuGetPackageResolver> logger)
    {
        _logger = logger;
        _settings = Settings.LoadDefaultSettings(Directory.GetCurrentDirectory());
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
        try
        {
            var repositories = GetSourceRepositories(feedUrl);
            var nugetLogger = new NuGetLogger(_logger);

            using var cacheContext = new SourceCacheContext();

            foreach (var repository in repositories)
            {
                try
                {
                    var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

                    IEnumerable<IPackageSearchMetadata> packages;
                    if (string.IsNullOrEmpty(version))
                    {
                        packages = await metadataResource.GetMetadataAsync(
                            packageId,
                            includePrerelease: true,
                            includeUnlisted: false,
                            cacheContext,
                            nugetLogger,
                            cancellationToken);

                        var latestPackage = packages
                            .OrderByDescending(p => p.Identity.Version)
                            .FirstOrDefault();

                        if (latestPackage == null)
                            continue;

                        var nupkgPath = await DownloadPackageAsync(
                            latestPackage.Identity.Id,
                            latestPackage.Identity.Version.ToString(),
                            feedUrl,
                            cancellationToken);

                        return new PackageInfo
                        {
                            PackageId = latestPackage.Identity.Id,
                            Version = latestPackage.Identity.Version.ToString(),
                            Resolved = true,
                            Source = repository.PackageSource.Source,
                            NupkgPath = nupkgPath,
                            Tfms = await GetTargetFrameworksAsync(nupkgPath, cancellationToken)
                        };
                    }
                    else
                    {
                        if (!NuGetVersion.TryParse(version, out var nugetVersion))
                        {
                            _logger.LogError("Invalid version format: {Version}", version);
                            continue;
                        }

                        packages = await metadataResource.GetMetadataAsync(
                            packageId,
                            includePrerelease: true,
                            includeUnlisted: false,
                            cacheContext,
                            nugetLogger,
                            cancellationToken);

                        var specificPackage = packages.FirstOrDefault(p => p.Identity.Version == nugetVersion);
                        if (specificPackage == null)
                            continue;

                        var nupkgPath = await DownloadPackageAsync(
                            specificPackage.Identity.Id,
                            specificPackage.Identity.Version.ToString(),
                            feedUrl,
                            cancellationToken);

                        return new PackageInfo
                        {
                            PackageId = specificPackage.Identity.Id,
                            Version = specificPackage.Identity.Version.ToString(),
                            Resolved = true,
                            Source = repository.PackageSource.Source,
                            NupkgPath = nupkgPath,
                            Tfms = await GetTargetFrameworksAsync(nupkgPath, cancellationToken)
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve package from {Source}", repository.PackageSource.Source);
                }
            }

            _logger.LogError("Package {PackageId} {Version} not found in any source", packageId, version ?? "latest");
            return new PackageInfo
            {
                PackageId = packageId,
                Version = version ?? "unknown",
                Resolved = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving package {PackageId} {Version}", packageId, version);
            throw;
        }
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
        try
        {
            if (!NuGetVersion.TryParse(version, out var nugetVersion))
            {
                throw new ArgumentException($"Invalid version format: {version}", nameof(version));
            }

            var packageIdentity = new PackageIdentity(packageId, nugetVersion);
            var repositories = GetSourceRepositories(feedUrl);
            var nugetLogger = new NuGetLogger(_logger);
            var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(_settings);

            var packagePath = Path.Combine(
                globalPackagesFolder,
                packageId.ToLowerInvariant(),
                nugetVersion.ToNormalizedString(),
                $"{packageId.ToLowerInvariant()}.{nugetVersion.ToNormalizedString()}.nupkg");

            if (File.Exists(packagePath))
            {
                _logger.LogInformation("Package already cached at {Path}", packagePath);
                return packagePath;
            }

            using var cacheContext = new SourceCacheContext();

            foreach (var repository in repositories)
            {
                try
                {
                    var downloadResource = await repository.GetResourceAsync<DownloadResource>(cancellationToken);

                    var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                        packageIdentity,
                        new PackageDownloadContext(cacheContext),
                        globalPackagesFolder,
                        nugetLogger,
                        cancellationToken);

                    if (downloadResult.Status == DownloadResourceResultStatus.Available)
                    {
                        using (downloadResult)
                        {
                            _logger.LogInformation("Downloaded package to {Path}", packagePath);
                            return packagePath;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download package from {Source}", repository.PackageSource.Source);
                }
            }

            throw new InvalidOperationException($"Package {packageId} {version} could not be downloaded from any source");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading package {PackageId} {Version}", packageId, version);
            throw;
        }
    }

    private List<SourceRepository> GetSourceRepositories(string? feedUrl)
    {
        var repositories = new List<SourceRepository>();

        if (!string.IsNullOrEmpty(feedUrl))
        {
            var packageSource = new PackageSource(feedUrl);
            repositories.Add(Repository.Factory.GetCoreV3(packageSource));
        }
        else
        {
            var packageSourceProvider = new PackageSourceProvider(_settings);
            var sources = packageSourceProvider.LoadPackageSources().Where(s => s.IsEnabled);

            foreach (var source in sources)
            {
                repositories.Add(Repository.Factory.GetCoreV3(source));
            }
        }

        return repositories;
    }

    private async Task<List<string>> GetTargetFrameworksAsync(string nupkgPath, CancellationToken cancellationToken)
    {
        var tfms = new List<string>();

        try
        {
            // If .nupkg file exists, read it directly
            if (File.Exists(nupkgPath))
            {
                using var packageReader = new PackageArchiveReader(nupkgPath);
                var libItems = await packageReader.GetLibItemsAsync(cancellationToken);
                tfms.AddRange(libItems.Select(item => item.TargetFramework.GetShortFolderName()).Distinct());
            }
            else
            {
                // Otherwise, read from extracted folder (global packages cache stores extracted contents)
                var packageFolder = Path.GetDirectoryName(nupkgPath);
                if (packageFolder != null && Directory.Exists(packageFolder))
                {
                    using var packageReader = new PackageFolderReader(packageFolder);
                    var libItems = await packageReader.GetLibItemsAsync(cancellationToken);
                    tfms.AddRange(libItems.Select(item => item.TargetFramework.GetShortFolderName()).Distinct());
                }
                else
                {
                    _logger.LogWarning("Package not found at {Path} or {Folder}", nupkgPath, packageFolder);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract target frameworks from {Path}", nupkgPath);
        }

        return tfms;
    }

    /// <summary>
    /// Extracts direct package dependencies from a NuGet package's .nuspec file.
    /// </summary>
    public async Task<List<DirectDependency>> GetDirectDependenciesAsync(
        string nupkgPath,
        CancellationToken cancellationToken = default)
    {
        var dependencies = new List<DirectDependency>();

        try
        {
            if (File.Exists(nupkgPath))
            {
                using var packageReader = new PackageArchiveReader(nupkgPath);
                var nuspecReader = await packageReader.GetNuspecReaderAsync(cancellationToken);
                var dependencyGroups = nuspecReader.GetDependencyGroups();

                foreach (var group in dependencyGroups)
                {
                    var tfm = group.TargetFramework.GetShortFolderName();

                    foreach (var package in group.Packages)
                    {
                        dependencies.Add(new DirectDependency
                        {
                            TargetFramework = tfm,
                            PackageId = package.Id,
                            VersionRange = package.VersionRange?.ToString() ?? "*"
                        });
                    }
                }
            }
            else
            {
                var packageFolder = Path.GetDirectoryName(nupkgPath);
                if (packageFolder != null && Directory.Exists(packageFolder))
                {
                    using var packageReader = new PackageFolderReader(packageFolder);
                    var nuspecReader = await packageReader.GetNuspecReaderAsync(cancellationToken);
                    var dependencyGroups = nuspecReader.GetDependencyGroups();

                    foreach (var group in dependencyGroups)
                    {
                        var tfm = group.TargetFramework.GetShortFolderName();

                        foreach (var package in group.Packages)
                        {
                            dependencies.Add(new DirectDependency
                            {
                                TargetFramework = tfm,
                                PackageId = package.Id,
                                VersionRange = package.VersionRange?.ToString() ?? "*"
                            });
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Package not found at {Path} for dependency reading", nupkgPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read dependencies from {Path}", nupkgPath);
        }

        return dependencies;
    }

    private class NuGetLogger : NuGet.Common.ILogger
    {
        private readonly ILogger<NuGetPackageResolver> _logger;

        public NuGetLogger(ILogger<NuGetPackageResolver> logger)
        {
            _logger = logger;
        }

        public void Log(NuGet.Common.LogLevel logLevel, string data)
        {
            _logger.Log(ConvertLogLevel(logLevel), data);
        }

        public void Log(ILogMessage message)
        {
            _logger.Log(ConvertLogLevel(message.Level), message.Message);
        }

        public Task LogAsync(NuGet.Common.LogLevel logLevel, string data)
        {
            Log(logLevel, data);
            return Task.CompletedTask;
        }

        public Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }

        public void LogDebug(string data) => _logger.LogDebug(data);
        public void LogError(string data) => _logger.LogError(data);
        public void LogInformation(string data) => _logger.LogInformation(data);
        public void LogInformationSummary(string data) => _logger.LogInformation(data);
        public void LogMinimal(string data) => _logger.LogInformation(data);
        public void LogVerbose(string data) => _logger.LogTrace(data);
        public void LogWarning(string data) => _logger.LogWarning(data);

        private Microsoft.Extensions.Logging.LogLevel ConvertLogLevel(NuGet.Common.LogLevel logLevel)
        {
            return logLevel switch
            {
                NuGet.Common.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
                NuGet.Common.LogLevel.Verbose => Microsoft.Extensions.Logging.LogLevel.Trace,
                NuGet.Common.LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
                NuGet.Common.LogLevel.Minimal => Microsoft.Extensions.Logging.LogLevel.Information,
                NuGet.Common.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
                NuGet.Common.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
                _ => Microsoft.Extensions.Logging.LogLevel.Information
            };
        }
    }
}

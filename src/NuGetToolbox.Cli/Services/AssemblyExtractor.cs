using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace NuGetToolbox.Cli.Services;

public class AssemblyExtractor
{
    private readonly ILogger<AssemblyExtractor> _logger;
    private readonly FrameworkSelector _frameworkSelector;

    public AssemblyExtractor(ILogger<AssemblyExtractor> logger, FrameworkSelector frameworkSelector)
    {
        _logger = logger;
        _frameworkSelector = frameworkSelector;
    }

    public record ExtractionResult(
        List<string> Assemblies,
        string? TempDir,
        NuGetFramework? SelectedFramework,
        IReadOnlyList<string>? AvailableTfms,
        string? ErrorMessage);

    public async Task<ExtractionResult> ExtractAssembliesAsync(
        string nupkgPath,
        string? requestedTfm,
        bool includeXmlDocs,
        CancellationToken cancellationToken)
    {
        using var packageReader = new PackageArchiveReader(nupkgPath);

        var refItems = (await packageReader.GetReferenceItemsAsync(cancellationToken)).ToList();
        var libItems = (await packageReader.GetLibItemsAsync(cancellationToken)).ToList();

        var primaryItems = refItems.Count > 0 ? refItems : libItems;
        var sourceType = refItems.Count > 0 ? "ref" : "lib";

        if (primaryItems.Count == 0)
        {
            return new ExtractionResult([], null, null, [], "No assemblies found in package");
        }

        _logger.LogDebug("Using {Source}/ assemblies (ref: {RefCount}, lib: {LibCount})",
            sourceType, refItems.Count, libItems.Count);

        var availableFrameworks = primaryItems
            .Select(g => g.TargetFramework)
            .Where(f => f != null && f != NuGetFramework.UnsupportedFramework)
            .ToList();

        if (availableFrameworks.Count == 0)
        {
            return new ExtractionResult([], null, null, [], "No valid target frameworks found");
        }

        NuGetFramework? selectedFramework;
        if (!string.IsNullOrEmpty(requestedTfm))
        {
            selectedFramework = availableFrameworks.FirstOrDefault(
                f => f.GetShortFolderName().Equals(requestedTfm, StringComparison.OrdinalIgnoreCase));

            if (selectedFramework == null)
            {
                var availableTfms = _frameworkSelector.GetAvailableTfms(availableFrameworks);
                return new ExtractionResult([], null, null, availableTfms,
                    $"TFM '{requestedTfm}' not found. Available: {string.Join(", ", availableTfms)}. Use --tfm to specify.");
            }
        }
        else
        {
            var runtimeFramework = FrameworkSelector.GetRuntimeFramework();
            selectedFramework = _frameworkSelector.SelectNearest(runtimeFramework, availableFrameworks);

            if (selectedFramework == null)
            {
                var availableTfms = _frameworkSelector.GetAvailableTfms(availableFrameworks);
                return new ExtractionResult([], null, null, availableTfms,
                    $"No compatible framework for {runtimeFramework.GetShortFolderName()}. Available: {string.Join(", ", availableTfms)}. Use --tfm to specify.");
            }
        }

        var targetGroup = primaryItems.FirstOrDefault(g => g.TargetFramework == selectedFramework);
        if (targetGroup == null)
        {
            return new ExtractionResult([], null, selectedFramework, null, "Framework group not found");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-toolbox-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        var assemblies = new List<string>();

        try
        {
            foreach (var item in targetGroup.Items.Where(i => i.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
            {
                var fileName = Path.GetFileName(item);
                var destPath = Path.Combine(tempDir, fileName);

                using (var stream = packageReader.GetStream(item))
                using (var fileStream = File.Create(destPath))
                {
                    await stream.CopyToAsync(fileStream, cancellationToken);
                }

                assemblies.Add(destPath);
            }

            if (includeXmlDocs)
            {
                // When using ref/ assemblies, XML docs are typically in lib/ folder
                // Try to get XML docs from both ref/ (primary) and lib/ (fallback)
                var xmlSourceGroup = targetGroup;
                
                // If we're using ref/ and lib/ has the same TFM, prefer lib/ for XML docs
                if (sourceType == "ref")
                {
                    var libGroup = libItems.FirstOrDefault(g => g.TargetFramework == selectedFramework);
                    if (libGroup != null && libGroup.Items.Any(i => i.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
                    {
                        xmlSourceGroup = libGroup;
                        _logger.LogDebug("Using lib/ for XML docs (ref/ assemblies typically don't include XML docs)");
                    }
                }
                
                foreach (var item in xmlSourceGroup.Items.Where(i => i.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
                {
                    var fileName = Path.GetFileName(item);
                    var destPath = Path.Combine(tempDir, fileName);

                    using (var stream = packageReader.GetStream(item))
                    using (var fileStream = File.Create(destPath))
                    {
                        await stream.CopyToAsync(fileStream, cancellationToken);
                    }
                }
            }

            _logger.LogInformation("Extracted {Count} assemblies from {Source}/{Tfm}",
                assemblies.Count, sourceType, selectedFramework.GetShortFolderName());

            return new ExtractionResult(assemblies, tempDir, selectedFramework, null, null);
        }
        catch
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
            throw;
        }
    }
}

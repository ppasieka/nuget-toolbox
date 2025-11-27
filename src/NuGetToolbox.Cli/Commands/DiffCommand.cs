using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGetToolbox.Cli.Services;

namespace NuGetToolbox.Cli.Commands;

/// <summary>
/// Diff command: Compare public API between two package versions.
/// </summary>
public static class DiffCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var packageOption = new Option<string>(["--package", "-p"])
        {
            Description = "Package ID",
            IsRequired = true
        };

        var fromOption = new Option<string>("--from")
        {
            Description = "From version",
            IsRequired = true
        };

        var toOption = new Option<string>("--to")
        {
            Description = "To version",
            IsRequired = true
        };

        var tfmOption = new Option<string?>("--tfm")
        {
            Description = "Target framework moniker (e.g., net8.0, netstandard2.0)"
        };

        var outputOption = new Option<string?>(["--output", "-o"])
        {
            Description = "Output file path (default: stdout)"
        };

        var command = new Command("diff", "Compare public API between two package versions")
        {
            packageOption,
            fromOption,
            toOption,
            tfmOption,
            outputOption
        };

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var package = ctx.ParseResult.GetValueForOption(packageOption);
            var from = ctx.ParseResult.GetValueForOption(fromOption);
            var to = ctx.ParseResult.GetValueForOption(toOption);
            var tfm = ctx.ParseResult.GetValueForOption(tfmOption);
            var output = ctx.ParseResult.GetValueForOption(outputOption);

            var token = ctx.GetCancellationToken();

            ctx.ExitCode = await HandlerAsync(package!, from!, to!, tfm, output, serviceProvider, token);
        });
        return command;
    }

    private static async Task<int> HandlerAsync(
        string packageId,
        string fromVersion,
        string toVersion,
        string? tfm,
        string? output,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DiffCommand");
        string? fromTempDir = null;
        string? toTempDir = null;

        try
        {
            var resolver = serviceProvider.GetRequiredService<NuGetPackageResolver>();
            var exporter = serviceProvider.GetRequiredService<SignatureExporter>();
            var analyzer = serviceProvider.GetRequiredService<ApiDiffAnalyzer>();

            logger.LogInformation("Comparing {PackageId} versions {From} -> {To}", packageId, fromVersion, toVersion);

            var fromPackage = await resolver.ResolvePackageAsync(packageId, fromVersion, cancellationToken: cancellationToken);
            if (fromPackage == null || !fromPackage.Resolved || string.IsNullOrEmpty(fromPackage.NupkgPath))
            {
                logger.LogError("Package {PackageId} version {Version} not found", packageId, fromVersion);
                return ExitCodes.NotFound;
            }

            var toPackage = await resolver.ResolvePackageAsync(packageId, toVersion, cancellationToken: cancellationToken);
            if (toPackage == null || !toPackage.Resolved || string.IsNullOrEmpty(toPackage.NupkgPath))
            {
                logger.LogError("Package {PackageId} version {Version} not found", packageId, toVersion);
                return ExitCodes.NotFound;
            }

            var (fromAssemblies, fromDir) = await ExtractAssembliesAsync(fromPackage.NupkgPath, tfm, logger, cancellationToken);
            fromTempDir = fromDir;

            var (toAssemblies, toDir) = await ExtractAssembliesAsync(toPackage.NupkgPath, tfm, logger, cancellationToken);
            toTempDir = toDir;

            if (fromAssemblies.Count == 0 || toAssemblies.Count == 0)
            {
                logger.LogWarning("No assemblies found in one or both package versions for TFM {Tfm}", tfm ?? "any");
                return ExitCodes.TfmMismatch;
            }

            var methodsFrom = exporter.ExportMethods(fromAssemblies, cancellationToken: cancellationToken);
            var methodsTo = exporter.ExportMethods(toAssemblies, cancellationToken: cancellationToken);

            var targetFramework = tfm ?? fromPackage.Tfms?.FirstOrDefault() ?? "unknown";
            var diffResult = analyzer.CompareVersions(packageId, methodsFrom, methodsTo, fromVersion, toVersion, targetFramework, cancellationToken);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(diffResult, options);

            if (!string.IsNullOrEmpty(output))
            {
                await File.WriteAllTextAsync(output, json, cancellationToken);
                logger.LogInformation("Diff result written to {OutputPath}", output);
            }
            else
            {
                Console.WriteLine(json);
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to compare package versions for {PackageId}", packageId);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.Error;
        }
        finally
        {
            CleanupTempDirectory(fromTempDir, logger);
            CleanupTempDirectory(toTempDir, logger);
        }
    }

    private static void CleanupTempDirectory(string? tempDir, ILogger? logger)
    {
        if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to cleanup temp directory {TempDir}", tempDir);
            }
        }
    }

    private static async Task<(List<string> assemblies, string? tempDir)> ExtractAssembliesAsync(string nupkgPath, string? tfm, ILogger logger, CancellationToken cancellationToken)
    {
        var assemblies = new List<string>();

        using var packageReader = new PackageArchiveReader(nupkgPath);
        var libItems = await packageReader.GetLibItemsAsync(cancellationToken);

        var targetGroup = string.IsNullOrEmpty(tfm)
            ? libItems.OrderByDescending(g => g.TargetFramework.Version).FirstOrDefault()
            : libItems.FirstOrDefault(g => g.TargetFramework.GetShortFolderName() == tfm);

        if (targetGroup == null)
        {
            logger.LogWarning("No lib items found for TFM {Tfm}", tfm ?? "any");
            return (assemblies, null);
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-toolbox-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

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

        var xmlItems = targetGroup.Items.Where(i => i.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        foreach (var item in xmlItems)
        {
            var fileName = Path.GetFileName(item);
            var destPath = Path.Combine(tempDir, fileName);

            using (var stream = packageReader.GetStream(item))
            using (var fileStream = File.Create(destPath))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }
        }

        logger.LogInformation("Extracted {Count} assemblies from {Tfm}", assemblies.Count, targetGroup.TargetFramework.GetShortFolderName());

        return (assemblies, tempDir);
    }
}

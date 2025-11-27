using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGetToolbox.Cli.Services;

namespace NuGetToolbox.Cli.Commands;

/// <summary>
/// ExportSignatures command: Export public method signatures with XML documentation.
/// </summary>
public static class ExportSignaturesCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var packageOption = new Option<string>(["--package", "-p"])
        {
            Description = "Package ID",
            IsRequired = true
        };

        var versionOption = new Option<string?>(["--version", "-v"])
        {
            Description = "Package version (if omitted, uses latest)"
        };

        var tfmOption = new Option<string?>("--tfm")
        {
            Description = "Target framework moniker (e.g., net8.0, netstandard2.0)"
        };

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: json or jsonl"
        };
        formatOption.SetDefaultValue("json");
        formatOption.FromAmong("json", "jsonl");

        var filterOption = new Option<string?>(["--filter", "--namespace"])
        {
            Name = "filter",
            Description = "Namespace filter (e.g., Newtonsoft.Json.Linq)"
        };

        var outputOption = new Option<string?>(["--output", "-o"])
        {
            Description = "Output file path (default: stdout)"
        };

        var command = new Command("export-signatures", "Export public method signatures with XML documentation")
        {
            packageOption,
            versionOption,
            tfmOption,
            formatOption,
            filterOption,
            outputOption
        };

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var package = ctx.ParseResult.GetValueForOption(packageOption);
            var version = ctx.ParseResult.GetValueForOption(versionOption);
            var tfm = ctx.ParseResult.GetValueForOption(tfmOption);
            var format = ctx.ParseResult.GetValueForOption(formatOption) ?? "json";
            var filter = ctx.ParseResult.GetValueForOption(filterOption);
            var output = ctx.ParseResult.GetValueForOption(outputOption);

            var token = ctx.GetCancellationToken();

            ctx.ExitCode = await HandlerAsync(package!, version, tfm, format, filter, output, serviceProvider, token);
        });
        return command;
    }

    private static async Task<int> HandlerAsync(
        string packageId,
        string? version,
        string? tfm,
        string format,
        string? namespaceFilter,
        string? output,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ExportSignaturesCommand));
        string? tempDir = null;

        try
        {
            var resolver = serviceProvider.GetRequiredService<NuGetPackageResolver>();
            var exporter = serviceProvider.GetRequiredService<SignatureExporter>();

            logger.LogInformation("Resolving package {PackageId} (version: {Version})", packageId, version ?? "latest");

            var packageInfo = await resolver.ResolvePackageAsync(packageId, version, cancellationToken: cancellationToken);

            if (packageInfo == null || !packageInfo.Resolved || string.IsNullOrEmpty(packageInfo.NupkgPath))
            {
                logger.LogError("Package {PackageId} not found", packageId);
                return ExitCodes.NotFound;
            }

            var (assemblyPaths, extractDir) = await ExtractAssembliesAsync(packageInfo.NupkgPath, tfm, logger, cancellationToken);
            tempDir = extractDir;

            if (assemblyPaths.Count == 0)
            {
                logger.LogWarning("No assemblies found in package for TFM {Tfm}", tfm ?? "any");
                return ExitCodes.TfmMismatch;
            }

            var methods = exporter.ExportMethods(assemblyPaths, namespaceFilter, cancellationToken);

            var result = format.ToLowerInvariant() == "jsonl"
                ? exporter.ExportToJsonL(methods)
                : exporter.ExportToJson(methods);

            if (!string.IsNullOrEmpty(output))
            {
                await File.WriteAllTextAsync(output, result, cancellationToken);
                logger.LogInformation("Method signatures written to {OutputPath}", output);
            }
            else
            {
                Console.WriteLine(result);
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export signatures for package {PackageId}", packageId);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.Error;
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to cleanup temp directory {TempDir}", tempDir);
                }
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

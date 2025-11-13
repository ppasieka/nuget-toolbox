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
    public static Command Create(IServiceProvider? serviceProvider = null)
    {
        var packageOption = new Option<string>("--package", "-p")
        {
            Description = "Package ID",
            Required = true
        };

        var versionOption = new Option<string?>("--version", "-v")
        {
            Description = "Package version (if omitted, uses latest)"
        };

        var tfmOption = new Option<string?>("--tfm")
        {
            Description = "Target framework moniker (e.g., net8.0, netstandard2.0)"
        };

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: json or jsonl",
            DefaultValueFactory = _ => "json"
        };

        var filterOption = new Option<string?>("--filter")
        {
            Description = "Namespace filter (e.g., Newtonsoft.Json.Linq)"
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output file path (default: stdout)"
        };

        var noCacheOption = new Option<bool>("--no-cache")
        {
            Description = "Bypass cache",
            DefaultValueFactory = _ => false
        };

        var command = new Command("export-signatures", "Export public method signatures with XML documentation")
        {
            packageOption,
            versionOption,
            tfmOption,
            formatOption,
            filterOption,
            outputOption,
            noCacheOption
        };

        command.SetAction(Handler);
        return command;

        int Handler(ParseResult parseResult)
        {
            var package = parseResult.GetValue(packageOption);
            var version = parseResult.GetValue(versionOption);
            var tfm = parseResult.GetValue(tfmOption);
            var format = parseResult.GetValue(formatOption) ?? "json";
            var filter = parseResult.GetValue(filterOption);
            var output = parseResult.GetValue(outputOption);
            var noCache = parseResult.GetValue(noCacheOption);

            return HandlerAsync(package!, version, tfm, format, filter, output, serviceProvider).GetAwaiter().GetResult();
        }
    }

    private static async Task<int> HandlerAsync(
        string packageId,
        string? version,
        string? tfm,
        string format,
        string? namespaceFilter,
        string? output,
        IServiceProvider? serviceProvider)
    {
        try
        {
            serviceProvider ??= CreateDefaultServiceProvider();

            var resolver = serviceProvider.GetRequiredService<NuGetPackageResolver>();
            var exporter = serviceProvider.GetRequiredService<SignatureExporter>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("ExportSignaturesCommand");

            logger.LogInformation("Resolving package {PackageId} (version: {Version})", packageId, version ?? "latest");

            var packageInfo = await resolver.ResolvePackageAsync(packageId, version);

            if (packageInfo == null || !packageInfo.Resolved || string.IsNullOrEmpty(packageInfo.NupkgPath))
            {
                logger.LogError("Package {PackageId} not found", packageId);
                return 1;
            }

            var assemblyPaths = await ExtractAssembliesAsync(packageInfo.NupkgPath, tfm, logger);

            if (assemblyPaths.Count == 0)
            {
                logger.LogWarning("No assemblies found in package for TFM {Tfm}", tfm ?? "any");
                return 1;
            }

            var methods = exporter.ExportMethods(assemblyPaths, namespaceFilter);

            var result = format.ToLowerInvariant() == "jsonl"
                ? exporter.ExportToJsonL(methods)
                : exporter.ExportToJson(methods);

            if (!string.IsNullOrEmpty(output))
            {
                await File.WriteAllTextAsync(output, result);
                logger.LogInformation("Method signatures written to {OutputPath}", output);
            }
            else
            {
                Console.WriteLine(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            var loggerFactory = serviceProvider?.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("ExportSignaturesCommand");
            logger?.LogError(ex, "Failed to export signatures for package {PackageId}", packageId);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<List<string>> ExtractAssembliesAsync(string nupkgPath, string? tfm, ILogger logger)
    {
        var assemblies = new List<string>();

        using var packageReader = new PackageArchiveReader(nupkgPath);
        var libItems = await packageReader.GetLibItemsAsync(CancellationToken.None);

        var targetGroup = string.IsNullOrEmpty(tfm)
            ? libItems.OrderByDescending(g => g.TargetFramework.Version).FirstOrDefault()
            : libItems.FirstOrDefault(g => g.TargetFramework.GetShortFolderName() == tfm);

        if (targetGroup == null)
        {
            logger.LogWarning("No lib items found for TFM {Tfm}", tfm ?? "any");
            return assemblies;
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
                await stream.CopyToAsync(fileStream);
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
                await stream.CopyToAsync(fileStream);
            }
        }

        logger.LogInformation("Extracted {Count} assemblies from {Tfm}", assemblies.Count, targetGroup.TargetFramework.GetShortFolderName());

        return assemblies;
    }

    private static IServiceProvider CreateDefaultServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<NuGetPackageResolver>();
        services.AddScoped<AssemblyInspector>();
        services.AddScoped<XmlDocumentationProvider>();
        services.AddScoped<SignatureExporter>();
        return services.BuildServiceProvider();
    }
}

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
    public static Command Create(IServiceProvider? serviceProvider = null)
    {
        var packageOption = new Option<string>("--package", "-p")
        {
            Description = "Package ID",
            Required = true
        };

        var fromOption = new Option<string>("--from")
        {
            Description = "From version",
            Required = true
        };

        var toOption = new Option<string>("--to")
        {
            Description = "To version",
            Required = true
        };

        var tfmOption = new Option<string?>("--tfm")
        {
            Description = "Target framework moniker (e.g., net8.0, netstandard2.0)"
        };

        var outputOption = new Option<string?>("--output", "-o")
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

        command.SetAction(Handler);
        return command;

        int Handler(ParseResult parseResult)
        {
            var package = parseResult.GetValue(packageOption);
            var from = parseResult.GetValue(fromOption);
            var to = parseResult.GetValue(toOption);
            var tfm = parseResult.GetValue(tfmOption);
            var output = parseResult.GetValue(outputOption);

            return HandlerAsync(package!, from!, to!, tfm, output, serviceProvider).GetAwaiter().GetResult();
        }
    }

    private static async Task<int> HandlerAsync(
        string packageId,
        string fromVersion,
        string toVersion,
        string? tfm,
        string? output,
        IServiceProvider? serviceProvider)
    {
        try
        {
            serviceProvider ??= CreateDefaultServiceProvider();

            var resolver = serviceProvider.GetRequiredService<NuGetPackageResolver>();
            var exporter = serviceProvider.GetRequiredService<SignatureExporter>();
            var analyzer = serviceProvider.GetRequiredService<ApiDiffAnalyzer>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DiffCommand");

            logger.LogInformation("Comparing {PackageId} versions {From} -> {To}", packageId, fromVersion, toVersion);

            var fromPackage = await resolver.ResolvePackageAsync(packageId, fromVersion);
            if (fromPackage == null || !fromPackage.Resolved || string.IsNullOrEmpty(fromPackage.NupkgPath))
            {
                logger.LogError("Package {PackageId} version {Version} not found", packageId, fromVersion);
                return 1;
            }

            var toPackage = await resolver.ResolvePackageAsync(packageId, toVersion);
            if (toPackage == null || !toPackage.Resolved || string.IsNullOrEmpty(toPackage.NupkgPath))
            {
                logger.LogError("Package {PackageId} version {Version} not found", packageId, toVersion);
                return 1;
            }

            var fromAssemblies = await ExtractAssembliesAsync(fromPackage.NupkgPath, tfm, logger);
            var toAssemblies = await ExtractAssembliesAsync(toPackage.NupkgPath, tfm, logger);

            if (fromAssemblies.Count == 0 || toAssemblies.Count == 0)
            {
                logger.LogWarning("No assemblies found in one or both package versions for TFM {Tfm}", tfm ?? "any");
                return 1;
            }

            var methodsFrom = exporter.ExportMethods(fromAssemblies);
            var methodsTo = exporter.ExportMethods(toAssemblies);

            var targetFramework = tfm ?? fromPackage.Tfms?.FirstOrDefault() ?? "unknown";
            var diffResult = analyzer.CompareVersions(packageId, methodsFrom, methodsTo, fromVersion, toVersion, targetFramework);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(diffResult, options);

            if (!string.IsNullOrEmpty(output))
            {
                await File.WriteAllTextAsync(output, json);
                logger.LogInformation("Diff result written to {OutputPath}", output);
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        catch (Exception ex)
        {
            var loggerFactory = serviceProvider?.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("DiffCommand");
            logger?.LogError(ex, "Failed to compare package versions for {PackageId}", packageId);
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
        services.AddLogging(builder => 
        {
            var logDir = Path.Combine(Path.GetTempPath(), "nuget-toolbox", "logs");
            Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, $"nuget-toolbox-{DateTime.UtcNow:yyyyMMdd}.log");
            builder.AddFile(logFile, minimumLevel: LogLevel.Debug);
        });
        services.AddScoped<NuGetPackageResolver>();
        services.AddScoped<AssemblyInspector>();
        services.AddScoped<XmlDocumentationProvider>();
        services.AddScoped<SignatureExporter>();
        services.AddScoped<ApiDiffAnalyzer>();
        return services.BuildServiceProvider();
    }
}

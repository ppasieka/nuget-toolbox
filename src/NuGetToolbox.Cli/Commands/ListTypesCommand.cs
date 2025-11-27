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
/// ListTypes command: List public types from a NuGet package.
/// </summary>
public static class ListTypesCommand
{
    public static Command Create(IServiceProvider? serviceProvider = null)
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

        var outputOption = new Option<string?>(["--output", "-o"])
        {
            Description = "Output file path (default: stdout)"
        };

        var command = new Command("list-types", "List public types (classes, interfaces, structs, enums) from a package")
        {
            packageOption,
            versionOption,
            tfmOption,
            outputOption
        };

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var package = ctx.ParseResult.GetValueForOption(packageOption);
            var version = ctx.ParseResult.GetValueForOption(versionOption);
            var tfm = ctx.ParseResult.GetValueForOption(tfmOption);
            var output = ctx.ParseResult.GetValueForOption(outputOption);
            var token = ctx.GetCancellationToken();

            ctx.ExitCode = await HandlerAsync(package!, version, tfm, output, serviceProvider, token);
        });
        return command;
    }

    private static async Task<int> HandlerAsync(
        string packageId,
        string? version,
        string? tfm,
        string? output,
        IServiceProvider? serviceProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            serviceProvider ??= CreateDefaultServiceProvider();

            var resolver = serviceProvider.GetRequiredService<NuGetPackageResolver>();
            var inspector = serviceProvider.GetRequiredService<AssemblyInspector>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("ListTypesCommand");

            logger.LogInformation("Resolving package {PackageId} (version: {Version})", packageId, version ?? "latest");

            var packageInfo = await resolver.ResolvePackageAsync(packageId, version, null, cancellationToken);

            if (packageInfo == null || !packageInfo.Resolved || string.IsNullOrEmpty(packageInfo.NupkgPath))
            {
                logger.LogError("Package {PackageId} not found", packageId);
                return ExitCodes.NotFound;
            }

            var assemblyPaths = await ExtractAssembliesAsync(packageInfo.NupkgPath, tfm, logger, cancellationToken);

            if (assemblyPaths.Count == 0)
            {
                logger.LogWarning("No assemblies found in package for TFM {Tfm}", tfm ?? "any");
                return ExitCodes.TfmMismatch;
            }

            var allTypes = new List<Models.TypeInfo>();
            foreach (var assemblyPath in assemblyPaths)
            {
                var types = inspector.ExtractPublicTypes(assemblyPath, cancellationToken);
                allTypes.AddRange(types);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(allTypes, options);

            if (!string.IsNullOrEmpty(output))
            {
                await File.WriteAllTextAsync(output, json, cancellationToken);
                logger.LogInformation("Type information written to {OutputPath}", output);
            }
            else
            {
                Console.WriteLine(json);
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            var loggerFactory = serviceProvider?.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("ListTypesCommand");
            logger?.LogError(ex, "Failed to list types for package {PackageId}", packageId);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.Error;
        }
    }

    private static async Task<List<string>> ExtractAssembliesAsync(string nupkgPath, string? tfm, ILogger logger, CancellationToken cancellationToken)
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
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            assemblies.Add(destPath);
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
        return services.BuildServiceProvider();
    }
}

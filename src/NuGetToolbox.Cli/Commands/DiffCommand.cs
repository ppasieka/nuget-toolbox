using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGetToolbox.Cli.Services;

namespace NuGetToolbox.Cli.Commands;

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
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DiffCommand));
        string? fromTempDir = null;
        string? toTempDir = null;

        try
        {
            var resolver = serviceProvider.GetRequiredService<NuGetPackageResolver>();
            var exporter = serviceProvider.GetRequiredService<SignatureExporter>();
            var analyzer = serviceProvider.GetRequiredService<ApiDiffAnalyzer>();
            var assemblyExtractor = serviceProvider.GetRequiredService<AssemblyExtractor>();

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

            var fromResult = await assemblyExtractor.ExtractAssembliesAsync(fromPackage.NupkgPath, tfm, true, cancellationToken);
            fromTempDir = fromResult.TempDir;

            if (fromResult.ErrorMessage != null)
            {
                logger.LogError("From package: {Error}", fromResult.ErrorMessage);
                Console.Error.WriteLine($"Error (from): {fromResult.ErrorMessage}");
                return ExitCodes.TfmMismatch;
            }

            var toResult = await assemblyExtractor.ExtractAssembliesAsync(toPackage.NupkgPath, tfm, true, cancellationToken);
            toTempDir = toResult.TempDir;

            if (toResult.ErrorMessage != null)
            {
                logger.LogError("To package: {Error}", toResult.ErrorMessage);
                Console.Error.WriteLine($"Error (to): {toResult.ErrorMessage}");
                return ExitCodes.TfmMismatch;
            }

            if (fromResult.Assemblies.Count == 0 || toResult.Assemblies.Count == 0)
            {
                logger.LogWarning("No assemblies found in one or both package versions for TFM {Tfm}", tfm ?? "any");
                return ExitCodes.TfmMismatch;
            }

            var methodsFrom = exporter.ExportMethods(fromResult.Assemblies, cancellationToken: cancellationToken);
            var methodsTo = exporter.ExportMethods(toResult.Assemblies, cancellationToken: cancellationToken);

            var targetFramework = fromResult.SelectedFramework?.GetShortFolderName() ?? tfm ?? "unknown";
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
}

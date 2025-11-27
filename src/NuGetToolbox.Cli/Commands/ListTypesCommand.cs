using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGetToolbox.Cli.Services;

namespace NuGetToolbox.Cli.Commands;

public static class ListTypesCommand
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
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ListTypesCommand));
        string? tempDir = null;

        try
        {
            var resolver = serviceProvider.GetRequiredService<NuGetPackageResolver>();
            var inspector = serviceProvider.GetRequiredService<AssemblyInspector>();
            var assemblyExtractor = serviceProvider.GetRequiredService<AssemblyExtractor>();

            logger.LogInformation("Resolving package {PackageId} (version: {Version})", packageId, version ?? "latest");

            var packageInfo = await resolver.ResolvePackageAsync(packageId, version, null, cancellationToken);

            if (packageInfo == null || !packageInfo.Resolved || string.IsNullOrEmpty(packageInfo.NupkgPath))
            {
                logger.LogError("Package {PackageId} not found", packageId);
                return ExitCodes.NotFound;
            }

            var result = await assemblyExtractor.ExtractAssembliesAsync(packageInfo.NupkgPath, tfm, false, cancellationToken);
            tempDir = result.TempDir;

            if (result.ErrorMessage != null)
            {
                logger.LogError("{Error}", result.ErrorMessage);
                Console.Error.WriteLine($"Error: {result.ErrorMessage}");
                return ExitCodes.TfmMismatch;
            }

            if (result.Assemblies.Count == 0)
            {
                logger.LogWarning("No assemblies found in package for TFM {Tfm}", tfm ?? "any");
                return ExitCodes.TfmMismatch;
            }

            var allTypes = new List<Models.TypeInfo>();
            foreach (var assemblyPath in result.Assemblies)
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
            logger.LogError(ex, "Failed to list types for package {PackageId}", packageId);
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
}

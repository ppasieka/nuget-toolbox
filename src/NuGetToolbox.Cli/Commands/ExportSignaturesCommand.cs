using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGetToolbox.Cli.Services;

namespace NuGetToolbox.Cli.Commands;

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
            var assemblyExtractor = serviceProvider.GetRequiredService<AssemblyExtractor>();

            logger.LogInformation("Resolving package {PackageId} (version: {Version})", packageId, version ?? "latest");

            var packageInfo = await resolver.ResolvePackageAsync(packageId, version, cancellationToken: cancellationToken);

            if (packageInfo == null || !packageInfo.Resolved || string.IsNullOrEmpty(packageInfo.NupkgPath))
            {
                logger.LogError("Package {PackageId} not found", packageId);
                return ExitCodes.NotFound;
            }

            var result = await assemblyExtractor.ExtractAssembliesAsync(packageInfo.NupkgPath, tfm, true, cancellationToken);
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

            var methods = exporter.ExportMethods(result.Assemblies, namespaceFilter, cancellationToken);

            var jsonResult = format.ToLowerInvariant() == "jsonl"
                ? exporter.ExportToJsonL(methods)
                : exporter.ExportToJson(methods);

            if (!string.IsNullOrEmpty(output))
            {
                await File.WriteAllTextAsync(output, jsonResult, cancellationToken);
                logger.LogInformation("Method signatures written to {OutputPath}", output);
            }
            else
            {
                Console.WriteLine(jsonResult);
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
}

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGetToolbox.Cli.Services;

namespace NuGetToolbox.Cli.Commands;

/// <summary>
/// Find command: Resolve a NuGet package by ID and optional version.
/// </summary>
public static class FindCommand
{
    public static Command Create(IServiceProvider? serviceProvider = null)
    {
        var packageOption = new Option<string>("--package", "-p")
        {
            Description = "Package ID to search for",
            Required = true
        };

        var versionOption = new Option<string?>("--version", "-v")
        {
            Description = "Package version (if omitted, uses latest)"
        };

        var feedOption = new Option<string?>("--feed", "-f")
        {
            Description = "NuGet feed URL (if omitted, uses system nuget.config)"
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output file path (default: stdout)"
        };

        var command = new Command("find", "Resolve a NuGet package by ID and version")
        {
            packageOption,
            versionOption,
            feedOption,
            outputOption
        };

        command.SetAction(Handler);
        return command;

        int Handler(ParseResult parseResult)
        {
            var packageId = parseResult.GetValue(packageOption);
            var version = parseResult.GetValue(versionOption);
            var feed = parseResult.GetValue(feedOption);
            var output = parseResult.GetValue(outputOption);

            return HandlerAsync(packageId!, version, feed, output, serviceProvider).GetAwaiter().GetResult();
        }
    }

    private static async Task<int> HandlerAsync(
        string packageId,
        string? version,
        string? feed,
        string? output,
        IServiceProvider? serviceProvider)
    {
        try
        {
            // Use default service provider if not provided
            serviceProvider ??= CreateDefaultServiceProvider();

            var resolver = serviceProvider.GetRequiredService<NuGetPackageResolver>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("FindCommand");

            logger.LogInformation("Resolving package {PackageId} (version: {Version}, feed: {Feed})",
                packageId, version ?? "latest", feed ?? "system-defined");

            var packageInfo = await resolver.ResolvePackageAsync(packageId, version, feed);

            if (packageInfo == null || !packageInfo.Resolved)
            {
                logger.LogError("Package {PackageId} not found", packageId);
                return 1;
            }

            // Serialize to JSON with camelCase
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(packageInfo, options);

            // Write to file or stdout
            if (!string.IsNullOrEmpty(output))
            {
                await File.WriteAllTextAsync(output, json);
                logger.LogInformation("Package information written to {OutputPath}", output);
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
            var logger = loggerFactory?.CreateLogger("FindCommand");
            logger?.LogError(ex, "Failed to resolve package {PackageId}", packageId);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static IServiceProvider CreateDefaultServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<NuGetPackageResolver>();
        return services.BuildServiceProvider();
    }
}

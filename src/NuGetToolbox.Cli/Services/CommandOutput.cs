using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace NuGetToolbox.Cli.Services;

/// <summary>
/// Centralized utility for JSON serialization and command output handling.
/// Provides consistent formatting across all CLI commands.
/// </summary>
public static class CommandOutput
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes an object to JSON using standard CLI options (camelCase, indented, null-ignored).
    /// </summary>
    public static string SerializeJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    /// <summary>
    /// Writes content to a file or stdout depending on output path.
    /// </summary>
    public static async Task WriteResultAsync(
        string content,
        string? outputPath,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, content, cancellationToken);
            logger.LogInformation("Output written to {OutputPath}", outputPath);
        }
        else
        {
            Console.WriteLine(content);
        }
    }
}

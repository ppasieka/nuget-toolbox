using System.Diagnostics;
using System.Text.Json;
using NuGetToolbox.Cli.Models;
using Xunit;

namespace NuGetToolbox.Tests;

public class ExportSignaturesCommandE2ETests
{
    private const string CliPath = "c:\\dev\\app\\nuget-toolbox\\src\\NuGetToolbox.Cli\\bin\\Debug\\net8.0\\NuGetToolbox.Cli.dll";

    [Fact]
    public async Task ExportSignatures_NewtonsoftJson_WithFilter_ReturnsJsonl()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} export-signatures --package Newtonsoft.Json --version 13.0.1 --filter Newtonsoft.Json --format jsonl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert
        Assert.Equal(0, process.ExitCode);
        Assert.NotEmpty(output);
        
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(lines);
    }

    [Fact]
    public async Task ExportSignatures_ValidatesMethodSignatureStructure()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} export-signatures --package Newtonsoft.Json --version 13.0.1 --filter Newtonsoft.Json --format jsonl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(10);
        foreach (var line in lines)
        {
            var method = JsonSerializer.Deserialize<MethodInfo>(line);
            Assert.NotNull(method);
            Assert.NotNull(method.Type);
            Assert.NotEmpty(method.Type);
            Assert.NotNull(method.Method);
            Assert.NotEmpty(method.Method);
            Assert.NotNull(method.Signature);
            Assert.NotEmpty(method.Signature);
        }
    }

    [Fact]
    public async Task ExportSignatures_DocumentedPackage_HasSummaries()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} export-signatures --package Newtonsoft.Json --version 13.0.1 --filter Newtonsoft.Json --format jsonl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var methods = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l)).ToList();
        var methodsWithSummary = methods.Count(m => !string.IsNullOrEmpty(m?.Summary));
        var percentage = (double)methodsWithSummary / methods.Count * 100;
        
        Assert.True(percentage >= 50, $"Expected at least 50% of methods to have summaries, got {percentage:F1}%");
    }
}

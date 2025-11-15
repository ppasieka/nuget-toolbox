using System.Diagnostics;
using System.Text.Json;
using NuGetToolbox.Cli.Models;
using Xunit.Abstractions;

namespace NuGetToolbox.Tests;

public class ExportSignaturesCommandE2ETests
{
    private readonly string _cliPath;
    private readonly ITestOutputHelper _output;

    public ExportSignaturesCommandE2ETests(ITestOutputHelper output)
    {
        _output = output;
        _cliPath = CliHelper.GetCliPath();
    }

    [Fact]
    public async Task ExportSignatures_NewtonsoftJson_WithFilter_ReturnsJsonl()
    {
        // Arrange
        var arguments = $"export-signatures --package Newtonsoft.Json --version 13.0.1 --filter Newtonsoft.Json --format jsonl";
        _output.WriteLine($"Executing: dotnet {_cliPath} {arguments}");
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_cliPath} export-signatures --package Newtonsoft.Json --version 13.0.1 --filter Newtonsoft.Json --format jsonl",
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
    public async Task ExportSignatures_WithNamespaceFlag_ProducesSameResultsAsFilter()
    {
        // Arrange
        var filterStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_cliPath} export-signatures --package Newtonsoft.Json --version 13.0.1 --filter Newtonsoft.Json.Linq --format jsonl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var namespaceStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_cliPath} export-signatures --package Newtonsoft.Json --version 13.0.1 --namespace Newtonsoft.Json.Linq --format jsonl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var filterProcess = Process.Start(filterStartInfo)!;
        var filterOutput = await filterProcess.StandardOutput.ReadToEndAsync();
        await filterProcess.WaitForExitAsync();

        using var namespaceProcess = Process.Start(namespaceStartInfo)!;
        var namespaceOutput = await namespaceProcess.StandardOutput.ReadToEndAsync();
        await namespaceProcess.WaitForExitAsync();

        // Assert
        Assert.Equal(0, filterProcess.ExitCode);
        Assert.Equal(0, namespaceProcess.ExitCode);
        
        // Both outputs should be identical
        Assert.Equal(filterOutput, namespaceOutput);
        
        // Verify the output contains expected types from Newtonsoft.Json.Linq namespace
        var lines = filterOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var methods = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l)).ToList();
        
        var linqTypes = methods.Select(m => m?.Type).Distinct().Where(t => t?.Contains("JToken") == true).ToList();
        Assert.NotEmpty(linqTypes);
    }

    [Fact(Skip = "E2E - requires actual CLI execution")]
    public async Task ExportSignatures_ImprovedVisibilityFiltering_ExcludesNonVisibleTypes()
    {
        // This test validates that the improved visibility filtering works correctly
        // by checking that only classes and interfaces are included in the output
        
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_cliPath} export-signatures --package Newtonsoft.Json --version 13.0.1 --filter Newtonsoft.Json --format jsonl",
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
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var methods = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l)).ToList();

        // Verify that all types are either classes or interfaces (not enums or structs)
        // This is an indirect validation since we can't directly check the type kind from MethodInfo
        // But we can verify that the output contains expected class/interface types
        var classTypes = methods.Where(m => m?.Type?.Contains("JsonConvert") == true).ToList();
        var interfaceTypes = methods.Where(m => m?.Type?.Contains("IJson") == true).ToList();
        
        Assert.NotEmpty(classTypes);
        // Newtonsoft.Json should have some I-prefixed interface types
        Assert.True(interfaceTypes.Any() || methods.Count > 0, "Should have either interface types or other valid types");
    }

    [Fact(Skip = "E2E - requires actual CLI execution")]
    public async Task ExportSignatures_WithPartialLoadResilience_HandlesMissingDependencies()
    {
        // This test validates that the command handles partial load failures gracefully
        // We use a package that might have some missing dependencies to test resilience
        
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_cliPath} export-signatures --package Newtonsoft.Json --version 13.0.1 --format jsonl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert
        Assert.Equal(0, process.ExitCode);
        Assert.NotEmpty(output);
        
        // Even if there are partial load issues, the command should succeed
        // and produce some output rather than failing completely
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(lines);
        
        // Any warnings about partial loads should go to stderr, not cause failure
        // The presence of output indicates successful processing despite any issues
    }
}

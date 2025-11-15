using System.Diagnostics;
using System.Text.Json;
using NuGetToolbox.Cli.Models;

namespace NuGetToolbox.Tests;

public class DiffCommandE2ETests
{
    private const string CliPath = "c:\\dev\\app\\nuget-toolbox\\src\\NuGetToolbox.Cli\\bin\\Debug\\net8.0\\NuGetToolbox.Cli.dll";

    [Fact]
    public async Task Diff_NewtonsoftJson_Versions_ReturnsValidJson()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} diff --package Newtonsoft.Json --from 13.0.1 --to 13.0.3",
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

        var diffResult = JsonSerializer.Deserialize<DiffResult>(output);
        Assert.NotNull(diffResult);
        Assert.Equal("Newtonsoft.Json", diffResult.PackageId);
        Assert.Equal("13.0.1", diffResult.VersionFrom);
        Assert.Equal("13.0.3", diffResult.VersionTo);
        Assert.NotNull(diffResult.Tfm);
        Assert.NotNull(diffResult.Added);
    }

    [Fact]
    public async Task Diff_ValidatesOutputStructure()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} diff --package Newtonsoft.Json --from 13.0.1 --to 13.0.3",
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
        Assert.Contains("\"packageId\"", output);
        Assert.Contains("\"versionFrom\"", output);
        Assert.Contains("\"versionTo\"", output);
        Assert.Contains("\"tfm\"", output);
        Assert.Contains("\"added\"", output);
        Assert.Contains("\"compatible\"", output);

        var diffResult = JsonSerializer.Deserialize<DiffResult>(output);
        Assert.NotNull(diffResult);
        Assert.IsType<bool>(diffResult.Compatible);
    }

    [Fact]
    public async Task Diff_SameVersion_ReturnsEmptyChanges()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} diff --package Newtonsoft.Json --from 13.0.1 --to 13.0.1",
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
        var diffResult = JsonSerializer.Deserialize<DiffResult>(output);
        Assert.NotNull(diffResult);
        Assert.True(diffResult.Added == null || diffResult.Added.Count == 0);
        Assert.True(diffResult.Removed == null || diffResult.Removed.Count == 0);
        Assert.True(diffResult.Compatible);
    }
}

using System.Diagnostics;
using System.Text.Json;
using NuGetToolbox.Cli.Models;
using Xunit;

namespace NuGetToolbox.Tests;

public class FindCommandE2ETests
{
    private const string CliPath = "c:\\dev\\app\\nuget-toolbox\\src\\NuGetToolbox.Cli\\bin\\Debug\\net8.0\\NuGetToolbox.Cli.dll";

    [Fact]
    public async Task Find_NewtonsoftJson_13_0_1_ReturnsValidJson()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} find --package Newtonsoft.Json --version 13.0.1",
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
        
        var packageInfo = JsonSerializer.Deserialize<PackageInfo>(output);
        Assert.NotNull(packageInfo);
        Assert.Equal("Newtonsoft.Json", packageInfo.PackageId);
        Assert.Equal("13.0.1", packageInfo.Version);
        Assert.True(packageInfo.Resolved);
        Assert.NotNull(packageInfo.Source);
        Assert.NotNull(packageInfo.NupkgPath);
        Assert.NotNull(packageInfo.Tfms);
        Assert.NotEmpty(packageInfo.Tfms);
    }

    [Fact]
    public async Task Find_NewtonsoftJson_ValidatesJsonStructure()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} find --package Newtonsoft.Json --version 13.0.1",
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
        Assert.Contains("\"resolvedVersion\"", output);
        Assert.Contains("\"resolved\"", output);
        Assert.Contains("\"source\"", output);
        Assert.Contains("\"nupkgPath\"", output);
        Assert.Contains("\"targetFrameworks\"", output);
    }
}

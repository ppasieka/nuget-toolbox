using System.Diagnostics;
using System.Text.Json;
using NuGetToolbox.Cli.Models;
using Xunit.Abstractions;

namespace NuGetToolbox.Tests;

public class FindCommandE2ETests
{
    private readonly string _cliPath;
    private readonly ITestOutputHelper _output;

    public FindCommandE2ETests(ITestOutputHelper output)
    {
        _output = output;
        _cliPath = CliHelper.GetCliPath();
    }

    [Fact]
    public async Task Find_NewtonsoftJson_13_0_1_ReturnsValidJson()
    {
        // Arrange
        var arguments = $"find --package Newtonsoft.Json --version 13.0.1";
        _output.WriteLine($"Executing: dotnet {_cliPath} {arguments}");
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_cliPath} find --package Newtonsoft.Json --version 13.0.1",
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

}

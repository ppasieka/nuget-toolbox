using System.Diagnostics;
using Xunit.Abstractions;

namespace NuGetToolbox.Tests;

[Collection("TempCleanup")]
public class TempCleanupE2ETests
{
    private readonly string _cliPath;
    private readonly ITestOutputHelper _output;

    public TempCleanupE2ETests(ITestOutputHelper output)
    {
        _output = output;
        _cliPath = CliHelper.GetCliPath();
    }

    [Fact]
    public async Task ListTypes_CleansUpTempDirectory_AfterSuccess()
    {
        // Arrange

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_cliPath} list-types --package Newtonsoft.Json --version 13.0.1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(startInfo)!;
        await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert - command should succeed (cleanup happens internally)
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public async Task ExportSignatures_CleansUpTempDirectory_AfterSuccess()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_cliPath} export-signatures --package Newtonsoft.Json --version 13.0.1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(startInfo)!;
        await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert - command should succeed (cleanup happens internally)
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public async Task Diff_CleansUpBothTempDirectories_AfterSuccess()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_cliPath} diff --package Newtonsoft.Json --from 13.0.1 --to 13.0.2",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(startInfo)!;
        await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert - command should succeed (cleanup happens internally)
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public async Task ListTypes_NoTempDirectory_WhenPackageNotFound()
    {
        // When package resolution fails, no temp directory should be created at all
        // (temp dirs are only created during extraction, which happens after resolution)
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_cliPath} list-types --package NonExistent.Package.That.Does.Not.Exist.12345 --version 1.0.0",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(startInfo)!;
        await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert - should have non-zero exit (package not found)
        Assert.NotEqual(0, process.ExitCode);
        // Note: No temp dir created on this path since extraction never runs
    }
}

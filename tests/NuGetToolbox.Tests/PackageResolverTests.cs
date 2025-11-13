using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using NuGetToolbox.Cli.Services;

namespace NuGetToolbox.Tests;

/// <summary>
/// Tests for NuGetPackageResolver service.
/// </summary>
public class PackageResolverTests
{
    [Fact]
    public async Task GetTargetFrameworksAsync_WithExtractedFolderOnly_ReadsFromFolder()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var packageFolder = Path.Combine(tempDir, "testpackage", "1.0.0");
        var libFolder = Path.Combine(packageFolder, "lib", "net8.0");
        Directory.CreateDirectory(libFolder);

        // Create a minimal .nuspec file
        var nuspecContent = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <authors>Test</authors>
    <description>Test package</description>
  </metadata>
</package>";
        File.WriteAllText(Path.Combine(packageFolder, "testpackage.nuspec"), nuspecContent);
        
        // Create a dummy assembly
        File.WriteAllText(Path.Combine(libFolder, "TestPackage.dll"), "dummy");

        var mockLogger = new Mock<ILogger<NuGetPackageResolver>>();
        var resolver = new NuGetPackageResolver(mockLogger.Object);

        // Construct path to .nupkg that doesn't exist
        var nupkgPath = Path.Combine(packageFolder, "testpackage.1.0.0.nupkg");

        try
        {
            // Act - use reflection to call private method
            var method = typeof(NuGetPackageResolver).GetMethod(
                "GetTargetFrameworksAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var result = await (Task<List<string>>)method!.Invoke(
                resolver,
                new object[] { nupkgPath, CancellationToken.None })!;

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("net8.0", result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task GetTargetFrameworksAsync_WithNupkgFile_ReadsFromArchive()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var packageFolder = Path.Combine(tempDir, "testpackage", "1.0.0");
        Directory.CreateDirectory(packageFolder);

        var nupkgPath = Path.Combine(packageFolder, "testpackage.1.0.0.nupkg");

        // Create a valid .nupkg (zip file with nuspec and lib folder)
        using (var zipArchive = ZipFile.Open(nupkgPath, ZipArchiveMode.Create))
        {
            // Add .nuspec
            var nuspecEntry = zipArchive.CreateEntry("testpackage.nuspec");
            using (var writer = new StreamWriter(nuspecEntry.Open()))
            {
                writer.Write(@"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <authors>Test</authors>
    <description>Test package</description>
  </metadata>
</package>");
            }

            // Add lib/net6.0/TestPackage.dll
            var dllEntry = zipArchive.CreateEntry("lib/net6.0/TestPackage.dll");
            using (var writer = new StreamWriter(dllEntry.Open()))
            {
                writer.Write("dummy");
            }
        }

        var mockLogger = new Mock<ILogger<NuGetPackageResolver>>();
        var resolver = new NuGetPackageResolver(mockLogger.Object);

        try
        {
            // Act
            var method = typeof(NuGetPackageResolver).GetMethod(
                "GetTargetFrameworksAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var result = await (Task<List<string>>)method!.Invoke(
                resolver,
                new object[] { nupkgPath, CancellationToken.None })!;

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("net6.0", result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ResolvePackageAsync_WithValidPackageId_ReturnsPackageInfo()
    {
        // Arrange
        // TODO: Setup mock NuGet source

        // Act
        // TODO: Call resolver

        // Assert
        // TODO: Verify results
    }

    [Fact]
    public async Task ResolvePackageAsync_WithVersion_ResolvesToSpecificVersion()
    {
        // Arrange
        // TODO: Setup

        // Act
        // TODO: Execute

        // Assert
        // TODO: Verify
    }
}

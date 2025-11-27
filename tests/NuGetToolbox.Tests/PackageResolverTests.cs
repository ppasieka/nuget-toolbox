using System.IO.Compression;
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
                [nupkgPath, CancellationToken.None]
            )!;

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
                [nupkgPath, CancellationToken.None]
            )!;

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
    public async Task GetDirectDependenciesAsync_WithNupkgFile_ReturnsDependencies()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var packageFolder = Path.Combine(tempDir, "testpackage", "1.0.0");
        Directory.CreateDirectory(packageFolder);

        var nupkgPath = Path.Combine(packageFolder, "testpackage.1.0.0.nupkg");

        // Create a valid .nupkg with dependencies
        using (var zipArchive = ZipFile.Open(nupkgPath, ZipArchiveMode.Create))
        {
            // Add .nuspec with dependencies
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
    <dependencies>
      <group targetFramework=""net8.0"">
        <dependency id=""Newtonsoft.Json"" version=""13.0.3"" />
        <dependency id=""Microsoft.Extensions.Logging"" version=""8.0.0"" />
      </group>
      <group targetFramework=""net6.0"">
        <dependency id=""Newtonsoft.Json"" version=""13.0.1"" />
      </group>
    </dependencies>
  </metadata>
</package>");
            }

            // Add lib folder
            var dllEntry = zipArchive.CreateEntry("lib/net8.0/TestPackage.dll");
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
            var dependencies = await resolver.GetDirectDependenciesAsync(nupkgPath);

            // Assert
            Assert.NotNull(dependencies);
            Assert.NotEmpty(dependencies);
            Assert.Equal(3, dependencies.Count);

            var net8Deps = dependencies.Where(d => d.TargetFramework == "net8.0").ToList();
            Assert.Equal(2, net8Deps.Count);
            Assert.Contains(net8Deps, d => d.PackageId == "Newtonsoft.Json");
            Assert.Contains(net8Deps, d => d.PackageId == "Microsoft.Extensions.Logging");

            var net6Deps = dependencies.Where(d => d.TargetFramework == "net6.0").ToList();
            Assert.Single(net6Deps);
            Assert.Contains(net6Deps, d => d.PackageId == "Newtonsoft.Json");
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
    public async Task GetDirectDependenciesAsync_WithNoDependencies_ReturnsEmptyList()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var packageFolder = Path.Combine(tempDir, "testpackage", "1.0.0");
        Directory.CreateDirectory(packageFolder);

        var nupkgPath = Path.Combine(packageFolder, "testpackage.1.0.0.nupkg");

        // Create a valid .nupkg without dependencies
        using (var zipArchive = ZipFile.Open(nupkgPath, ZipArchiveMode.Create))
        {
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
        }

        var mockLogger = new Mock<ILogger<NuGetPackageResolver>>();
        var resolver = new NuGetPackageResolver(mockLogger.Object);

        try
        {
            // Act
            var dependencies = await resolver.GetDirectDependenciesAsync(nupkgPath);

            // Assert
            Assert.NotNull(dependencies);
            Assert.Empty(dependencies);
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
}

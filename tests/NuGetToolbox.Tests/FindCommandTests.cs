using NuGetToolbox.Cli.Commands;
using NuGetToolbox.Cli.Models;
using Microsoft.Extensions.DependencyInjection;

namespace NuGetToolbox.Tests;

public class FindCommandTests
{
    [Fact]
    public void Create_WithServiceProvider_ReturnsCommand()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var command = FindCommand.Create(serviceProvider);

        // Assert
        Assert.NotNull(command);
        Assert.Equal("find", command.Name);
    }

    [Fact]
    public void PackageInfo_SerializesWithCorrectJsonPropertyNames()
    {
        // Arrange
        var packageInfo = new PackageInfo
        {
            PackageId = "Newtonsoft.Json",
            Version = "13.0.3",
            Resolved = true,
            Source = "https://api.nuget.org/v3/index.json",
            NupkgPath = "/path/to/newtonsoft.json.13.0.3.nupkg",
            Tfms = ["net462", "netstandard2.0", "net6.0"]
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(packageInfo, options);

        // Assert
        Assert.Contains("\"packageId\"", json);
        Assert.Contains("\"resolvedVersion\"", json);
        Assert.Contains("\"targetFrameworks\"", json);
        Assert.Contains("\"nupkgPath\"", json);
        Assert.Contains("Newtonsoft.Json", json);
        Assert.Contains("13.0.3", json);
    }

    [Fact]
    public void PackageInfo_ResolvedTrueSerializesCorrectly()
    {
        // Arrange
        var packageInfo = new PackageInfo
        {
            PackageId = "Test",
            Version = "1.0.0",
            Resolved = true
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(packageInfo);

        // Assert
        Assert.Contains("\"resolved\":true", json);
    }

    [Fact]
    public void PackageInfo_ResolvedFalseSerializesCorrectly()
    {
        // Arrange
        var packageInfo = new PackageInfo
        {
            PackageId = "NotFound",
            Version = "",
            Resolved = false
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(packageInfo);

        // Assert
        Assert.Contains("\"resolved\":false", json);
    }

    [Fact]
    public void PackageInfo_WithMultipleTfms_SerializesList()
    {
        // Arrange
        var packageInfo = new PackageInfo
        {
            PackageId = "Test",
            Version = "1.0.0",
            Resolved = true,
            Tfms = ["net6.0", "net7.0", "net8.0"]
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(packageInfo, options);

        // Assert
        Assert.Contains("\"targetFrameworks\"", json);
        Assert.Contains("net6.0", json);
        Assert.Contains("net7.0", json);
        Assert.Contains("net8.0", json);
    }

    [Fact]
    public void PackageInfo_AllPropertiesPopulated_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
  ""packageId"": ""Newtonsoft.Json"",
  ""resolvedVersion"": ""13.0.3"",
  ""resolved"": true,
  ""source"": ""https://api.nuget.org/v3/index.json"",
  ""nupkgPath"": ""/path/to/package.nupkg"",
  ""targetFrameworks"": [""net462"", ""netstandard2.0""]
}";

        // Act
        var packageInfo = System.Text.Json.JsonSerializer.Deserialize<PackageInfo>(json);

        // Assert
        Assert.NotNull(packageInfo);
        Assert.Equal("Newtonsoft.Json", packageInfo.PackageId);
        Assert.Equal("13.0.3", packageInfo.Version);
        Assert.True(packageInfo.Resolved);
        Assert.Equal("https://api.nuget.org/v3/index.json", packageInfo.Source);
        Assert.Equal("/path/to/package.nupkg", packageInfo.NupkgPath);
        Assert.NotNull(packageInfo.Tfms);
        Assert.Equal(2, packageInfo.Tfms.Count);
    }
}

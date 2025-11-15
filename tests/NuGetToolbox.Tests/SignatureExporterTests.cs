using System.Diagnostics;
using System.Text.Json;
using NuGetToolbox.Cli.Models;

namespace NuGetToolbox.Tests;

public class SignatureExporterTests
{
    private const string CliPath = "c:\\dev\\app\\nuget-toolbox\\src\\NuGetToolbox.Cli\\bin\\Debug\\net8.0\\NuGetToolbox.Cli.dll";

    [Fact]
    public async Task ExportSignatures_NewtonsoftJson_ExtractsMethodSignaturesWithParameters()
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
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var signatures = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l)).ToList();

        // Find a method with parameters - JsonConvert.SerializeObject
        var serializeMethod = signatures.FirstOrDefault(s =>
            s?.Type == "Newtonsoft.Json.JsonConvert" &&
            s.Method == "SerializeObject" &&
            s.Signature.Contains("Object"));

        Assert.NotNull(serializeMethod);
        Assert.NotNull(serializeMethod.Signature);
        Assert.NotEmpty(serializeMethod.Signature);

        // Verify signature contains method name and parameters
        Assert.Contains("SerializeObject", serializeMethod.Signature);
        Assert.Contains("Object", serializeMethod.Signature);

        // Verify it has parameters information
        Assert.NotNull(serializeMethod.Params);
        Assert.NotEmpty(serializeMethod.Params);
    }

    [Fact]
    public async Task ExportSignatures_NewtonsoftJson_ExtractsReturnTypeDocumentation()
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
        var signatures = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l)).ToList();
        var methodsWithReturnDocs = signatures.Where(s => !string.IsNullOrEmpty(s?.Returns)).ToList();

        Assert.NotEmpty(methodsWithReturnDocs);

        // Verify at least one method has return type documentation
        var hasReturnDoc = methodsWithReturnDocs.Any(m => !string.IsNullOrWhiteSpace(m?.Returns));
        Assert.True(hasReturnDoc, "Expected at least one method to have return type documentation");
    }

    [Fact]
    public async Task ExportSignatures_NewtonsoftJson_ExtractsXmlSummaryDocumentation()
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
        var signatures = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l)).ToList();
        var methodsWithSummary = signatures.Where(s => !string.IsNullOrEmpty(s?.Summary)).ToList();

        Assert.NotEmpty(methodsWithSummary);

        // At least 50% of methods should have XML documentation
        var percentageWithDocs = (double)methodsWithSummary.Count / signatures.Count * 100;
        Assert.True(percentageWithDocs >= 50,
            $"Expected at least 50% of methods to have XML documentation, got {percentageWithDocs:F1}%");
    }

    [Fact]
    public async Task ExportSignatures_NewtonsoftJson_ExtractsParameterDocumentation()
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
        var signatures = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l)).ToList();
        var methodsWithParamDocs = signatures.Where(s => s?.Params != null && s.Params.Count > 0).ToList();

        Assert.NotEmpty(methodsWithParamDocs);

        // Find a specific method we know has parameter documentation
        var deserializeMethod = signatures.FirstOrDefault(s =>
            s?.Type == "Newtonsoft.Json.JsonConvert" &&
            s.Method == "DeserializeObject" &&
            s.Params != null);

        if (deserializeMethod != null && deserializeMethod.Params != null)
        {
            Assert.NotEmpty(deserializeMethod.Params);

            // Verify parameter documentation is not just empty strings
            var nonEmptyParams = deserializeMethod.Params.Where(p => !string.IsNullOrWhiteSpace(p.Value)).ToList();
            Assert.NotEmpty(nonEmptyParams);
        }
    }

    [Fact]
    public async Task ExportSignatures_NewtonsoftJson_ValidatesCompleteMethodInfo()
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

        // Assert - pick a well-known method and validate all its components
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var signatures = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l)).ToList();

        var toStringMethod = signatures.FirstOrDefault(s =>
            s?.Type == "Newtonsoft.Json.JsonConvert" &&
            s.Method == "ToString" &&
            s.Signature.Contains("DateTime"));

        if (toStringMethod != null)
        {
            // Must have type
            Assert.NotNull(toStringMethod.Type);
            Assert.Equal("Newtonsoft.Json.JsonConvert", toStringMethod.Type);

            // Must have method name
            Assert.NotNull(toStringMethod.Method);
            Assert.Equal("ToString", toStringMethod.Method);

            // Must have signature
            Assert.NotNull(toStringMethod.Signature);
            Assert.Contains("DateTime", toStringMethod.Signature);

            // Should have summary (Newtonsoft.Json is well-documented)
            Assert.NotNull(toStringMethod.Summary);
            Assert.NotEmpty(toStringMethod.Summary);
        }
    }

    [Fact]
    public async Task ExportSignatures_OutputAsJsonL_IsDeserializable()
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
        Assert.NotEmpty(output);

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(lines);

        // Verify each line is valid JSON and deserializable
        foreach (var line in lines.Take(10))
        {
            var method = JsonSerializer.Deserialize<MethodInfo>(line, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(method);
            Assert.NotNull(method.Type);
            Assert.NotNull(method.Method);
            Assert.NotNull(method.Signature);
        }
    }

    [Fact]
    public async Task ExportSignatures_IncludesParameterTypeAndNameMetadata()
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
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var signatures = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })).ToList();

        var serializeMethod = signatures.FirstOrDefault(s =>
            s?.Type == "Newtonsoft.Json.JsonConvert" &&
            s.Method == "SerializeObject" &&
            s.Parameters != null && s.Parameters.Count > 0);

        Assert.NotNull(serializeMethod);
        Assert.NotNull(serializeMethod.Parameters);
        Assert.NotEmpty(serializeMethod.Parameters);

        var firstParam = serializeMethod.Parameters.First();
        Assert.NotNull(firstParam.Name);
        Assert.NotNull(firstParam.Type);
        Assert.NotEmpty(firstParam.Name);
        Assert.NotEmpty(firstParam.Type);
    }

    [Fact]
    public async Task ExportSignatures_IncludesReturnTypeMetadata()
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
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var signatures = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })).ToList();

        var serializeMethod = signatures.FirstOrDefault(s =>
            s?.Type == "Newtonsoft.Json.JsonConvert" &&
            s.Method == "SerializeObject");

        Assert.NotNull(serializeMethod);
        Assert.NotNull(serializeMethod.ReturnType);
        Assert.NotEmpty(serializeMethod.ReturnType);
        Assert.Equal("System.String", serializeMethod.ReturnType);
    }

    [Fact]
    public async Task ExportSignatures_ParametersPopulatedWhenXmlDocsMissing()
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
        var signatures = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })).ToList();

        var methodsWithoutParamDocs = signatures.Where(s => s?.Params == null || s.Params.Count == 0).ToList();

        foreach (var method in methodsWithoutParamDocs.Take(10))
        {
            if (method?.Parameters != null && method.Parameters.Count > 0)
            {
                Assert.NotNull(method.Parameters);
                Assert.All(method.Parameters, p =>
                {
                    Assert.NotNull(p.Name);
                    Assert.NotNull(p.Type);
                });
                return;
            }
        }
    }

    [Fact]
    public async Task ExportSignatures_GenericReturnTypes_ExtractedCorrectly()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} export-signatures --package Newtonsoft.Json --version 13.0.1 --filter Newtonsoft.Json.Linq --format jsonl",
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
        var signatures = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })).ToList();

        // Find methods with generic return types (e.g., IEnumerable<JToken>)
        var genericReturnMethods = signatures
            .Where(s => s?.ReturnType != null && s.ReturnType.Contains("<") && s.ReturnType.Contains(">"))
            .ToList();

        Assert.NotEmpty(genericReturnMethods);

        // Verify the angle brackets are not encoded
        var sampleMethod = genericReturnMethods.First();
        Assert.NotNull(sampleMethod.ReturnType);
        Assert.DoesNotContain("&lt;", sampleMethod.ReturnType);
        Assert.DoesNotContain("&gt;", sampleMethod.ReturnType);
        Assert.Contains("<", sampleMethod.ReturnType);
        Assert.Contains(">", sampleMethod.ReturnType);
    }

    [Fact]
    public async Task ExportSignatures_GenericParameterTypes_ExtractedCorrectly()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} export-signatures --package Newtonsoft.Json --version 13.0.1 --filter Newtonsoft.Json.Linq --format jsonl",
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
        var signatures = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })).ToList();

        // Find methods with generic parameter types
        var methodsWithGenericParams = signatures
            .Where(s => s?.Parameters != null &&
                        s.Parameters.Any(p => p.Type.Contains("<") && p.Type.Contains(">")))
            .ToList();

        Assert.NotEmpty(methodsWithGenericParams);

        // Verify a specific method with generic parameters
        var sampleMethod = methodsWithGenericParams.First();
        var genericParam = sampleMethod.Parameters!.First(p => p.Type.Contains("<"));

        Assert.NotNull(genericParam.Type);
        Assert.DoesNotContain("&lt;", genericParam.Type);
        Assert.DoesNotContain("&gt;", genericParam.Type);
        Assert.Contains("<", genericParam.Type);
        Assert.Contains(">", genericParam.Type);
    }

    [Fact]
    public async Task ExportSignatures_ComplexGenericTypes_FormattedCorrectly()
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
        var signatures = lines.Select(l => JsonSerializer.Deserialize<MethodInfo>(l, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })).ToList();

        // Look for methods with complex generic types (e.g., IList<T>, Dictionary<K,V>)
        var complexGenericMethods = signatures
            .Where(s => s?.Parameters != null &&
                        s.Parameters.Any(p => p.Type.Contains("IList") || p.Type.Contains("Dictionary") || p.Type.Contains("IEnumerable")))
            .ToList();

        if (complexGenericMethods.Any())
        {
            var method = complexGenericMethods.First();
            var genericParam = method.Parameters!.First(p => p.Type.Contains("IList") || p.Type.Contains("Dictionary") || p.Type.Contains("IEnumerable"));

            // Verify proper formatting with full namespace and no HTML encoding
            Assert.StartsWith("System.", genericParam.Type);
            Assert.Contains("<", genericParam.Type);
            Assert.Contains(">", genericParam.Type);
            Assert.DoesNotContain("&lt;", genericParam.Type);
            Assert.DoesNotContain("&gt;", genericParam.Type);
        }
    }
}

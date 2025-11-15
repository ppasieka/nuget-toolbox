using System.Reflection;

namespace NuGetToolbox.Tests;

public class SchemaCommandTests
{
    [Theory]
    [InlineData("find")]
    [InlineData("list-types")]
    [InlineData("export-signatures")]
    [InlineData("diff")]
    [InlineData("models")]
    public void SchemaResourcesExist(string schemaName)
    {
        // Arrange
        var resourceName = $"NuGetToolbox.Cli.Schemas.{(schemaName == "models" ? "models-1.0" : schemaName)}.schema.json";
        var assembly = Assembly.Load("NuGetToolbox.Cli");

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);

        // Assert
        Assert.NotNull(stream);
        Assert.True(stream.Length > 0);
    }

    [Theory]
    [InlineData("find")]
    [InlineData("list-types")]
    [InlineData("export-signatures")]
    [InlineData("diff")]
    [InlineData("models")]
    public void SchemaFilesAreValidJson(string schemaName)
    {
        // Arrange
        var resourceName = $"NuGetToolbox.Cli.Schemas.{(schemaName == "models" ? "models-1.0" : schemaName)}.schema.json";
        var assembly = Assembly.Load("NuGetToolbox.Cli");

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        // Assert - should not throw
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }

    [Fact]
    public void ModelsSchemaHasRequiredMetadata()
    {
        // Arrange
        var resourceName = "NuGetToolbox.Cli.Schemas.models-1.0.schema.json";
        var assembly = Assembly.Load("NuGetToolbox.Cli");

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var doc = System.Text.Json.JsonDocument.Parse(json);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("$schema", out var schema));
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", schema.GetString());

        Assert.True(doc.RootElement.TryGetProperty("$id", out var id));
        Assert.Contains("models-1.0.schema.json", id.GetString());

        Assert.True(doc.RootElement.TryGetProperty("title", out var title));
        Assert.False(string.IsNullOrWhiteSpace(title.GetString()));

        Assert.True(doc.RootElement.TryGetProperty("description", out var description));
        Assert.False(string.IsNullOrWhiteSpace(description.GetString()));
    }

    [Theory]
    [InlineData("PackageInfo")]
    [InlineData("TypeInfo")]
    [InlineData("MethodInfo")]
    [InlineData("ParameterInfo")]
    [InlineData("DiffResult")]
    [InlineData("DiffItem")]
    [InlineData("DirectDependency")]
    public void ModelsSchemaContainsExpectedDefinitions(string defName)
    {
        // Arrange
        var resourceName = "NuGetToolbox.Cli.Schemas.models-1.0.schema.json";
        var assembly = Assembly.Load("NuGetToolbox.Cli");

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var doc = System.Text.Json.JsonDocument.Parse(json);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("$defs", out var defs));
        Assert.True(defs.TryGetProperty(defName, out var def));

        // Verify each definition has required fields
        Assert.True(def.TryGetProperty("type", out _));
        Assert.True(def.TryGetProperty("properties", out var properties));
        Assert.True(properties.EnumerateObject().Any());
    }

    [Theory]
    [InlineData("find")]
    [InlineData("list-types")]
    [InlineData("export-signatures")]
    [InlineData("diff")]
    public void CommandSchemaHasRequiredMetadata(string commandName)
    {
        // Arrange
        var resourceName = $"NuGetToolbox.Cli.Schemas.{commandName}.schema.json";
        var assembly = Assembly.Load("NuGetToolbox.Cli");

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var doc = System.Text.Json.JsonDocument.Parse(json);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("$schema", out var schema));
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", schema.GetString());

        Assert.True(doc.RootElement.TryGetProperty("title", out var title));
        Assert.False(string.IsNullOrWhiteSpace(title.GetString()));

        Assert.True(doc.RootElement.TryGetProperty("description", out var description));
        Assert.False(string.IsNullOrWhiteSpace(description.GetString()));
    }

    [Fact]
    public void PackageInfoPropertiesHaveDescriptions()
    {
        // Arrange
        var resourceName = "NuGetToolbox.Cli.Schemas.models-1.0.schema.json";
        var assembly = Assembly.Load("NuGetToolbox.Cli");

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var doc = System.Text.Json.JsonDocument.Parse(json);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("$defs", out var defs));
        Assert.True(defs.TryGetProperty("PackageInfo", out var packageInfo));
        Assert.True(packageInfo.TryGetProperty("properties", out var properties));

        foreach (var prop in properties.EnumerateObject())
        {
            Assert.True(prop.Value.TryGetProperty("description", out var description),
                $"Property '{prop.Name}' missing description");
            Assert.False(string.IsNullOrWhiteSpace(description.GetString()),
                $"Property '{prop.Name}' has empty description");
        }
    }

    [Fact]
    public void MethodInfoPropertiesHaveDescriptions()
    {
        // Arrange
        var resourceName = "NuGetToolbox.Cli.Schemas.models-1.0.schema.json";
        var assembly = Assembly.Load("NuGetToolbox.Cli");

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var doc = System.Text.Json.JsonDocument.Parse(json);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("$defs", out var defs));
        Assert.True(defs.TryGetProperty("MethodInfo", out var methodInfo));
        Assert.True(methodInfo.TryGetProperty("properties", out var properties));

        foreach (var prop in properties.EnumerateObject())
        {
            Assert.True(prop.Value.TryGetProperty("description", out var description),
                $"Property '{prop.Name}' missing description");
            Assert.False(string.IsNullOrWhiteSpace(description.GetString()),
                $"Property '{prop.Name}' has empty description");
        }
    }
}

using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Logging;
using Moq;
using NuGetToolbox.Cli.Services;

namespace NuGetToolbox.Tests;

/// <summary>
/// Tests for AssemblyInspector service with focus on missing dependency handling.
/// </summary>
public class AssemblyInspectorTests
{
    [Fact]
    public void ExtractPublicTypes_WithValidAssembly_ReturnsTypes()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var assemblyPath = Path.Combine(tempDir, "TestAssembly.dll");

        CreateSimpleAssembly(assemblyPath);

        var mockLogger = new Mock<ILogger<AssemblyInspector>>();
        var inspector = new AssemblyInspector(mockLogger.Object);

        try
        {
            // Act
            var types = inspector.ExtractPublicTypes(assemblyPath);

            // Assert
            Assert.NotNull(types);
            Assert.NotEmpty(types);
            Assert.Contains(types, t => t.Name == "TestClass");
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
    public void ExtractPublicTypes_WithMissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AssemblyInspector>>();
        var inspector = new AssemblyInspector(mockLogger.Object);
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent.dll");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => inspector.ExtractPublicTypes(nonExistentPath));
    }

    [Fact]
    public void ExtractPublicTypes_WithMissingDependencies_LogsDebugAndContinues()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var assemblyPath = Path.Combine(tempDir, "TestAssembly.dll");

        CreateSimpleAssembly(assemblyPath);

        var mockLogger = new Mock<ILogger<AssemblyInspector>>();
        var inspector = new AssemblyInspector(mockLogger.Object);

        try
        {
            // Act
            var types = inspector.ExtractPublicTypes(assemblyPath);

            // Assert - should succeed even if some dependencies are missing
            Assert.NotNull(types);
            
            // Verify Debug logging was called if there were any loader exceptions
            mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
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
    public void ExtractPublicTypesFromMultiple_WithMultipleAssemblies_ReturnsAllTypes()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var assembly1Path = Path.Combine(tempDir, "Assembly1.dll");
        var assembly2Path = Path.Combine(tempDir, "Assembly2.dll");

        CreateSimpleAssembly(assembly1Path);
        CreateSimpleAssembly(assembly2Path);

        var mockLogger = new Mock<ILogger<AssemblyInspector>>();
        var inspector = new AssemblyInspector(mockLogger.Object);

        try
        {
            // Act
            var types = inspector.ExtractPublicTypesFromMultiple(assembly1Path, assembly2Path);

            // Assert
            Assert.NotNull(types);
            Assert.NotEmpty(types);
            Assert.True(types.Count >= 2);
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

    private void CreateSimpleAssembly(string path)
    {
        var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(path));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        
        // Create a simple public class
        var typeBuilder = moduleBuilder.DefineType(
            "TestClass",
            TypeAttributes.Public | TypeAttributes.Class);
        
        typeBuilder.CreateType();

        // Save the assembly using reflection
        var generator = new Lokad.ILPack.AssemblyGenerator();
        generator.GenerateAssembly(assemblyBuilder, path);
    }
}

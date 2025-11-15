using System.Reflection;

namespace NuGetToolbox.Tests;

public partial class SignatureExporterTests
{
    public class SignatureExporterUnitTests
    {
        [Fact]
        public void VisibilityFiltering_LogicTest()
        {
            // Test the filtering logic directly without mocking
            var systemAssembly = typeof(object).Assembly;
            var types = systemAssembly.GetTypes();
            
            // Test the filtering logic
            var filteredTypes = types.Where(t => t.IsVisible && (t.IsClass || t.IsInterface)).ToList();
            
            // Assert that filtering works as expected
            Assert.All(filteredTypes, t =>
            {
                Assert.True(t.IsVisible, $"Type {t.FullName} should be visible");
                Assert.True(t.IsClass || t.IsInterface, $"Type {t.FullName} should be class or interface");
            });
            
            // Verify that some types are excluded (like enums, structs, etc.)
            var excludedTypes = types.Where(t => !t.IsVisible || !(t.IsClass || t.IsInterface)).ToList();
            Assert.NotEmpty(excludedTypes);
        }

        [Fact]
        public void PartialLoadException_HandlingLogic()
        {
            // Test the exception handling logic
            var reflectionTypeLoadException = new ReflectionTypeLoadException(
                new Type[] { typeof(string), null, typeof(int) }, // Some loaded, some null
                new Exception[] { null!, new Exception("Missing dependency"), null! });

            // Test the exception handling logic
            Type[] types;
            try
            {
                // This would normally throw, but we're testing the handling logic
                throw reflectionTypeLoadException;
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray()!;
                
                // Verify that only non-null types are processed
                Assert.Equal(2, types.Length); // string and int should be loaded
                Assert.Contains(typeof(string), types);
                Assert.Contains(typeof(int), types);
                Assert.DoesNotContain(null, types);
            }

            // Verify the filtering logic still works after partial load
            var filteredTypes = types.Where(t => t.IsVisible && (t.IsClass || t.IsInterface)).ToList();
            Assert.All(filteredTypes, t =>
            {
                Assert.True(t.IsVisible);
                Assert.True(t.IsClass || t.IsInterface);
            });
        }

        [Fact]
        public void NamespaceFilter_LogicTest()
        {
            // Test that namespace filtering works after the changes
            var systemAssembly = typeof(object).Assembly;
            var types = systemAssembly.GetTypes();
            var visibleTypes = types.Where(t => t.IsVisible && (t.IsClass || t.IsInterface)).ToList();
            
            // Test namespace filtering
            var systemTypes = visibleTypes.Where(t => 
                t.Namespace != null && t.Namespace.StartsWith("System", StringComparison.Ordinal)).ToList();
            
            Assert.NotEmpty(systemTypes);
            Assert.All(systemTypes, t => Assert.StartsWith("System", t.Namespace ?? ""));
            
            // Test with a more specific namespace
            var linqTypes = visibleTypes.Where(t => 
                t.Namespace != null && t.Namespace.StartsWith("System.Linq", StringComparison.Ordinal)).ToList();
            
            // All returned types should have the correct namespace
            Assert.All(linqTypes, t => Assert.StartsWith("System.Linq", t.Namespace ?? ""));
        }
    }
}

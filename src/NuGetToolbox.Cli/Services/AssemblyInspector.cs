using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace NuGetToolbox.Cli.Services;

/// <summary>
/// Inspects assemblies using MetadataLoadContext for safe metadata-only inspection.
/// Never loads code into the default context.
/// </summary>
public class AssemblyInspector
{
    private readonly ILogger<AssemblyInspector> _logger;

    public AssemblyInspector(ILogger<AssemblyInspector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts public types from assembly files.
    /// </summary>
    public List<Models.TypeInfo> ExtractPublicTypes(string assemblyPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Extracting public types from {AssemblyPath}", assemblyPath);

        var runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
        var paths = new List<string>(runtimeAssemblies) { assemblyPath };
        var resolver = new PathAssemblyResolver(paths);

        using var metadataContext = new MetadataLoadContext(resolver);

        try
        {
            var assembly = metadataContext.LoadFromAssemblyPath(assemblyPath);
            var types = new List<Models.TypeInfo>();

            Type[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogDebug("ReflectionTypeLoadException encountered. Using partially-loaded types from {AssemblyPath}", assemblyPath);

                foreach (var loaderException in ex.LoaderExceptions.Where(e => e != null).Distinct())
                {
                    _logger.LogDebug("Loader exception: {ExceptionMessage}", loaderException!.Message);
                }

                assemblyTypes = ex.Types.Where(t => t != null).ToArray()!;
            }

            foreach (var type in assemblyTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (!type.IsPublic)
                        continue;

                    var kind = GetTypeKind(type);
                    if (kind == null)
                        continue;

                    types.Add(new Models.TypeInfo
                    {
                        Namespace = type.Namespace ?? string.Empty,
                        Name = type.Name,
                        Kind = kind
                    });
                }
                catch (TypeLoadException ex)
                {
                    _logger.LogDebug("Failed to load type {TypeName}: {Message}", type?.FullName ?? "unknown", ex.Message);
                }
                catch (FileNotFoundException ex)
                {
                    _logger.LogDebug("Missing dependency for type {TypeName}: {Message}", type?.FullName ?? "unknown", ex.Message);
                }
                catch (FileLoadException ex)
                {
                    _logger.LogDebug("Failed to load file for type {TypeName}: {Message}", type?.FullName ?? "unknown", ex.Message);
                }
                catch (NotSupportedException ex)
                {
                    _logger.LogDebug("Type not supported {TypeName}: {Message}", type?.FullName ?? "unknown", ex.Message);
                }
            }

            _logger.LogInformation("Extracted {Count} public types from {AssemblyPath}", types.Count, assemblyPath);
            return types;
        }
        catch (BadImageFormatException ex)
        {
            throw new InvalidOperationException($"Invalid assembly format: {assemblyPath}", ex);
        }
    }

    /// <summary>
    /// Extracts public types from multiple assembly files.
    /// </summary>
    public List<Models.TypeInfo> ExtractPublicTypesFromMultiple(string[] assemblyPaths, CancellationToken cancellationToken = default)
    {
        var allTypes = new List<Models.TypeInfo>();

        foreach (var path in assemblyPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var types = ExtractPublicTypes(path, cancellationToken);
            allTypes.AddRange(types);
        }

        return allTypes;
    }

    /// <summary>
    /// Gets all public members (methods, properties, etc.) from a type.
    /// </summary>
    public IEnumerable<MemberInfo> GetPublicMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        var methods = type.GetMethods(flags);
        var properties = type.GetProperties(flags);
        var fields = type.GetFields(flags);

        return methods.Cast<MemberInfo>()
            .Concat(properties)
            .Concat(fields)
            .Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"));
    }

    private static string? GetTypeKind(Type type)
    {
        if (type.IsClass)
            return "class";
        if (type.IsInterface)
            return "interface";
        if (type.IsValueType && !type.IsEnum)
            return "struct";
        if (type.IsEnum)
            return "enum";

        return null;
    }
}

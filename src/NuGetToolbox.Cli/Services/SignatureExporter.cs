using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace NuGetToolbox.Cli.Services;

/// <summary>
/// Exports method signatures with documentation using Roslyn symbol display.
/// </summary>
public class SignatureExporter
{
    private readonly AssemblyInspector _assemblyInspector;
    private readonly XmlDocumentationProvider _xmlDocProvider;
    private readonly ILogger<SignatureExporter> _logger;

    private static readonly SymbolDisplayFormat SignatureFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeModifiers,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public SignatureExporter(
        AssemblyInspector assemblyInspector,
        XmlDocumentationProvider xmlDocProvider,
        ILogger<SignatureExporter> logger)
    {
        _assemblyInspector = assemblyInspector;
        _xmlDocProvider = xmlDocProvider;
        _logger = logger;
    }

    /// <summary>
    /// Exports methods from assemblies with optional namespace filtering.
    /// </summary>
    public List<Models.MethodInfo> ExportMethods(IEnumerable<string> assemblyPaths, string? namespaceFilter = null)
    {
        var methods = new List<Models.MethodInfo>();
        var pathsArray = assemblyPaths.ToArray();

        if (pathsArray.Length == 0)
        {
            _logger.LogWarning("No assembly paths provided");
            return methods;
        }

        var resolver = new PathAssemblyResolver(pathsArray.Concat(GetRuntimeAssemblies()));
        using var mlc = new MetadataLoadContext(resolver);

        foreach (var assemblyPath in pathsArray)
        {
            try
            {
                var assembly = mlc.LoadFromAssemblyPath(assemblyPath);
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

                if (File.Exists(xmlPath))
                {
                    _xmlDocProvider.LoadDocumentation(xmlPath);
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray()!;
                    _logger.LogWarning("Partial type load in {Assembly}: {LoadedCount}/{TotalCount} types loaded", 
                        assemblyPath, types.Length, ex.Types.Length);
                }

                var visibleTypes = types
                    .Where(t => t.IsVisible && (t.IsClass || t.IsInterface));

                if (namespaceFilter != null)
                {
                    visibleTypes = visibleTypes.Where(t =>
                        t.Namespace != null && t.Namespace.StartsWith(namespaceFilter, StringComparison.Ordinal));
                }

                foreach (var type in visibleTypes)
                {
                    // Use DeclaredOnly to include only methods directly declared on this type
                    // This excludes inherited interface methods for consistency and performance
                    var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .Where(m => !m.IsSpecialName);

                    foreach (var method in publicMethods)
                    {
                        methods.Add(CreateMethodInfo(method, type));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process assembly: {AssemblyPath}", assemblyPath);
            }
        }

        _logger.LogInformation("Exported {Count} methods", methods.Count);
        return methods;
    }

    /// <summary>
    /// Exports methods to JSON format.
    /// </summary>
    public string ExportToJson(List<Models.MethodInfo> methods)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(methods, options);
    }

    /// <summary>
    /// Exports methods to JSONL (JSON Lines) format.
    /// </summary>
    public string ExportToJsonL(List<Models.MethodInfo> methods)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var lines = methods.Select(m => JsonSerializer.Serialize(m, options));
        return string.Join('\n', lines);
    }

    private Models.MethodInfo CreateMethodInfo(MethodInfo method, Type declaringType)
    {
        var signature = BuildSignature(method);
        var docId = GetDocumentationCommentId(method);

        var parameters = method.GetParameters().Select(p => new Models.ParameterInfo
        {
            Name = p.Name ?? "unknown",
            Type = GetFullTypeName(p.ParameterType)
        }).ToList();

        var methodInfo = new Models.MethodInfo
        {
            Type = declaringType.FullName ?? declaringType.Name,
            Method = method.Name,
            Signature = signature,
            Summary = _xmlDocProvider.GetSummary(docId),
            Params = _xmlDocProvider.GetParameters(docId),
            Returns = _xmlDocProvider.GetReturns(docId),
            Parameters = parameters,
            ReturnType = GetFullTypeName(method.ReturnType)
        };

        return methodInfo;
    }

    private static string BuildSignature(MethodInfo method)
    {
        var returnType = method.ReturnType.IsGenericType
            ? FormatGenericType(method.ReturnType)
            : method.ReturnType.Name;

        var parameters = string.Join(", ", method.GetParameters().Select(p =>
        {
            var prefix = p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : "";
            var paramType = p.ParameterType.IsByRef
                ? (p.ParameterType.GetElementType()?.Name ?? "unknown")
                : (p.ParameterType.IsGenericType ? FormatGenericType(p.ParameterType) : p.ParameterType.Name);
            return $"{prefix}{paramType} {p.Name}";
        }));

        var genericParams = method.IsGenericMethod
            ? $"<{string.Join(", ", method.GetGenericArguments().Select(t => t.Name))}>"
            : "";

        var modifiers = method.IsStatic ? "static " : "";
        var accessibility = method.IsPublic ? "public " : method.IsFamily ? "protected " : "";

        return $"{accessibility}{modifiers}{returnType} {method.Name}{genericParams}({parameters})";
    }

    private static string FormatGenericType(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        var genericTypeName = type.Name.Contains('`')
            ? type.Name.Substring(0, type.Name.IndexOf('`'))
            : type.Name;

        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(t =>
            t.IsGenericType ? FormatGenericType(t) : t.Name));

        return $"{genericTypeName}<{genericArgs}>";
    }

    private static string GetDocumentationCommentId(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        var typeFullName = declaringType?.FullName?.Replace('+', '.');

        var parameters = method.GetParameters();
        var paramList = parameters.Length > 0
            ? $"({string.Join(",", parameters.Select(p => GetParameterTypeString(p.ParameterType)))})"
            : string.Empty;

        var genericSuffix = method.IsGenericMethod
            ? $"``{method.GetGenericArguments().Length}"
            : string.Empty;

        var methodName = method.Name;
        if (method.IsConstructor)
        {
            methodName = method.IsStatic ? "#cctor" : "#ctor";
        }

        return $"M:{typeFullName}.{methodName}{genericSuffix}{paramList}";
    }

    private static string GetParameterTypeString(Type type)
    {
        if (type.IsByRef)
        {
            var elementType = type.GetElementType();
            return GetParameterTypeString(elementType!) + "@";
        }

        if (type.IsGenericParameter)
        {
            if (type.DeclaringMethod != null)
                return $"``{type.GenericParameterPosition}";
            return $"`{type.GenericParameterPosition}";
        }

        if (type.IsGenericType)
        {
            var genericTypeName = type.GetGenericTypeDefinition().FullName?.Replace('+', '.');
            var args = type.GetGenericArguments();
            var argStrings = string.Join(",", args.Select(GetParameterTypeString));
            return $"{genericTypeName}{{{argStrings}}}";
        }

        return type.FullName?.Replace('+', '.') ?? type.Name;
    }

    private static string GetFullTypeName(Type type)
    {
        if (type.IsByRef)
        {
            var elementType = type.GetElementType();
            return GetFullTypeName(elementType!) + "&";
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            var rank = type.GetArrayRank();
            var brackets = rank == 1 ? "[]" : $"[{new string(',', rank - 1)}]";
            return GetFullTypeName(elementType!) + brackets;
        }

        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            var genericTypeName = genericTypeDef.FullName?.Replace('+', '.') ?? genericTypeDef.Name;
            var args = type.GetGenericArguments();
            var argStrings = string.Join(", ", args.Select(GetFullTypeName));

            if (genericTypeName.Contains('`'))
                genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));

            return $"{genericTypeName}<{argStrings}>";
        }

        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        return type.FullName?.Replace('+', '.') ?? type.Name;
    }

    private static IEnumerable<string> GetRuntimeAssemblies()
    {
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (runtimePath == null)
            return Array.Empty<string>();

        return Directory.GetFiles(runtimePath, "*.dll");
    }
}

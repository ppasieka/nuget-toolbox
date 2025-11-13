using Microsoft.Extensions.Logging;
using NuGetToolbox.Cli.Models;

namespace NuGetToolbox.Cli.Services;

/// <summary>
/// Exports method signatures in idiomatic C# using Roslyn symbol display.
/// </summary>
public class SignatureExporter
{
    private readonly ILogger<SignatureExporter> _logger;
    private readonly AssemblyInspector _assemblyInspector;
    private readonly XmlDocumentationProvider _documentationProvider;

    public SignatureExporter(
        ILogger<SignatureExporter> logger,
        AssemblyInspector assemblyInspector,
        XmlDocumentationProvider documentationProvider)
    {
        _logger = logger;
        _assemblyInspector = assemblyInspector;
        _documentationProvider = documentationProvider;
    }

    /// <summary>
    /// Exports public methods from assembly files with signatures and documentation.
    /// </summary>
    public List<MethodInfo> ExportMethods(params string[] assemblyPaths)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Exports methods to JSON format.
    /// </summary>
    public string ExportToJson(List<MethodInfo> methods)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Exports methods to JSONL format (one per line).
    /// </summary>
    public string ExportToJsonL(List<MethodInfo> methods)
    {
        throw new NotImplementedException();
    }
}

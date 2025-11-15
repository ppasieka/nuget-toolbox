namespace NuGetToolbox.Tests;

/// <summary>
/// Helper utility for locating and invoking the built CLI in E2E tests.
/// </summary>
public static class CliHelper
{
    /// <summary>
    /// Locates the built NuGetToolbox.Cli.dll in the Debug output directory.
    /// </summary>
    /// <returns>Full path to the CLI DLL.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the CLI DLL cannot be found.</exception>
    public static string GetCliPath()
    {
        var solutionDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var cliPath = Path.Combine(
            solutionDir, "src", "NuGetToolbox.Cli", "bin", "Debug", "net8.0", "NuGetToolbox.Cli.dll");

        if (!File.Exists(cliPath))
        {
            throw new FileNotFoundException(
                $"CLI DLL not found at {cliPath}. Please build the project first.",
                cliPath);
        }

        return cliPath;
    }
}

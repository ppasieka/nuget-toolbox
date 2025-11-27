namespace NuGetToolbox.Cli;

public static class ExitCodes
{
    public const int Success = 0;
    public const int NotFound = 1;
    public const int TfmMismatch = 2;
    public const int InvalidOptions = 3;
    public const int NetworkError = 4;
    public const int Error = 5;
}

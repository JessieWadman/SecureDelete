using System.IO;

namespace SecureDelete;

/// <summary>
/// Finds Microsoft Sysinternals SDelete. SDelete is a separate Microsoft dependency and is never
/// bundled or downloaded, so discovery is best-effort in a sensible order:
///   1. sdelete64.exe / sdelete.exe next to SecureDelete.exe
///   2. sdelete64.exe / sdelete.exe on PATH
/// (A configured location would slot in between the two, if configuration is ever added.)
/// </summary>
public sealed class SDeleteLocator
{
    static readonly string[] ExecutableNames = ["sdelete64.exe", "sdelete.exe"];

    readonly string _appDirectory;
    readonly string? _pathEnvironment;

    public SDeleteLocator(string appDirectory, string? pathEnvironment)
    {
        _appDirectory = appDirectory;
        _pathEnvironment = pathEnvironment;
    }

    public static SDeleteLocator Default() =>
        new(AppContext.BaseDirectory, Environment.GetEnvironmentVariable("PATH"));

    /// <summary>Returns the full path to SDelete, or <c>null</c> if it cannot be found.</summary>
    public string? Locate()
    {
        foreach (string name in ExecutableNames)
        {
            string candidate = Path.Combine(_appDirectory, name);
            if (File.Exists(candidate))
                return candidate;
        }

        if (!string.IsNullOrEmpty(_pathEnvironment))
        {
            foreach (string dir in _pathEnvironment.Split(
                         Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                foreach (string name in ExecutableNames)
                {
                    string candidate;
                    try
                    {
                        candidate = Path.Combine(dir, name);
                    }
                    catch (ArgumentException)
                    {
                        // A malformed PATH entry (illegal characters); skip it.
                        continue;
                    }

                    if (File.Exists(candidate))
                        return candidate;
                }
            }
        }

        return null;
    }
}

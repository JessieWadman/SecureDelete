namespace SecureDelete;

/// <summary>Outcome of one SDelete invocation.</summary>
public sealed class SDeleteResult
{
    public required bool Success { get; init; }

    /// <summary>SDelete's exit code, or -1 if the process never produced one.</summary>
    public required int ExitCode { get; init; }

    public string StandardOutput { get; init; } = "";

    public string StandardError { get; init; } = "";

    /// <summary>True when the run was aborted (the child process was terminated) rather than completing.</summary>
    public bool Canceled { get; init; }
}

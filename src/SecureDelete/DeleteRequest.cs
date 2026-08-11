namespace SecureDelete;

/// <summary>Whether the validated target is a file or a directory.</summary>
public enum TargetKind
{
    File,
    Directory,
}

/// <summary>
/// A validated, ready-to-execute secure-delete request. Instances are only produced
/// by <see cref="TargetInspector"/>, so by construction the path exists, is not a
/// reparse point, and the pass count is one of the allowed values.
/// </summary>
public sealed record DeleteRequest
{
    /// <summary>Fully qualified target path.</summary>
    public required string Path { get; init; }

    public required TargetKind Kind { get; init; }

    /// <summary>Number of overwrite passes (1, 3, 7, or 10).</summary>
    public int Passes { get; init; } = 1;

    /// <summary>True when the target lives on a UNC / network share.</summary>
    public bool IsUnc { get; init; }
}

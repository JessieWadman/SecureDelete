namespace SecureDelete;

/// <summary>What the process was asked to do.</summary>
public enum CommandMode
{
    /// <summary>Securely delete a single target.</summary>
    Delete,

    /// <summary>Show usage information.</summary>
    Help,

    /// <summary>Arguments could not be understood.</summary>
    Invalid,
}

/// <summary>
/// Parsed command line. SecureDelete is invoked by Explorer as
/// <c>SecureDelete.exe --delete "&lt;path&gt;"</c> with exactly one target.
/// </summary>
public sealed class CommandLineOptions
{
    public CommandMode Mode { get; init; }

    /// <summary>The single target path, when <see cref="Mode"/> is <see cref="CommandMode.Delete"/>.</summary>
    public string? Target { get; init; }

    /// <summary>Human-readable reason the arguments were rejected, when invalid.</summary>
    public string? Error { get; init; }

    public static CommandLineOptions Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return new CommandLineOptions { Mode = CommandMode.Help };

        string first = args[0];

        if (first is "--help" or "-h" or "/?" or "/help")
            return new CommandLineOptions { Mode = CommandMode.Help };

        if (first is "--delete" or "-d")
        {
            if (args.Count < 2)
                return new CommandLineOptions
                {
                    Mode = CommandMode.Invalid,
                    Error = "No target was specified. Usage: SecureDelete.exe --delete \"<file or folder>\"",
                };

            if (args.Count > 2)
                return new CommandLineOptions
                {
                    Mode = CommandMode.Invalid,
                    Error = "Exactly one target may be deleted per invocation.",
                };

            string target = args[1];
            if (string.IsNullOrWhiteSpace(target))
                return new CommandLineOptions
                {
                    Mode = CommandMode.Invalid,
                    Error = "The target path is empty.",
                };

            return new CommandLineOptions { Mode = CommandMode.Delete, Target = target };
        }

        return new CommandLineOptions
        {
            Mode = CommandMode.Invalid,
            Error = $"Unrecognized argument: {first}",
        };
    }
}

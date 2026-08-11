using System.IO;

namespace SecureDelete;

/// <summary>Raised when a target cannot be safely accepted for deletion.</summary>
public sealed class TargetValidationException(string message) : Exception(message);

/// <summary>
/// Turns a raw path plus a requested pass count into a validated <see cref="DeleteRequest"/>,
/// or throws <see cref="TargetValidationException"/> with a user-facing explanation.
///
/// This is where "fail closed" lives: missing/nonexistent targets, invalid pass counts, and
/// reparse points (symlinks, junctions, mount points) are all rejected before SDelete runs.
/// </summary>
public static class TargetInspector
{
    /// <summary>The only overwrite-pass values the UI offers and the runner accepts.</summary>
    public static readonly int[] AllowedPasses = [1, 3, 7, 10];

    public static DeleteRequest Inspect(string? rawPath, int passes)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            throw new TargetValidationException("No target was specified.");

        ValidatePasses(passes);

        string full;
        try
        {
            full = Path.GetFullPath(rawPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new TargetValidationException("The target path is not valid:\n\n" + rawPath);
        }

        bool fileExists = File.Exists(full);
        bool dirExists = Directory.Exists(full);

        if (!fileExists && !dirExists)
            throw new TargetValidationException(
                "The target no longer exists. It may already have been moved or deleted:\n\n" + full);

        TargetKind kind = dirExists ? TargetKind.Directory : TargetKind.File;

        FileAttributes attrs;
        try
        {
            attrs = File.GetAttributes(full);
        }
        catch (UnauthorizedAccessException)
        {
            throw new TargetValidationException("Access is denied to the target:\n\n" + full);
        }

        // A reparse point on the target itself is ambiguous: deleting it could either remove
        // just the link or recurse into whatever it points at. Refuse rather than guess.
        if (attrs.HasFlag(FileAttributes.ReparsePoint))
            throw new TargetValidationException(
                "The target is a reparse point (symbolic link, junction, or mount point):\n\n" +
                full + "\n\n" +
                "SecureDelete will not follow it, because that could destroy data stored in another location. " +
                "Handle the link manually if you are certain.");

        // For a recursive directory delete, refuse if any descendant is a reparse point so that
        // SDelete's recursion cannot escape the visible tree.
        if (kind == TargetKind.Directory)
            EnsureNoReparsePointsBelow(full);

        return new DeleteRequest
        {
            Path = full,
            Kind = kind,
            Passes = passes,
            IsUnc = IsUncPath(full),
        };
    }

    public static void ValidatePasses(int passes)
    {
        if (Array.IndexOf(AllowedPasses, passes) < 0)
            throw new TargetValidationException(
                "Overwrite pass count must be one of: " + string.Join(", ", AllowedPasses) + ".");
    }

    public static bool IsUncPath(string fullPath) =>
        fullPath.StartsWith(@"\\", StringComparison.Ordinal) &&
        !fullPath.StartsWith(@"\\?\", StringComparison.Ordinal) &&
        !fullPath.StartsWith(@"\\.\", StringComparison.Ordinal);

    /// <summary>
    /// Walks the tree without ever following a reparse point. Throws on the first junction /
    /// symlink / mount point found beneath <paramref name="root"/>, so recursion cannot escape.
    /// </summary>
    static void EnsureNoReparsePointsBelow(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            string dir = stack.Pop();

            IEnumerable<string> subdirs;
            try
            {
                subdirs = Directory.EnumerateDirectories(dir);
            }
            catch (UnauthorizedAccessException)
            {
                throw new TargetValidationException(
                    "Access was denied while inspecting this folder for links before deletion:\n\n" + dir);
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }

            foreach (string sub in subdirs)
            {
                FileAttributes a;
                try
                {
                    a = File.GetAttributes(sub);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or FileNotFoundException or DirectoryNotFoundException)
                {
                    // Cannot read the attributes, so cannot prove it is safe: fail closed.
                    throw new TargetValidationException(
                        "A folder inside the target could not be inspected for links, so deletion was stopped:\n\n" + sub);
                }

                if (a.HasFlag(FileAttributes.ReparsePoint))
                    throw new TargetValidationException(
                        "This folder contains a reparse point (junction, symbolic link, or mount point):\n\n" +
                        sub + "\n\n" +
                        "SecureDelete will not recursively delete through it, because that could destroy data in " +
                        "another location. Remove or relocate the linked item, then try again.");

                stack.Push(sub);
            }
        }
    }
}

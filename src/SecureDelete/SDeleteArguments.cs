using System.Globalization;

namespace SecureDelete;

/// <summary>
/// Builds the SDelete argument vector for a request. Kept separate from process launching so it
/// can be unit-tested without deleting anything.
///
/// Reference (Microsoft Sysinternals, verified 2026):
///     sdelete [-p passes] [-r] [-s] [-q] [-f] &lt;file or directory [...]&gt;
///       -p N   number of overwrite passes (default 1)
///       -r     remove the Read-Only attribute
///       -s     recurse subdirectories
///       -q     quiet
///     -accepteula / -nobanner suppress the first-run EULA dialog and startup banner so the
///     process is fully non-interactive.
/// </summary>
public static class SDeleteArguments
{
    public static IReadOnlyList<string> Build(DeleteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        TargetInspector.ValidatePasses(request.Passes);

        var args = new List<string>
        {
            "-accepteula", // never block on the first-run EULA dialog
            "-nobanner",   // keep captured output clean
            "-q",          // quiet / non-interactive
            "-r",          // clear Read-Only so read-only targets can be removed
        };

        if (request.Kind == TargetKind.Directory)
            args.Add("-s"); // recurse subdirectories

        args.Add("-p");
        args.Add(request.Passes.ToString(CultureInfo.InvariantCulture));

        // Exactly one target, passed as its own argument (never concatenated into a command line).
        args.Add(request.Path);

        return args;
    }
}

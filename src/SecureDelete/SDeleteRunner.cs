using System.Diagnostics;
using System.Text;

namespace SecureDelete;

/// <summary>Launches SDelete for a request. An interface so the UI can be tested with a fake.</summary>
public interface ISDeleteRunner
{
    Task<SDeleteResult> RunAsync(DeleteRequest request, IProgress<string>? output, CancellationToken cancellationToken);
}

/// <summary>
/// Runs SDelete out-of-process with output redirected and captured asynchronously. User-controlled
/// text is only ever passed through <see cref="ProcessStartInfo.ArgumentList"/> — never a shell.
/// </summary>
public sealed class SDeleteRunner(string sdeletePath) : ISDeleteRunner
{
    readonly string _sdeletePath = sdeletePath;

    public Task<SDeleteResult> RunAsync(DeleteRequest request, IProgress<string>? output, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var psi = new ProcessStartInfo
        {
            FileName = _sdeletePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (string arg in SDeleteArguments.Build(request))
            psi.ArgumentList.Add(arg);

        return RunCoreAsync(psi, output, cancellationToken);
    }

    /// <summary>
    /// The launch / capture / interpret core, independent of SDelete argument construction so it can
    /// be exercised in tests against a harmless stub process.
    /// </summary>
    internal static async Task<SDeleteResult> RunCoreAsync(
        ProcessStartInfo psi, IProgress<string>? output, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;
            stdout.AppendLine(e.Data);
            if (!string.IsNullOrWhiteSpace(e.Data))
                output?.Report(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stderr.AppendLine(e.Data);
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new SDeleteResult
            {
                Success = false,
                ExitCode = -1,
                StandardError = "SDelete could not be started:\n" + ex.Message,
            };
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        bool canceled = false;
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            canceled = true;
            TryKillTree(process);
            // Wait for the terminated process so redirected buffers flush before we read them.
            try
            {
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Nothing more we can do; fall through and report what we have.
            }
        }

        bool exited = process.HasExited;
        int exitCode = exited ? process.ExitCode : -1;

        return new SDeleteResult
        {
            Success = !canceled && exited && exitCode == 0,
            ExitCode = exitCode,
            StandardOutput = stdout.ToString(),
            StandardError = stderr.ToString(),
            Canceled = canceled,
        };
    }

    static void TryKillTree(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Process may have exited between the check and the kill; ignore.
        }
    }
}

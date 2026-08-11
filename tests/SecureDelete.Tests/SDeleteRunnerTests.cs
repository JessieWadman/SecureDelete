using System.Diagnostics;
using SecureDelete;
using Xunit;

namespace SecureDelete.Tests;

/// <summary>
/// Exercises the process launch / capture / interpretation core against cmd.exe, so exit-code and
/// stdout/stderr handling are verified without running SDelete or deleting anything.
/// </summary>
public class SDeleteRunnerTests
{
    static ProcessStartInfo Cmd(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("/c");
        psi.ArgumentList.Add(command);
        return psi;
    }

    [Fact]
    public async Task ZeroExit_IsSuccess_AndCapturesStdout()
    {
        var result = await SDeleteRunner.RunCoreAsync(Cmd("echo hello-out& exit 0"), null, CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello-out", result.StandardOutput);
    }

    [Fact]
    public async Task NonZeroExit_IsFailure_WithExitCode()
    {
        var result = await SDeleteRunner.RunCoreAsync(Cmd("exit 5"), null, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Equal(5, result.ExitCode);
        Assert.False(result.Canceled);
    }

    [Fact]
    public async Task Stderr_IsCaptured()
    {
        var result = await SDeleteRunner.RunCoreAsync(Cmd("echo boom 1>&2& exit 1"), null, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Contains("boom", result.StandardError);
    }

    [Fact]
    public async Task Progress_ReceivesOutputLines()
    {
        var lines = new List<string>();
        var progress = new Progress<string>(l => lines.Add(l));
        // Give the Progress callback time to flush on the captured context.
        await SDeleteRunner.RunCoreAsync(Cmd("echo line-one& exit 0"), progress, CancellationToken.None);
        // Progress<T> posts asynchronously; allow it to drain.
        await Task.Delay(100);
        Assert.Contains(lines, l => l.Contains("line-one"));
    }

    [Fact]
    public async Task FailureToStart_IsReportedNotThrown()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "this-executable-does-not-exist-xyz.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        var result = await SDeleteRunner.RunCoreAsync(psi, null, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("could not be started", result.StandardError);
    }

    [Fact]
    public async Task Cancellation_AbortsAndReportsCanceled()
    {
        using var cts = new CancellationTokenSource();
        // Sleep ~10s via ping; cancel shortly after start.
        var task = SDeleteRunner.RunCoreAsync(Cmd("ping -n 10 127.0.0.1 >nul"), null, cts.Token);
        cts.CancelAfter(300);
        var result = await task;

        Assert.True(result.Canceled);
        Assert.False(result.Success);
    }
}

using System.IO;
using SecureDelete;
using Xunit;

namespace SecureDelete.Tests;

public sealed class SDeleteLocatorTests : IDisposable
{
    readonly string _root;

    public SDeleteLocatorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "sd-loc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    [Fact]
    public void FindsSdelete64_BesideExecutable_First()
    {
        var appDir = Path.Combine(_root, "app");
        Directory.CreateDirectory(appDir);
        var beside = Path.Combine(appDir, "sdelete64.exe");
        File.WriteAllText(beside, "");

        var locator = new SDeleteLocator(appDir, pathEnvironment: null);
        Assert.Equal(beside, locator.Locate());
    }

    [Fact]
    public void FallsBackToSdeleteExe_BesideExecutable()
    {
        var appDir = Path.Combine(_root, "app2");
        Directory.CreateDirectory(appDir);
        var beside = Path.Combine(appDir, "sdelete.exe");
        File.WriteAllText(beside, "");

        var locator = new SDeleteLocator(appDir, pathEnvironment: null);
        Assert.Equal(beside, locator.Locate());
    }

    [Fact]
    public void FindsSdelete_OnPath_WhenNotBesideExecutable()
    {
        var appDir = Path.Combine(_root, "app3");
        var pathDir = Path.Combine(_root, "onpath");
        Directory.CreateDirectory(appDir);
        Directory.CreateDirectory(pathDir);
        var onPath = Path.Combine(pathDir, "sdelete64.exe");
        File.WriteAllText(onPath, "");

        var locator = new SDeleteLocator(appDir, pathDir);
        Assert.Equal(onPath, locator.Locate());
    }

    [Fact]
    public void ReturnsNull_WhenNotFoundAnywhere()
    {
        var appDir = Path.Combine(_root, "empty");
        Directory.CreateDirectory(appDir);
        var locator = new SDeleteLocator(appDir, pathEnvironment: @"C:\definitely\not\here;");
        Assert.Null(locator.Locate());
    }

    [Fact]
    public void BesideExecutable_TakesPriorityOverPath()
    {
        var appDir = Path.Combine(_root, "prio-app");
        var pathDir = Path.Combine(_root, "prio-path");
        Directory.CreateDirectory(appDir);
        Directory.CreateDirectory(pathDir);
        var beside = Path.Combine(appDir, "sdelete64.exe");
        File.WriteAllText(beside, "");
        File.WriteAllText(Path.Combine(pathDir, "sdelete64.exe"), "");

        var locator = new SDeleteLocator(appDir, pathDir);
        Assert.Equal(beside, locator.Locate());
    }
}

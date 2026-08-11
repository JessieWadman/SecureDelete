using System.IO;
using SecureDelete;
using Xunit;

namespace SecureDelete.Tests;

public sealed class TargetInspectorTests : IDisposable
{
    readonly string _root;

    public TargetInspectorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "sd-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingTarget_Throws(string? path)
    {
        Assert.Throws<TargetValidationException>(() => TargetInspector.Inspect(path, 1));
    }

    [Fact]
    public void NonexistentTarget_Throws()
    {
        var path = Path.Combine(_root, "does-not-exist.txt");
        Assert.Throws<TargetValidationException>(() => TargetInspector.Inspect(path, 1));
    }

    [Fact]
    public void ExistingFile_IsClassifiedAsFile()
    {
        var path = Path.Combine(_root, "a file.txt");
        File.WriteAllText(path, "x");
        var req = TargetInspector.Inspect(path, 3);
        Assert.Equal(TargetKind.File, req.Kind);
        Assert.Equal(3, req.Passes);
        Assert.Equal(Path.GetFullPath(path), req.Path);
    }

    [Fact]
    public void ExistingDirectory_IsClassifiedAsDirectory()
    {
        var dir = Path.Combine(_root, "sub dir");
        Directory.CreateDirectory(dir);
        var req = TargetInspector.Inspect(dir, 1);
        Assert.Equal(TargetKind.Directory, req.Kind);
    }

    [Fact]
    public void UnicodePath_IsAccepted()
    {
        var path = Path.Combine(_root, "日本語— füür.txt");
        File.WriteAllText(path, "x");
        var req = TargetInspector.Inspect(path, 1);
        Assert.Equal(TargetKind.File, req.Kind);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(0)]
    [InlineData(5)]
    public void InvalidPassCount_Throws(int passes)
    {
        var path = Path.Combine(_root, "b.txt");
        File.WriteAllText(path, "x");
        Assert.Throws<TargetValidationException>(() => TargetInspector.Inspect(path, passes));
    }

    [Fact]
    public void DirectorySymlink_AsTarget_IsRejected()
    {
        var real = Path.Combine(_root, "real");
        Directory.CreateDirectory(real);
        var link = Path.Combine(_root, "link");

        try
        {
            Directory.CreateSymbolicLink(link, real);
        }
        catch (Exception)
        {
            // Creating symlinks may require privilege / developer mode; skip if unavailable.
            return;
        }

        Assert.Throws<TargetValidationException>(() => TargetInspector.Inspect(link, 1));
    }

    [Fact]
    public void DirectoryContainingSymlink_IsRejected()
    {
        var parent = Path.Combine(_root, "parent");
        var real = Path.Combine(_root, "target");
        Directory.CreateDirectory(parent);
        Directory.CreateDirectory(real);
        var link = Path.Combine(parent, "nested-link");

        try
        {
            Directory.CreateSymbolicLink(link, real);
        }
        catch (Exception)
        {
            return; // skip when symlink creation is not permitted
        }

        Assert.Throws<TargetValidationException>(() => TargetInspector.Inspect(parent, 1));
    }

    [Fact]
    public void PlainDirectoryTree_WithoutLinks_IsAccepted()
    {
        var parent = Path.Combine(_root, "tree");
        Directory.CreateDirectory(Path.Combine(parent, "a", "b"));
        File.WriteAllText(Path.Combine(parent, "a", "f.txt"), "x");
        var req = TargetInspector.Inspect(parent, 1);
        Assert.Equal(TargetKind.Directory, req.Kind);
    }

    [Fact]
    public void ValidatePasses_AcceptsAllowedValues()
    {
        foreach (var p in TargetInspector.AllowedPasses)
            TargetInspector.ValidatePasses(p); // must not throw
    }
}

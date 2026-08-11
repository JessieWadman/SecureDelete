using SecureDelete;
using Xunit;

namespace SecureDelete.Tests;

public class SDeleteArgumentsTests
{
    static DeleteRequest File(string path, int passes = 1) =>
        new() { Path = path, Kind = TargetKind.File, Passes = passes };

    static DeleteRequest Dir(string path, int passes = 1) =>
        new() { Path = path, Kind = TargetKind.Directory, Passes = passes };

    [Fact]
    public void File_HasQuietRemoveReadonlyAndNoRecurse()
    {
        var args = SDeleteArguments.Build(File(@"C:\a\b.txt"));
        Assert.Contains("-accepteula", args);
        Assert.Contains("-nobanner", args);
        Assert.Contains("-q", args);
        Assert.Contains("-r", args);
        Assert.DoesNotContain("-s", args);          // files are not recursed
    }

    [Fact]
    public void Directory_AddsRecurseFlag()
    {
        var args = SDeleteArguments.Build(Dir(@"C:\a\folder"));
        Assert.Contains("-s", args);
    }

    [Fact]
    public void Passes_AreEmittedAfterDashP()
    {
        var args = SDeleteArguments.Build(File(@"C:\a\b.txt", 7));
        int i = args.ToList().IndexOf("-p");
        Assert.True(i >= 0);
        Assert.Equal("7", args[i + 1]);
    }

    [Fact]
    public void Target_IsTheLastArgument_AndNotQuotedOrAltered()
    {
        var path = @"C:\a\b.txt";
        var args = SDeleteArguments.Build(File(path));
        Assert.Equal(path, args[^1]);
    }

    [Fact]
    public void Target_WithSpaces_IsASingleUnquotedArgument()
    {
        var path = @"C:\folder with spaces\my file.txt";
        var args = SDeleteArguments.Build(File(path));
        Assert.Equal(path, args[^1]);       // ArgumentList quoting is handled by the runtime, not us
        Assert.DoesNotContain("\"", args[^1]);
    }

    [Fact]
    public void Target_WithUnicode_IsPreservedExactly()
    {
        var path = @"C:\данные\файл— füür_日本語.txt";
        var args = SDeleteArguments.Build(File(path));
        Assert.Equal(path, args[^1]);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public void InvalidPassCount_Throws(int passes)
    {
        Assert.Throws<TargetValidationException>(() => SDeleteArguments.Build(File(@"C:\a.txt", passes)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(10)]
    public void ValidPassCounts_AreAccepted(int passes)
    {
        var args = SDeleteArguments.Build(File(@"C:\a.txt", passes));
        Assert.Equal(passes.ToString(), args[args.ToList().IndexOf("-p") + 1]);
    }
}

using SecureDelete;
using Xunit;

namespace SecureDelete.Tests;

public class CommandLineOptionsTests
{
    [Fact]
    public void NoArguments_IsHelp()
    {
        var o = CommandLineOptions.Parse([]);
        Assert.Equal(CommandMode.Help, o.Mode);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    public void HelpFlags_AreHelp(string flag)
    {
        var o = CommandLineOptions.Parse([flag]);
        Assert.Equal(CommandMode.Help, o.Mode);
    }

    [Theory]
    [InlineData("--delete")]
    [InlineData("-d")]
    public void Delete_WithTarget_IsDelete(string flag)
    {
        var o = CommandLineOptions.Parse([flag, @"C:\some\file.txt"]);
        Assert.Equal(CommandMode.Delete, o.Mode);
        Assert.Equal(@"C:\some\file.txt", o.Target);
    }

    [Fact]
    public void Delete_WithoutTarget_IsInvalid()
    {
        var o = CommandLineOptions.Parse(["--delete"]);
        Assert.Equal(CommandMode.Invalid, o.Mode);
        Assert.NotNull(o.Error);
    }

    [Fact]
    public void Delete_WithTwoTargets_IsInvalid()
    {
        var o = CommandLineOptions.Parse(["--delete", "a", "b"]);
        Assert.Equal(CommandMode.Invalid, o.Mode);
    }

    [Fact]
    public void Delete_WithEmptyTarget_IsInvalid()
    {
        var o = CommandLineOptions.Parse(["--delete", "   "]);
        Assert.Equal(CommandMode.Invalid, o.Mode);
    }

    [Fact]
    public void Target_WithSpaces_IsPreservedAsSingleArgument()
    {
        var o = CommandLineOptions.Parse(["--delete", @"C:\folder with spaces\my file.txt"]);
        Assert.Equal(CommandMode.Delete, o.Mode);
        Assert.Equal(@"C:\folder with spaces\my file.txt", o.Target);
    }

    [Fact]
    public void UnknownArgument_IsInvalid()
    {
        var o = CommandLineOptions.Parse(["--wat"]);
        Assert.Equal(CommandMode.Invalid, o.Mode);
    }
}

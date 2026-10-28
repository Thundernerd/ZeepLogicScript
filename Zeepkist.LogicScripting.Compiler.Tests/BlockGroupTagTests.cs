using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockGroupTagTests
{
    private static readonly ScriptId _script = new("script");

    [Fact]
    public void ABlockWithItsOwnTag_IsAMember()
    {
        Assert.Equal(_script, BlockGroupTag.GetScriptId(BlockGroupTag.Create(_script, "UID"), "UID"));
    }

    [Fact]
    public void APastedCopy_IsNoMember()
    {
        Assert.Null(BlockGroupTag.GetScriptId(BlockGroupTag.Create(_script, "UID"), "COPY"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("UID")]
    [InlineData(":UID")]
    public void AMissingOrBrokenTag_IsNoMember(string tag)
    {
        Assert.Null(BlockGroupTag.GetScriptId(tag, "UID"));
    }

    [Fact]
    public void AUidWithTheSeparator_StillMatches()
    {
        Assert.Equal(_script, BlockGroupTag.GetScriptId(BlockGroupTag.Create(_script, "a:b"), "a:b"));
    }
}

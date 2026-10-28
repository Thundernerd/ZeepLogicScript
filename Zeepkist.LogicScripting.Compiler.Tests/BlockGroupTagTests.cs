using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockGroupTagTests
{
    [Fact]
    public void ABlockWithItsOwnTag_IsAMember()
    {
        Assert.Equal("script", BlockGroupTag.GetScriptId(BlockGroupTag.Create("script", "UID"), "UID"));
    }

    [Fact]
    public void APastedCopy_IsNoMember()
    {
        Assert.Null(BlockGroupTag.GetScriptId(BlockGroupTag.Create("script", "UID"), "COPY"));
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
        Assert.Equal("script", BlockGroupTag.GetScriptId(BlockGroupTag.Create("script", "a:b"), "a:b"));
    }
}

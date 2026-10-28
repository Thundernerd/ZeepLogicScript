using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class ScriptNameTests
{
    [Theory]
    [InlineData("doors")]
    [InlineData("a")]
    [InlineData("Lamps and doors, part 2")]
    public void AnOrdinaryName_IsFine(string name)
    {
        Assert.Null(ScriptName.GetError(name, []));
    }

    [Fact]
    public void FortyCharacters_IsFine()
    {
        Assert.Null(ScriptName.GetError(new string('a', 40), []));
    }

    [Fact]
    public void FortyOneCharacters_IsAnError()
    {
        Assert.NotNull(ScriptName.GetError(new string('a', 41), []));
    }

    [Fact]
    public void AnEmptyName_IsAnError()
    {
        Assert.NotNull(ScriptName.GetError(string.Empty, []));
    }

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\tb")]
    public void AControlCharacter_IsAnError(string name)
    {
        Assert.NotNull(ScriptName.GetError(name, []));
    }

    [Fact]
    public void ANameThatOnlyDiffersInCase_IsAnError()
    {
        Assert.NotNull(ScriptName.GetError("DOORS", ["doors"]));
    }
}

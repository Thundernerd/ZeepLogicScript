using TNRD.Zeepkist.LogicScripting.UI;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class HeldEscapeTests
{
    [Fact]
    public void TheEventComesTheNextFrame_ReleasesTheHeldEscape()
    {
        var escape = new HeldEscape();
        Assert.True(escape.Hold(10));

        Assert.True(escape.Release(11));
        Assert.False(escape.Release(11));
    }

    [Fact]
    public void AnEventInTheSameFrame_IsAnEarlierPress()
    {
        var escape = new HeldEscape();
        escape.Hold(10);

        Assert.False(escape.Release(10));
        Assert.True(escape.Release(11));
    }

    [Fact]
    public void ASecondEscapeWhileOneIsHeld_IsNotHeld()
    {
        var escape = new HeldEscape();
        escape.Hold(10);

        Assert.False(escape.Hold(11));
        Assert.True(escape.Release(11));
        Assert.True(escape.Hold(12));
    }

    [Fact]
    public void TheEventNeverComes_ExpiresAFrameLate()
    {
        var escape = new HeldEscape();
        escape.Hold(10);

        Assert.False(escape.Expire(11));
        Assert.True(escape.Expire(12));
        Assert.False(escape.Release(12));
        Assert.True(escape.Hold(12));
    }

    [Fact]
    public void AReleasedEscape_DoesNotExpire()
    {
        var escape = new HeldEscape();
        escape.Hold(10);
        escape.Release(11);

        Assert.False(escape.Expire(12));
    }
}

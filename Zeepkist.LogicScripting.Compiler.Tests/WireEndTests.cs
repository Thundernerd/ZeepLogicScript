using System;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class WireEndTests
{
    [Fact]
    public void CompiledEnd_HasAnIndexButNoUid()
    {
        var end = WireEnd.Compiled(3);

        Assert.False(end.IsPlaced);
        Assert.Equal(3, end.Block);
        Assert.Throws<InvalidOperationException>(() => end.Uid);
    }

    [Fact]
    public void PlacedEnd_HasAUidButNoIndex()
    {
        var end = WireEnd.Placed("AbC12");

        Assert.True(end.IsPlaced);
        Assert.Equal("AbC12", end.Uid);
        Assert.Throws<InvalidOperationException>(() => end.Block);
    }
}

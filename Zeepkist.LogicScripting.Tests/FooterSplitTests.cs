using TNRD.Zeepkist.LogicScripting.UI;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class FooterSplitTests
{
    private const float MinListHeight = 60;

    [Fact]
    public void TheFooterFits_GetsAllItWants()
    {
        Assert.Equal(100, FooterSplit.GetFooterHeight(400, 100, MinListHeight));
    }

    [Fact]
    public void TheFooterDoesntFit_LeavesTheListItsMinimum()
    {
        Assert.Equal(90, FooterSplit.GetFooterHeight(150, 200, MinListHeight));
    }

    [Fact]
    public void TheMinimumDoesntFit_GetsNothing()
    {
        Assert.Equal(0, FooterSplit.GetFooterHeight(40, 100, MinListHeight));
    }
}

using TNRD.Zeepkist.LogicScripting.UI;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class FooterSplitTests
{
    private const float MinListHeight = 60;
    private const float MinRestHeight = 60;
    private const float MinWarningHeight = 30;

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

    [Fact]
    public void TheWarningFits_GetsAllItWants()
    {
        Assert.Equal(100, FooterSplit.GetWarningHeight(400, 100, MinRestHeight, MinWarningHeight));
    }

    [Fact]
    public void TheWarningDoesntFit_LeavesTheRestItsMinimum()
    {
        Assert.Equal(90, FooterSplit.GetWarningHeight(150, 200, MinRestHeight, MinWarningHeight));
    }

    [Fact]
    public void TheWarningsMinimumDoesntFit_StillGetsItsMinimum()
    {
        Assert.Equal(30, FooterSplit.GetWarningHeight(70, 200, MinRestHeight, MinWarningHeight));
    }

    [Fact]
    public void AShortWarning_TakesNoMoreThanItWants()
    {
        Assert.Equal(20, FooterSplit.GetWarningHeight(70, 20, MinRestHeight, MinWarningHeight));
    }
}

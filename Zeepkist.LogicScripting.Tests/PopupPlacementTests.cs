using TNRD.Zeepkist.LogicScripting.UI;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class PopupPlacementTests
{
    private const float Height = 100;
    private static readonly (float Bottom, float Top) _editor = (100, 400);
    private static readonly (float Bottom, float Top) _window = (0, 500);

    [Fact]
    public void TheFirstPlaceFitsTheEditor_ChoosesIt()
    {
        Assert.Equal(200, PopupPlacement.Choose(200, 250, Height, _editor, _window));
    }

    [Fact]
    public void OnlyTheSecondPlaceFitsTheEditor_ChoosesIt()
    {
        Assert.Equal(250, PopupPlacement.Choose(350, 250, Height, _editor, _window));
    }

    [Fact]
    public void NeitherFitsTheEditor_ChoosesTheFirstThatFitsTheWindow()
    {
        Assert.Equal(50, PopupPlacement.Choose(50, 350, Height, _editor, _window));
        Assert.Equal(350, PopupPlacement.Choose(450, 350, Height, _editor, _window));
    }

    [Fact]
    public void NeitherFitsTheWindow_ChoosesNone()
    {
        Assert.Null(PopupPlacement.Choose(-50, 450, Height, _editor, _window));
    }
}

using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class NameColumnTests
{
    [Fact]
    public void NamesThatFit_TakeAColumnAsWideAsTheLongest_PlusTheGap()
    {
        var column = NameColumn.Fit([new ReferenceRow("ab", "x"), new ReferenceRow("abcd", "y")], Measure, 2, 100);

        Assert.Equal(6, column.Width);
        Assert.Equal(4, column.GetNameWidth(1));
        Assert.True(column.Fits(0));
        Assert.True(column.Fits(1));
    }

    [Fact]
    public void ANameTooLongForItsShareOfTheWidth_DoesNotFit_AndLeavesTheColumnToTheOthers()
    {
        // 45 of 100 is the most the column takes
        var column = NameColumn.Fit([new ReferenceRow("ab", "x"), new ReferenceRow(new string('a', 44), "y")], Measure, 2, 100);

        Assert.Equal(4, column.Width);
        Assert.True(column.Fits(0));
        Assert.False(column.Fits(1));
    }

    [Fact]
    public void ANameThatFillsItsShareExactly_StillFits()
    {
        var column = NameColumn.Fit([new ReferenceRow(new string('a', 43), "x")], Measure, 2, 100);

        Assert.Equal(45, column.Width);
        Assert.True(column.Fits(0));
    }

    private static float Measure(string text)
    {
        return text.Length;
    }
}

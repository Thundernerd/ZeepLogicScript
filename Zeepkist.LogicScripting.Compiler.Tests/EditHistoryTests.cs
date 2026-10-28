using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class EditHistoryTests
{
    [Fact]
    public void TypingInARow_IsOneStep()
    {
        var history = new EditHistory();
        var text = Type(history, string.Empty, "abc", 0);

        Assert.Equal("abc", text.Text);
        Assert.True(history.TryUndo(out var undone));
        Assert.Equal(new TextSnapshot(string.Empty, 0), undone);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Redo_RestoresTheTextAfterTheStep()
    {
        var history = new EditHistory();
        Type(history, string.Empty, "abc", 0);
        history.TryUndo(out _);

        Assert.True(history.TryRedo(out var redone));
        Assert.Equal(new TextSnapshot("abc", 3), redone);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void APause_StartsANewStep()
    {
        var history = new EditHistory();
        var text = Type(history, string.Empty, "ab", 0);
        Type(history, text.Text, "c", EditHistory.GroupSeconds + 1);

        history.TryUndo(out var undone);

        Assert.Equal("ab", undone.Text);
    }

    [Fact]
    public void ALineBreak_StartsANewStep()
    {
        var history = new EditHistory();
        Type(history, string.Empty, "ab\ncd", 0);

        history.TryUndo(out var undone);

        Assert.Equal("ab", undone.Text);
    }

    [Fact]
    public void MovingTheCaret_StartsANewStep()
    {
        var history = new EditHistory();
        var text = Type(history, string.Empty, "ab", 0);
        history.BreakGroup();
        Type(history, text.Text, "c", 0);

        history.TryUndo(out var undone);

        Assert.Equal("ab", undone.Text);
    }

    [Fact]
    public void DeletingInARow_IsOneStep_SeparateFromTyping()
    {
        var history = new EditHistory();
        Type(history, string.Empty, "abc", 0);
        history.Record(new TextSnapshot("abc", 3), new TextSnapshot("ab", 2), 0);
        history.Record(new TextSnapshot("ab", 2), new TextSnapshot("a", 1), 0);

        history.TryUndo(out var undone);
        Assert.Equal(new TextSnapshot("abc", 3), undone);

        history.TryUndo(out undone);
        Assert.Equal(string.Empty, undone.Text);
    }

    [Fact]
    public void ReplacingASelection_IsAStepOfItsOwn()
    {
        var history = new EditHistory();
        Type(history, string.Empty, "abc", 0);
        history.Record(new TextSnapshot("abc", 3, -3), new TextSnapshot("x", 1), 0);
        Type(history, "x", "y", 0);

        history.TryUndo(out var undone);
        Assert.Equal("x", undone.Text);

        history.TryUndo(out undone);
        Assert.Equal(new TextSnapshot("abc", 3, -3), undone);
    }

    [Fact]
    public void AnInsertOfSeveralLines_IsExactlyOneStep_NotMergedWithTheTypingAround()
    {
        var history = new EditHistory();
        var text = Type(history, string.Empty, "a = 1\n", 0);
        var inserted = new TextSnapshot(text.Text + "lamp_1 = placed(\"L\")\nlamp_2 = placed(\"M\")", 47);
        history.Record(text, inserted, 0);
        Type(history, inserted.Text, "\nb", 0);

        history.TryUndo(out var undone);
        Assert.Equal(inserted, undone);

        history.TryUndo(out undone);
        Assert.Equal(text, undone);
    }

    [Fact]
    public void AnEditAfterUndo_DropsTheRedoSteps()
    {
        var history = new EditHistory();
        Type(history, string.Empty, "ab", 0);
        history.TryUndo(out _);
        Type(history, string.Empty, "x", 0);

        Assert.False(history.CanRedo);
    }

    [Fact]
    public void TypingAfterUndo_DoesNotMergeIntoTheStepBefore()
    {
        var history = new EditHistory();
        var first = Type(history, string.Empty, "a", 0);
        history.BreakGroup();
        Type(history, first.Text, "b", 0);
        history.TryUndo(out _);
        Type(history, first.Text, "c", 0);

        history.TryUndo(out var undone);

        Assert.Equal("a", undone.Text);
    }

    [Fact]
    public void AnUnchangedText_IsNotAStep()
    {
        var history = new EditHistory();
        history.Record(new TextSnapshot("a", 0), new TextSnapshot("a", 1), 0);

        Assert.False(history.CanUndo);
    }

    [Fact]
    public void OnlyTheNewestSteps_AreKept()
    {
        var history = new EditHistory();
        var text = string.Empty;
        for (var i = 0; i < EditHistory.MaxSteps + 5; i++)
        {
            history.BreakGroup();
            text = Type(history, text, "a", 0).Text;
        }

        var undoCount = 0;
        while (history.TryUndo(out _))
        {
            undoCount++;
        }

        Assert.Equal(EditHistory.MaxSteps, undoCount);
    }

    /// <summary>
    /// Types each character at the end of the text, as the editor reports it.
    /// </summary>
    private static TextSnapshot Type(EditHistory history, string text, string typed, double time)
    {
        var before = new TextSnapshot(text, text.Length);
        foreach (var c in typed)
        {
            var after = new TextSnapshot(before.Text + c, before.Text.Length + 1);
            history.Record(before, after, time);
            before = after;
        }

        return before;
    }
}

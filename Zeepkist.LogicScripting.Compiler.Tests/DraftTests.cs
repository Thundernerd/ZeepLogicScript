using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class DraftTests
{
    private static readonly ScriptId _id = new("id");

    [Fact]
    public void TheHash_IsSha256InHex()
    {
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", Draft.HashOf(string.Empty));
    }

    [Fact]
    public void TheHashOfNoText_IsNull()
    {
        Assert.Null(Draft.HashOf(null));
    }

    [Fact]
    public void ADraftOfAStoredScript_IsBasedOnItsText()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], new Dictionary<ScriptId, int>(), _ => null);
        list.Entries[0].Text = "x = 2";

        var draft = Draft.For(list.Entries[0]);

        Assert.Equal("x = 2", draft.Text);
        Assert.True(draft.IsBasedOn(new StoredScript(_id, "doors", "x = 1", 1)));
        Assert.False(draft.IsBasedOn(new StoredScript(_id, "doors", "x = 2", 1)));
    }

    [Fact]
    public void ADraftOfANeverCompiledScript_HasNoBase()
    {
        var entry = new ScriptList().Add("doors");

        Assert.Null(Draft.For(entry).BaseHash);
    }

    [Fact]
    public void TakeChanged_ReturnsEachDraftOnceUntilItChanges()
    {
        var list = new ScriptList();
        var entry = list.Add("doors");
        var changes = new DraftChanges();

        Assert.Single(changes.TakeChanged(list.Entries));
        Assert.Empty(changes.TakeChanged(list.Entries));

        entry.Text = "x = 1";
        Assert.Equal("x = 1", Assert.Single(changes.TakeChanged(list.Entries)).Text);

        entry.Name = "lamps";
        Assert.Equal("lamps", Assert.Single(changes.TakeChanged(list.Entries)).Name);
    }

    [Fact]
    public void TakeChanged_ReturnsADraftWhoseBaseChanged()
    {
        var list = new ScriptList();
        var entry = list.Add("doors");
        var changes = new DraftChanges();
        changes.TakeChanged(list.Entries);

        list.Update([new StoredScript(entry.Id, "doors", string.Empty, 1)], new Dictionary<ScriptId, int>(), _ => null);

        Assert.NotNull(Assert.Single(changes.TakeChanged(list.Entries)).BaseHash);
    }

    [Fact]
    public void AForgottenDraft_IsReturnedAgain()
    {
        var list = new ScriptList();
        var entry = list.Add("doors");
        var changes = new DraftChanges();
        changes.TakeChanged(list.Entries);

        changes.Forget(entry.Id);

        Assert.Single(changes.TakeChanged(list.Entries));
    }
}

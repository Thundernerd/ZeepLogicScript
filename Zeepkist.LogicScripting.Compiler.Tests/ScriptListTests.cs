using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class ScriptListTests
{
    private static readonly ScriptId _id = new("id");

    [Fact]
    public void ANewScript_IsNeverCompiled()
    {
        var list = new ScriptList();

        var entry = list.Add("doors");

        Assert.Equal("doors", entry.Name);
        Assert.Equal(string.Empty, entry.Text);
        Assert.Equal(ScriptMark.NeverCompiled, entry.Mark);
    }

    [Fact]
    public void NewScripts_GetTheirOwnIds()
    {
        var list = new ScriptList();

        Assert.NotEqual(list.Add("a").Id, list.Add("b").Id);
    }

    [Fact]
    public void AStoredScript_ComesInWithTheLevelsNameAndText()
    {
        var list = new ScriptList();

        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], [_id]);

        var entry = Assert.Single(list.Entries);
        Assert.Equal(_id, entry.Id);
        Assert.Equal("doors", entry.Name);
        Assert.Equal("x = 1", entry.Text);
        Assert.Equal(ScriptMark.None, entry.Mark);
    }

    [Fact]
    public void ChangedText_IsNotCompiled()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], [_id]);

        list.Entries[0].Text = "x = 2";

        Assert.Equal(ScriptMark.NotCompiled, list.Entries[0].Mark);
    }

    [Fact]
    public void ARename_IsNotCompiled()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], [_id]);

        list.Entries[0].Name = "lamps";

        Assert.Equal(ScriptMark.NotCompiled, list.Entries[0].Mark);
    }

    [Fact]
    public void ANewerLevelText_KeepsTheWindowsText()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], [_id]);

        list.Update([new StoredScript(_id, "doors", "x = 0", 1)], [_id]);

        Assert.Equal("x = 1", list.Entries[0].Text);
        Assert.Equal(ScriptMark.NotCompiled, list.Entries[0].Mark);
    }

    [Fact]
    public void AGroupWithoutText_IsCarrierMissing()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], [_id]);

        list.Update([], [_id]);

        Assert.Equal(ScriptMark.CarrierMissing, list.Entries[0].Mark);
    }

    [Fact]
    public void AScriptGoneFromTheLevel_StaysInTheWindow()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], [_id]);

        list.Update([], []);

        var entry = Assert.Single(list.Entries);
        Assert.Equal("x = 1", entry.Text);
        Assert.Equal(ScriptMark.NeverCompiled, entry.Mark);
    }

    [Fact]
    public void AGroupWithoutTextTheWindowDoesntHave_IsNotListed()
    {
        var list = new ScriptList();

        list.Update([], [new ScriptId("orphan")]);

        Assert.Empty(list.Entries);
    }

    [Fact]
    public void ANewScriptOnceStored_HasNoMark()
    {
        var list = new ScriptList();
        var entry = list.Add("doors");
        entry.Text = "x = 1";

        list.Update([new StoredScript(entry.Id, "doors", "x = 1", 1)], [entry.Id]);

        Assert.Single(list.Entries);
        Assert.Equal(ScriptMark.None, entry.Mark);
    }

    [Fact]
    public void ANameInUse_IsAnError()
    {
        var list = new ScriptList();
        list.Add("Doors");

        Assert.NotNull(list.GetNameError("doors"));
    }

    [Fact]
    public void ARenameToItsOwnName_IsFine()
    {
        var list = new ScriptList();
        var entry = list.Add("doors");

        Assert.Null(list.GetNameError("Doors", entry.Id));
    }

    [Fact]
    public void Clear_ForgetsEveryScript()
    {
        var list = new ScriptList();
        list.Add("doors");

        list.Clear();

        Assert.Empty(list.Entries);
    }

    [Fact]
    public void ANameTheLevelStillHolds_IsAnError()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], [_id]);
        list.Entries[0].Name = "lamps";

        Assert.NotNull(list.GetNameError("doors"));
    }

    [Fact]
    public void ARenameBackToTheStoredName_IsFine()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], [_id]);
        list.Entries[0].Name = "lamps";

        Assert.Null(list.GetNameError("doors", _id));
    }

    [Fact]
    public void AStoredScriptWithoutAName_GetsOne()
    {
        var list = new ScriptList();

        list.Update([new StoredScript(_id, string.Empty, "x = 1", 1)], [_id]);

        Assert.Equal("Script", list.Entries[0].Name);
    }

    [Fact]
    public void AStoredScriptWithATakenName_GetsANumber()
    {
        var list = new ScriptList();
        list.Add("doors");

        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], [_id]);

        Assert.Equal("doors 2", list.Entries[1].Name);
    }

    [Fact]
    public void AStoredScriptWithATooLongName_IsCut()
    {
        var list = new ScriptList();

        list.Update([new StoredScript(_id, new string('a', 50), "x = 1", 1)], [_id]);

        Assert.Null(ScriptName.GetError(list.Entries[0].Name, []));
    }
}

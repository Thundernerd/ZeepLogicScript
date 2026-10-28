using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class ScriptListTests
{
    private static readonly ScriptId _id = new("id");
    private static readonly ScriptId _orphanId = new("orphan");

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

        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);

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
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);

        list.Entries[0].Text = "x = 2";

        Assert.Equal(ScriptMark.NotCompiled, list.Entries[0].Mark);
    }

    [Fact]
    public void ARename_IsNotCompiled()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);

        list.Entries[0].Name = "lamps";

        Assert.Equal(ScriptMark.NotCompiled, list.Entries[0].Mark);
    }

    [Fact]
    public void ANewerLevelText_KeepsTheWindowsText()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);

        list.Update([new StoredScript(_id, "doors", "x = 0", 1)], Groups(_id), NoDraft);

        Assert.Equal("x = 1", list.Entries[0].Text);
        Assert.Equal(ScriptMark.NotCompiled, list.Entries[0].Mark);
    }

    [Fact]
    public void AGroupWithoutText_IsCarrierMissing()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);

        list.Update([], Groups(_id), NoDraft);

        Assert.Equal(ScriptMark.CarrierMissing, list.Entries[0].Mark);
    }

    [Fact]
    public void AScriptGoneFromTheLevel_StaysInTheWindow()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);

        list.Update([], Groups(), NoDraft);

        var entry = Assert.Single(list.Entries);
        Assert.Equal("x = 1", entry.Text);
        Assert.Equal(ScriptMark.NeverCompiled, entry.Mark);
    }

    [Fact]
    public void AGroupWithoutTextTheWindowDoesntHave_IsNotListed()
    {
        var list = new ScriptList();

        list.Update([], Groups(_orphanId), NoDraft);

        Assert.Empty(list.Entries);
    }

    [Fact]
    public void ANewScriptOnceStored_HasNoMark()
    {
        var list = new ScriptList();
        var entry = list.Add("doors");
        entry.Text = "x = 1";

        list.Update([new StoredScript(entry.Id, "doors", "x = 1", 1)], Groups(entry.Id), NoDraft);

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
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);
        list.Entries[0].Name = "lamps";

        Assert.NotNull(list.GetNameError("doors"));
    }

    [Fact]
    public void ARenameBackToTheStoredName_IsFine()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);
        list.Entries[0].Name = "lamps";

        Assert.Null(list.GetNameError("doors", _id));
    }

    [Fact]
    public void AStoredScriptWithoutAName_GetsOne()
    {
        var list = new ScriptList();

        list.Update([new StoredScript(_id, string.Empty, "x = 1", 1)], Groups(_id), NoDraft);

        Assert.Equal("Script", list.Entries[0].Name);
    }

    [Fact]
    public void AStoredScriptWithATakenName_GetsANumber()
    {
        var list = new ScriptList();
        list.Add("doors");

        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);

        Assert.Equal("doors 2", list.Entries[1].Name);
    }

    [Fact]
    public void AStoredScriptWithATooLongName_IsCut()
    {
        var list = new ScriptList();

        list.Update([new StoredScript(_id, new string('a', 50), "x = 1", 1)], Groups(_id), NoDraft);

        Assert.Null(ScriptName.GetError(list.Entries[0].Name, []));
    }

    [Fact]
    public void AGroupWithoutTextTheWindowDoesntHave_IsOrphaned()
    {
        var list = new ScriptList();

        list.Update([], new Dictionary<ScriptId, int> { [_orphanId] = 3 }, NoDraft);

        var orphan = Assert.Single(list.Orphans);
        Assert.Equal(_orphanId, orphan.Id);
        Assert.Equal(3, orphan.BlockCount);
        Assert.Null(orphan.Draft);
    }

    [Fact]
    public void AGroupWhoseScriptTheWindowHas_IsNotOrphaned()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);

        list.Update([], Groups(_id), NoDraft);

        Assert.Empty(list.Orphans);
        Assert.Equal(ScriptMark.CarrierMissing, list.Entries[0].Mark);
    }

    [Fact]
    public void AnOrphan_ComesWithItsDraft()
    {
        var list = new ScriptList();
        var draft = new Draft(_orphanId, "doors", "x = 1", null);

        list.Update([], Groups(_orphanId), id => id == _orphanId ? draft : null);

        Assert.Same(draft, Assert.Single(list.Orphans).Draft);
    }

    [Fact]
    public void Restore_AddsTheDraftsScriptWithoutItsText()
    {
        var list = new ScriptList();
        list.Update([], Groups(_orphanId), _ => new Draft(_orphanId, "doors", "x = 1", Draft.HashOf("x = 0")));

        var entry = list.Restore(list.Orphans[0]);

        Assert.Equal(_orphanId, entry.Id);
        Assert.Equal("doors", entry.Name);
        Assert.Equal("x = 1", entry.Text);
        Assert.Equal(ScriptMark.CarrierMissing, entry.Mark);
        Assert.Empty(list.Orphans);
    }

    [Fact]
    public void Restore_TakesAFreeName()
    {
        var list = new ScriptList();
        list.Add("doors");
        list.Update([], Groups(_orphanId), _ => new Draft(_orphanId, "doors", "x = 1", null));

        var entry = list.Restore(list.Orphans[0]);

        Assert.Equal("doors 2", entry.Name);
    }

    [Fact]
    public void ARestoredScript_StaysOutOfTheOrphans()
    {
        var list = new ScriptList();
        list.Update([], Groups(_orphanId), _ => new Draft(_orphanId, "doors", "x = 1", null));
        list.Restore(list.Orphans[0]);

        list.Update([], Groups(_orphanId), NoDraft);

        Assert.Empty(list.Orphans);
        Assert.Single(list.Entries);
    }

    [Fact]
    public void ADraftTypedOverTheLevelsText_IsUsed()
    {
        var list = new ScriptList();
        var draft = new Draft(_id, "lamps", "x = 2", Draft.HashOf("x = 1"));

        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), _ => draft);

        var entry = Assert.Single(list.Entries);
        Assert.Equal("lamps", entry.Name);
        Assert.Equal("x = 2", entry.Text);
        Assert.Equal(ScriptMark.NotCompiled, entry.Mark);
    }

    [Fact]
    public void ADraftTypedOverOtherText_IsNotUsed()
    {
        var list = new ScriptList();
        var draft = new Draft(_id, "lamps", "x = 2", Draft.HashOf("x = 0"));

        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), _ => draft);

        Assert.Equal("doors", list.Entries[0].Name);
        Assert.Equal("x = 1", list.Entries[0].Text);
    }

    [Fact]
    public void ADraftOfANeverCompiledScript_IsNotUsedOverTheLevelsText()
    {
        var list = new ScriptList();
        var draft = new Draft(_id, "lamps", "x = 2", null);

        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), _ => draft);

        Assert.Equal("x = 1", list.Entries[0].Text);
    }

    [Fact]
    public void ADraft_DoesntChangeAScriptTheWindowHas()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);

        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), _ => new Draft(_id, "doors", "x = 2", Draft.HashOf("x = 1")));

        Assert.Equal("x = 1", list.Entries[0].Text);
    }

    [Fact]
    public void Remove_DropsTheScript()
    {
        var list = new ScriptList();
        var entry = list.Add("doors");

        list.Remove(entry.Id);

        Assert.Empty(list.Entries);
    }

    private static Draft NoDraft(ScriptId id)
    {
        return null;
    }

    private static Dictionary<ScriptId, int> Groups(params ScriptId[] ids)
    {
        return ids.ToDictionary(id => id, _ => 1);
    }
}

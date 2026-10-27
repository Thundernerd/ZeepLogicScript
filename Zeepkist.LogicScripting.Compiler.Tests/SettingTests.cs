using System.Linq;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class SettingTests
{
    [Theory]
    [InlineData("death(1, player: all)", "xpr=0")]
    [InlineData("death(1, player: 1)", "xpr=1")]
    [InlineData("death(1, player: 4)", "xpr=4")]
    [InlineData("player_input(player: 1)", "xpr=0")]
    [InlineData("player_input(player: 4)", "xpr=3")]
    [InlineData("wheel_telemetry(player: 2)", "xpr=1")]
    [InlineData("force_giver(player: 3)", "xpr=3")]
    [InlineData("random(min: -5, max: 10)", "xv1=-5, xv2=10")]
    [InlineData("counter(max_reset: 9, min_reset: -9)", "ld1=9, ld2=-9")]
    [InlineData("simple_memory(initial: 7)", "oi2=7")]
    [InlineData("toggle_latch(initial: true)", "x5=1")]
    [InlineData("date_timer(utc: true)", "utc=1")]
    [InlineData("pulser(mode: decreased)", "xpl=5")]
    [InlineData("lerp(op: smooth_lerp)", "xmo=1")]
    [InlineData("wheel_telemetry(mode: physics_material)", "xbs=3")]
    [InlineData("force_giver(particles: fan, space: global_rotation, force_mode: velocity_change)", "xfm=3, xco=4, xpt=5")]
    public void Settings_AreStoredAsTheEditorStoresThem(string call, string settings)
    {
        var result = ScriptCompiler.Compile(call, 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(settings, Render(result.Blocks.Last()));
    }

    [Theory]
    [InlineData("death(1, player: 5)", 1, 18, "player takes all, 1, 2, 3 or 4")]
    [InlineData("player_input(player: all)", 1, 22, "player takes 1, 2, 3 or 4")]
    [InlineData("player_input(player: 0)", 1, 22, "player takes 1, 2, 3 or 4")]
    [InlineData("player_input(player: 1 + 1)", 1, 22, "player takes 1, 2, 3 or 4")]
    [InlineData("generator(player: 1)", 1, 11, "'generator' has no input or Setting 'player'")]
    public void Player_Errors(string text, int line, int column, string message)
    {
        AssertSingleError(text, line, column, message);
    }

    [Fact]
    public void Style_PicksTheBlock()
    {
        var result = ScriptCompiler.Compile("power_display(passthrough(1, style: skinny_2h), style: compact)", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal([2414, 2506, 2513], result.Blocks.Select(b => b.Style.BlockId));
        Assert.All(result.Blocks.Skip(1), b => Assert.Empty(b.Settings));
    }

    [Theory]
    [InlineData("power_display(1, style: tiny)", 1, 25, "'tiny' isn't a style of power_display; use normal, compact")]
    [InlineData("power_display(1, style: 1)", 1, 25, "style takes a word: normal, compact")]
    [InlineData("power_display(1, stile: compact)", 1, 18, "'power_display' has no input or Setting 'stile'; did you mean style?")]
    [InlineData("logic(1, 1, style: normal)", 1, 13, "'logic' has no input or Setting 'style'")]
    [InlineData("power_display(1, style: compact, style: normal)", 1, 34, "'style' is given twice")]
    public void Style_Errors(string text, int line, int column, string message)
    {
        AssertSingleError(text, line, column, message);
    }

    [Fact]
    public void Hide_PerCall_HidesThatBlockOnly()
    {
        var result = ScriptCompiler.Compile("power_display(logic(1, 1, op: or, hide: true))", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(["lbp=1", "xmo=1, lbhd=1", ""], result.Blocks.Select(Render));
    }

    [Fact]
    public void DefaultHide_HidesEveryBlock_AndTheCallWins()
    {
        var result = ScriptCompiler.Compile("power_display(1 & 2, hide: false)\ndefault hide: true", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(["lbp=1, lbhd=1", "lbp=2, lbhd=1", "xmo=0, lbhd=1", "lbhd=0"], result.Blocks.Select(Render));
    }

    [Theory]
    [InlineData("default hide: true\ndefault hide: false", 2, 1, "default hide: is given twice")]
    [InlineData("default op: and", 1, 9, "Only hide: has a default")]
    [InlineData("default hide: 1", 1, 15, "hide takes true or false")]
    public void DefaultHide_Errors(string text, int line, int column, string message)
    {
        AssertSingleError(text, line, column, message);
    }

    [Fact]
    public void Default_IsStillAName()
    {
        var result = ScriptCompiler.Compile("default = 1 & 1\npower_display(default)", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    [Theory]
    [InlineData("t = simple_telemetry()\npower_display(t.speed)", 0)]
    [InlineData("t = simple_telemetry(mode: basic)\npower_display(t.state)", 3)]
    [InlineData("t = simple_telemetry(mode: local_velocity)\npower_display(t.x)", 1)]
    [InlineData("t = simple_telemetry(mode: global_velocity)\npower_display(t.heading)", 3)]
    [InlineData("power_display(simple_telemetry(mode: global_velocity).x)", 0)]
    public void ModeOutputs_FollowTheMode(string text, int connector)
    {
        var result = ScriptCompiler.Compile(text, 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(connector, Assert.Single(result.Wires).FromConnector);
    }

    [Theory]
    [InlineData("t = simple_telemetry(mode: local_velocity)\npower_display(t.speed)", 2, 17, "'simple_telemetry' has no output 'speed' in mode local_velocity; use yaw, x, y, z")]
    [InlineData("t = simple_telemetry()\npower_display(t.heading)", 2, 17, "'simple_telemetry' has no output 'heading' in mode basic; use speed, yaw, up, state")]
    [InlineData("t = simple_telemetry(mode: global_velocity)\npower_display(t)", 2, 15, "'t' has several outputs; pick one, like t.x")]
    public void ModeOutputs_Errors(string text, int line, int column, string message)
    {
        AssertSingleError(text, line, column, message);
    }

    [Theory]
    [InlineData("toggler(1)", 1, 1, "'toggler' needs targets:, 1 to 8 placed blocks to switch; placed(...) isn't available yet")]
    [InlineData("door(1)", 1, 1, "'door' needs targets:, 1 to 8 placed blocks to switch; placed(...) isn't available yet")]
    [InlineData("toggler(1, targets: x)", 1, 21, "targets takes placed blocks, and placed(...) isn't available yet")]
    public void Toggler_NeedsPlacedTargets(string text, int line, int column, string message)
    {
        AssertSingleError(text, line, column, message);
    }

    [Theory]
    [InlineData("power_display(1)", new[] { 2415 }, 1, 1, "'power_display' style normal doesn't match this game version and is turned off")]
    [InlineData("power_display(1, style: compact)", new[] { 2513 }, 1, 1, "'power_display' style compact doesn't match this game version and is turned off")]
    [InlineData("x = 1 & 2", new[] { 2420 }, 1, 7, "'logic' doesn't match this game version and is turned off")]
    [InlineData("x = !1", new[] { 2418 }, 1, 5, "'digitizer' doesn't match this game version and is turned off")]
    [InlineData("x = 1 & 1", new[] { 2414 }, 1, 5, "'generator' doesn't match this game version and is turned off")]
    public void DisabledBlocks_AreErrors(string text, int[] disabled, int line, int column, string message)
    {
        var result = ScriptCompiler.Compile(text, 0, disabled);

        var error = Assert.Single(result.Errors);
        Assert.Equal((new Position(line, column), message), (error.Position, error.Message));
    }

    [Fact]
    public void DisabledBlocks_LeaveOtherStylesWorking()
    {
        var result = ScriptCompiler.Compile("power_display(1, style: compact)", 0, [2415]);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    private static string Render(CompiledBlock block)
    {
        return string.Join(", ", block.Settings.Select(s => $"{s.Key}={(s.Kind == SettingKind.Int ? s.IntValue.ToString() : s.TextValue)}"));
    }

    private static void AssertSingleError(string text, int line, int column, string message)
    {
        var result = ScriptCompiler.Compile(text, 0);

        var error = Assert.Single(result.Errors);
        Assert.Equal((new Position(line, column), message), (error.Position, error.Message));
    }
}

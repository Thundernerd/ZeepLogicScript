using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class SettingInputTests
{
    private const string PulseAfter = "function pulse_after(x, ticks) -> out\n    out = pulser(delay(x, ticks: ticks))\nend\n";

    [Theory]
    [InlineData("function f(x, n) -> y\n    y = delay(x, ticks: n)\nend\n", "f(1, n: 20)", "delay(1, ticks: 20)")]
    [InlineData("function f(x, n) -> y\n    y = delay(x, ticks: n)\nend\n", "f(1, 20)", "delay(1, ticks: 20)")]
    [InlineData("function f(x, n) -> y\n    y = counter(x, min_reset: n)\nend\n", "f(1, n: -3)", "counter(1, min_reset: -3)")]
    [InlineData("function f(x, b) -> y\n    y = digitizer(x, invert: b)\nend\n", "f(1, b: true)", "digitizer(1, invert: true)")]
    [InlineData("function f(who) -> y\n    y = player_input(player: who).horn\nend\n", "f(who: 2)", "player_input(player: 2).horn")]
    [InlineData("function f(a, b, o) -> y\n    y = logic(a, b, op: o)\nend\n", "f(1, 0, o: xor)", "logic(1, 0, op: xor)")]
    [InlineData("function f(x, s) -> y\n    y = passthrough(x, style: s)\nend\n", "f(1, s: skinny_2h)", "passthrough(1, style: skinny_2h)")]
    [InlineData("function f(x, h) -> y\n    y = passthrough(x, hide: h)\nend\n", "f(1, h: true)", "passthrough(1, hide: true)")]
    public void Constant_IsSubstitutedIntoTheSetting(string function, string call, string handWritten)
    {
        var expanded = Compile($"{function}power_display({call})");

        Assert.True(expanded.Succeeded, string.Join("\n", expanded.Errors));
        Assert.Equal(Describe(Compile($"power_display({handWritten})")), Describe(expanded));
    }

    [Fact]
    public void PulseAfter_DelaysByTheTicksPassed()
    {
        var result = Compile(PulseAfter + "p = player_input(player: 1)\ny = pulse_after(p.horn, ticks: 20)\npower_display(y)");

        Assert.Equal("pulser(delay[ldl=20](player_input[xpr=0]().horn))", BlockTree.RenderLast(result));
    }

    [Fact]
    public void EachCall_GetsItsOwnConstant()
    {
        var result = Compile(PulseAfter + "power_display(pulse_after(1, ticks: 2) & pulse_after(1, ticks: 3))");

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(Describe(Compile("power_display(pulser(delay(1, ticks: 2)) & pulser(delay(1, ticks: 3)))")), Describe(result));
    }

    [Fact]
    public void Constant_IsForwardedThroughTwoFunctions()
    {
        var result = Compile(
            PulseAfter
            + "function slow(x, wait) -> y\n    y = pulse_after(x, wait)\nend\n"
            + "function slower(x, wait) -> y\n    y = slow(x, wait: wait)\nend\n"
            + "power_display(slower(1, wait: 7))");

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(Describe(Compile("power_display(pulser(delay(1, ticks: 7)))")), Describe(result));
    }

    [Fact]
    public void Forwarding_WorksWhicheverOrderTheFunctionsAreDefinedIn()
    {
        var result = Compile(
            "function slower(x, wait) -> y\n    y = slow(x, wait: wait)\nend\n"
            + "function slow(x, wait) -> y\n    y = pulse_after(x, wait)\nend\n"
            + PulseAfter
            + "power_display(slower(1, wait: 7))");

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    [Fact]
    public void Constant_IsForwardedIntoALibrary()
    {
        var result = Compile(
            "function wait(x, n) -> y\n    y = timing.pulse_after(x, ticks: n)\nend\npower_display(wait(1, n: 4))",
            new() { ["timing"] = PulseAfter });

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(Describe(Compile("power_display(pulser(delay(1, ticks: 4)))")), Describe(result));
    }

    [Fact]
    public void UncalledFunction_WithASettingInput_HasNoErrors()
    {
        var result = Compile(PulseAfter + "function slow(x, wait) -> y\n    y = pulse_after(x, wait)\nend\npower_display(1)");

        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("p = player_input()\npower_display(pulse_after(p.horn, ticks: p.horn))", "5:42: 'ticks' is used as a Setting in 'pulse_after'; pass a number, true or false, or a word, not a Wire")]
    [InlineData("t = 20\npower_display(pulse_after(1, ticks: t))", "5:37: 'ticks' is used as a Setting in 'pulse_after'; pass a number, true or false, or a word, not a Wire")]
    [InlineData("power_display(pulse_after(1, 2 + 3))", "4:32: 'ticks' is used as a Setting in 'pulse_after'; pass a number, true or false, or a word, not a Wire")]
    [InlineData("power_display(pulse_after(1, ticks: 0))", "4:37: ticks must be at least 1")]
    [InlineData("power_display(pulse_after(1, ticks: long))", "4:37: ticks takes a whole number, like ticks: 5")]
    public void Arguments_Errors(string call, string error)
    {
        var result = Compile(PulseAfter + call);

        Assert.Equal([error], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void WrongOptionWord_IsReportedWhereTheCallWroteIt()
    {
        var result = Compile("function f(a, b, o) -> y\n    y = logic(a, b, op: o)\nend\npower_display(f(1, 0, o: maybe))");

        Assert.Equal(["4:26: 'maybe' isn't an option of op; use and, or, nand, nor, xor, xnor"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void WrongConstant_ForwardedThroughTwoFunctions_IsReportedAtTheFirstCall()
    {
        var result = Compile(
            PulseAfter
            + "function slow(x, wait) -> y\n    y = pulse_after(x, wait)\nend\n"
            + "power_display(slow(1, wait: 0))");

        Assert.Equal(["7:29: ticks must be at least 1"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void WrongConstant_PassedIntoALibrary_IsReportedWhereTheScriptWroteIt()
    {
        var result = Compile("power_display(timing.pulse_after(1, ticks: 0))", new() { ["timing"] = PulseAfter });

        Assert.Equal(["1:44: ticks must be at least 1"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void WrongConstant_WrittenInALibrary_IsReportedAtTheCallIntoIt()
    {
        var result = Compile(
            "power_display(buttons.press(1))",
            new() { ["buttons"] = PulseAfter + "function press(x) -> y\n    y = pulse_after(x, ticks: 0)\nend" });

        Assert.Equal(["1:15: In buttons.press, line 5: ticks must be at least 1"], result.Errors.Select(e => e.ToString()));
    }

    [Theory]
    [InlineData("function f(x, n) -> y\n    y = delay(x, ticks: n) + n\nend\npower_display(f(1, 2))", "2:30: 'n' is used as a Setting on line 2, so it can't also be used as a Signal")]
    [InlineData("function f(x, n) -> y\n    a = n & 1\n    y = delay(x, ticks: n)\nend\npower_display(f(1, 2))", "2:9: 'n' is used as a Setting on line 3, so it can't also be used as a Signal")]
    [InlineData("function f(x, n) -> y\n    y = pulse_after(n, n)\nend\npower_display(f(1, 2))\n" + PulseAfter, "2:21: 'n' is used as a Setting on line 2, so it can't also be used as a Signal")]
    [InlineData("function f(x, n) -> y\n    y = delay(x, ticks: n)\nend\npower_display(f(1, 2))\nfunction g(n) -> y\n    y = f(n, n)\nend", "6:11: 'n' is used as a Setting on line 6, so it can't also be used as a Signal")]
    public void InputUsedAsSignalAndSetting_IsAnError(string text, string error)
    {
        var result = Compile(text);

        Assert.Equal([error], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void InputUsedAsSignalAndSetting_InALibrary_IsALibraryError()
    {
        var result = Compile(
            "power_display(timing.f(1, 2))",
            new() { ["timing"] = "function f(x, n) -> y\n    y = delay(x, ticks: n) + n\nend" });

        Assert.Equal(
            ["1:15: Library 'timing' has an error on line 2: 'n' is used as a Setting on line 2, so it can't also be used as a Signal"],
            result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void ToggleTargets_StayWires()
    {
        var result = Compile(
            "function open(x, wall)\n    toggler(x, targets: wall)\nend\nopen(1, placed(\"W\"))",
            level: new Dictionary<string, PlacedBlock>
            {
                ["W"] = new PlacedBlock(Manifest.BlockManifest.Toggler.DefaultStyle.BlockId)
            });

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    private static string Describe(CompileResult result)
    {
        return BlockTree.RenderLast(result) + " " + string.Join(",", result.Blocks.Select(b => b.Style.Name));
    }

    private static CompileResult Compile(
        string text,
        Dictionary<string, string> libraries = null,
        Dictionary<string, PlacedBlock> level = null)
    {
        return ScriptCompiler.Compile(text, 0, [], uid => level?.GetValueOrDefault(uid), name => libraries?.GetValueOrDefault(name));
    }
}

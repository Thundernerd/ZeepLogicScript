using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// The live check compiles and colors whatever is in the editor while the player types, so every reader of Script
/// text has to answer half-typed and broken text with errors, never an exception.
/// </summary>
public class NeverThrowsTests
{
    // Fixed, so a failure names an input that reproduces
    private const int Seed = 90;

    private static readonly string[] _validScripts =
    [
        "x = 1 & 1\n\npower_display(x)\npower_display(x)",
        "# a whole line\nx = 1 & 2 # after a statement\npower_display(x)",
        "x = logic(\n    1, # first\n    2,\n    op: or\n)\npower_display(x, hide: true)",
        "p = player_input()\nd = delay(p.horn, ticks: 1)\npower_display(!d & -d * 2 + (3 % 4))",
        "press = 1\nheld = delay(held ^ press, ticks: 1)\npower_display(held)",
        "default hide: true\nm = multiplexer(1, 2, 3, 4, select: 0)\npower_display(m, style: compact)",
        "dm = demultiplexer(1, 2)\npower_display(dm.ch1 + dm.ch4)",
        "t = placed(\"T\")\nlamp_1 = placed(\"L\")\nlamp_1(intensity: t)\nlamp_1(red: 1, green: 2)",
        "s = placed(\"S\")\nd = placed(\"D\")\npower_display(s.x & d.x)",
        "x = delay(later, ticks: 2)\nlater = passthrough(x, style: skinny_2h)",
        "r = random(min: -5, max: 5)\nc = counter(r)\npower_display(math(c, 2, op: pow) != 1 | c >= 3 ^ c < 2)",
        "toggler(1, targets: [placed(\"T\"), placed(\"L\")])\ndeath(1)",
        "function edge(x) -> rising\n    prev = delay(x, ticks: 1)\n    rising = x & !prev\nend\npower_display(edge(player_input().horn))",
        "l = latch(b: 0, a: 1)\npower_display(l.q)\nfunction latch(a, b) -> q, not_q\n    q = delay(!b & (a | q), ticks: 1)\n    not_q = !q\nend",
        "function light(lamp, x)\n    lamp(intensity: x)\n    t = placed(\"T\")\nend\nlight(placed(\"L\"), 1)\nx = delay(same(later), ticks: 1)\nlater = 1\nfunction same(v) -> w\nw = v\nend",
        "t = simple_telemetry(mode: local_velocity)\nw = placed(\"W\")\nw(red: t.x)"
    ];

    private static readonly Dictionary<string, PlacedBlock> _level = new()
    {
        ["T"] = new PlacedBlock(BlockManifest.Trigger.DefaultStyle.BlockId),
        ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId),
        ["W"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId, wiredInputs: [0]),
        ["D"] = new PlacedBlock(BlockManifest.DistanceSensor.DefaultStyle.BlockId, downstream: ["T"]),
        ["S"] = new PlacedBlock(
            BlockManifest.SimpleTelemetry.DefaultStyle.BlockId,
            [BlockManifest.SimpleTelemetryMode.Choose("local_velocity")]),
        ["G"] = new PlacedBlock(BlockManifest.Passthrough.DefaultStyle.BlockId, isInBlockGroup: true),
        ["OTHER"] = new PlacedBlock(1)
    };

    private static readonly string[] _tokens =
    [
        "x", "y", "later", "default", "function", "end", "->", "f", "f(x)", "function f(x) -> y\n", "\nend\n", "true", "false", "placed", "logic", "power_display", "delay", "toggler", "targets",
        "player_input", "demultiplexer", "multiplexer", "math", "op", "ticks", "hide", "style", "ch1", "horn", "and",
        "simple_telemetry", "mode", "local_velocity",
        "\"W\"", "\"G\"", "\"OTHER\"", "0", "1", "-1", "99999999999999999999", "2147483648", "\"T\"", "\"L\"", "\"", "(", ")", "[", "]", ",", ":",
        ".", "=", "==", "!=", "&", "&&", "|", "||", "^", "!", "~", "-", "+", "*", "/", "%", "<", ">", "<=", ">=", "<<",
        ">>", "#", " ", "\t", "\n", "\r\n", "$", "1.5", "é"
    ];

    private static readonly Position[] _oddPositions =
    [
        new(0, 0), new(1, 0), new(1, 1), new(-1, -1), new(2, 500), new(500, 1), new(int.MaxValue, int.MaxValue)
    ];

    [Fact]
    public void ValidScripts_Compile()
    {
        foreach (var script in _validScripts)
        {
            var result = Compile(script);
            Assert.True(result.Succeeded, $"{Escape(script)}\n{string.Join("\n", result.Errors)}");
        }
    }

    [Fact]
    public void RandomCharacters()
    {
        var random = new Random(Seed);
        const string Characters = "abxyz_019 \t\n\r\"#()[],:.=!&|^~-+*/%<>${}'\\é\0";
        for (var i = 0; i < 2000; i++)
        {
            StringBuilder text = new();
            var length = random.Next(1, 60);
            for (var j = 0; j < length; j++)
            {
                text.Append(random.Next(20) == 0 ? (char)random.Next(char.MaxValue) : Characters[random.Next(Characters.Length)]);
            }

            AssertNeverThrows(text.ToString());
        }
    }

    [Fact]
    public void RandomTokens()
    {
        var random = new Random(Seed);
        for (var i = 0; i < 3000; i++)
        {
            StringBuilder text = new();
            var length = random.Next(1, 30);
            for (var j = 0; j < length; j++)
            {
                text.Append(_tokens[random.Next(_tokens.Length)]);
                if (random.Next(2) == 0)
                    text.Append(' ');
            }

            AssertNeverThrows(text.ToString());
        }
    }

    [Fact]
    public void ValidScripts_CutOffAtEveryPosition()
    {
        foreach (var script in _validScripts)
        {
            for (var i = 0; i <= script.Length; i++)
            {
                AssertNeverThrows(script.Substring(0, i));
                AssertNeverThrows(script.Substring(i));
            }
        }
    }

    [Fact]
    public void ValidScripts_WithOneCharacterDeleted()
    {
        foreach (var script in _validScripts)
        {
            for (var i = 0; i < script.Length; i++)
            {
                AssertNeverThrows(script.Remove(i, 1));
            }
        }
    }

    [Fact]
    public void ValidScripts_WithOneCharacterInserted()
    {
        const string Inserted = "x1\"#()[],:.=!&-\n ";
        foreach (var script in _validScripts)
        {
            for (var i = 0; i <= script.Length; i++)
            {
                foreach (var c in Inserted)
                {
                    AssertNeverThrows(script.Insert(i, c.ToString()));
                }
            }
        }
    }

    [Fact]
    public void ValidScripts_WithRandomTokensSwapped()
    {
        var random = new Random(Seed);
        for (var i = 0; i < 2000; i++)
        {
            var script = _validScripts[random.Next(_validScripts.Length)];
            var at = random.Next(script.Length);
            var length = Math.Min(random.Next(1, 6), script.Length - at);
            AssertNeverThrows(script.Remove(at, length).Insert(at, _tokens[random.Next(_tokens.Length)]));
        }
    }

    [Theory]
    [InlineData("(", ")", "1")]
    [InlineData("(", "", "1")]
    [InlineData("", ")", "1")]
    [InlineData("[", "]", "1")]
    [InlineData("[", "", "1")]
    [InlineData("power_display(", ")", "1")]
    [InlineData("logic(1, ", ")", "1")]
    [InlineData("-", "", "1")]
    [InlineData("!", "", "x")]
    [InlineData("- ", "", "x")]
    [InlineData("1 & ", "", "1")]
    [InlineData("x.", "", "y")]
    [InlineData("power_display(x) + ", "", "1")]
    [InlineData("logic(op_in: ", ")", "1")]
    public void DeepNesting(string open, string close, string inner)
    {
        const int Depth = 100_000;
        StringBuilder text = new("x = ");
        text.Insert(text.Length, open, Depth).Append(inner).Insert(text.Length, close, Depth);
        AssertNeverThrows(text.ToString());
    }

    [Fact]
    public void LongChainOfOperators()
    {
        AssertNeverThrows("x = 1" + string.Concat(Enumerable.Repeat(" & 1", 100_000)) + "\npower_display(x)");
    }

    [Fact]
    public void LongChainOfDefinitions_Compiles()
    {
        StringBuilder text = new("x0 = player_input().horn\n");
        for (var i = 1; i < 20_000; i++)
        {
            text.Append($"x{i} = x{i - 1} & 1\n");
        }

        var result = AssertNeverThrows(text.ToString());
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    [Fact]
    public void LongChainOfDefinitions_UsedBeforeTheyAreDefined()
    {
        StringBuilder text = new("power_display(x0)\n");
        for (var i = 0; i < 20_000; i++)
        {
            text.Append($"x{i} = delay(x{i + 1}, ticks: 1)\n");
        }

        AssertNeverThrows(text.Append("x20000 = 1").ToString());
    }

    [Theory]
    [InlineData("x")]
    [InlineData("1")]
    [InlineData("\"")]
    [InlineData("#")]
    [InlineData(" ")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("power_display(1)\n")]
    public void VeryLongLine(string repeated)
    {
        AssertNeverThrows("x = " + string.Concat(Enumerable.Repeat(repeated, 100_000)));
    }

    [Theory]
    [InlineData("power_display(\"")]
    [InlineData("x = placed(\"L")]
    [InlineData("x = placed(\"L\"")]
    [InlineData("x = logic(1,\n")]
    [InlineData("x = [1, 2\n")]
    [InlineData("x = (1 &\n")]
    [InlineData("\"\n\"\n\"")]
    [InlineData("((((\n))))")]
    [InlineData(")))\n(((")]
    [InlineData("]]]\n[[[")]
    [InlineData("function f(\nend")]
    [InlineData("function f(x) -> y\n")]
    [InlineData("function f(x) -> (y\nend")]
    public void UnterminatedStringsAndBrackets(string text)
    {
        AssertNeverThrows(text);
        AssertNeverThrows(text + "\npower_display(1)");
    }

    private static CompileResult AssertNeverThrows(string text)
    {
        try
        {
            return SmallStack.Run(() => Read(text));
        }
        catch (Exception exception)
        {
            Assert.Fail($"{Escape(text)}\n{exception}");
            return null;
        }
    }

    /// <summary>
    /// Everything the editor does with the text while the player types.
    /// </summary>
    private static CompileResult Read(string text)
    {
        var result = Compile(text);
        AssertSpansInOrder(text, ScriptHighlighter.Highlight(text));
        ScriptText.GetErrorSpans(text, result.Errors.Select(e => e.Position).Concat(_oddPositions));
        foreach (var position in _oddPositions)
        {
            ScriptText.GetIndex(text, position);
        }

        var definedNames = DefinedNames.Read(text, uid => _level.GetValueOrDefault(uid));
        Complete(new TextSnapshot(text, -1), definedNames);
        Complete(new TextSnapshot(text, text.Length + 1), definedNames);

        foreach (var snapshot in GetSnapshots(text))
        {
            Edit(snapshot);
            Complete(snapshot, definedNames);
        }

        return result;
    }

    /// <summary>
    /// The editor draws the text run by run between the spans, so they have to follow each other inside the text.
    /// </summary>
    private static void AssertSpansInOrder(string text, IReadOnlyList<HighlightSpan> spans)
    {
        var end = 0;
        foreach (var span in spans)
        {
            Assert.True(
                span.Start >= end && span.Length >= 0 && span.Start + span.Length <= text.Length,
                $"Span {span.Start}+{span.Length} after {end} in {text.Length} characters");
            end = span.Start + span.Length;
        }
    }

    private static IEnumerable<TextSnapshot> GetSnapshots(string text)
    {
        foreach (var caret in new[] { 0, text.Length / 2, text.Length })
        {
            yield return new TextSnapshot(text, caret);
            yield return new TextSnapshot(text, caret, -caret);
            yield return new TextSnapshot(text, caret, text.Length - caret);
        }
    }

    private static void Edit(TextSnapshot snapshot)
    {
        EditCommands.ToggleComment(snapshot);
        EditCommands.Indent(snapshot);
        EditCommands.Outdent(snapshot);
        EditCommands.NewLine(snapshot);
        EditCommands.MoveTo(snapshot, 0, false);
        EditCommands.MoveTo(snapshot, snapshot.Text.Length, true);
        EditCommands.Home(snapshot, false);
        EditCommands.Home(snapshot, true);
        EditCommands.End(snapshot, false);
        EditCommands.End(snapshot, true);
        EditCommands.DeleteTo(snapshot, 0);
        EditCommands.DeleteTo(snapshot, snapshot.Text.Length);
        EditCommands.MoveLines(snapshot, -1);
        EditCommands.MoveLines(snapshot, 1);
        EditCommands.Duplicate(snapshot);
        EditCommands.DeleteLines(snapshot);
    }

    /// <summary>
    /// Accepts the first item too, so a word span that doesn't fit the text would fail here.
    /// </summary>
    private static void Complete(TextSnapshot snapshot, IReadOnlyList<DefinedName> definedNames)
    {
        var context = CaretContext.At(snapshot.Text, snapshot.Caret);
        var items = Completion.GetItems(context, definedNames);
        if (items.Count > 0)
            Completion.Accept(context, items[0]);
    }

    private static CompileResult Compile(string text)
    {
        return ScriptCompiler.Compile(text, 1, [], uid => _level.GetValueOrDefault(uid));
    }

    private static string Escape(string text)
    {
        var shown = text.Length > 200 ? text.Substring(0, 200) + "…" : text;
        return shown.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}

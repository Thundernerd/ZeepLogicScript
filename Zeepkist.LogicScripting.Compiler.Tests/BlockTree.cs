using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Writes a compiled block and what feeds it as a nested call, so tests can read the wiring at a glance:
/// a Generator is its power, any other block is <c>function[settings](input, …)</c> with <c>_</c> for an unwired input.
/// </summary>
internal static class BlockTree
{
    public static string Render(CompileResult result, int block)
    {
        var compiled = result.Blocks[block];
        if (compiled.Type == BlockManifest.Generator)
            return compiled.Settings.Count == 0 ? "0" : compiled.Settings[0].TextValue;

        var settings = compiled.Settings.Count == 0
            ? string.Empty
            : "[" + string.Join(", ", compiled.Settings.Select(s => $"{s.Key}={(s.Kind == SettingKind.Int ? s.IntValue.ToString() : s.TextValue)}")) + "]";

        var lastWired = compiled.Type.Inputs
            .Select((input, i) => (input, i))
            .Where(x => result.Wires.Any(w => w.To == block && w.ToConnector == x.input.Index))
            .Select(x => x.i + 1)
            .DefaultIfEmpty(0)
            .Max();

        var inputs = compiled.Type.Inputs.Take(lastWired).Select(input =>
        {
            var wire = result.Wires.FirstOrDefault(w => w.To == block && w.ToConnector == input.Index);
            if (wire == null)
                return "_";

            var source = result.Blocks[wire.From];
            var output = source.Type.Outputs.Count > 1 ? "." + source.Type.Outputs.First(o => o.Index == wire.FromConnector).Name : string.Empty;
            return Render(result, wire.From) + output;
        });

        return $"{compiled.Type.Function}{settings}({string.Join(", ", inputs)})";
    }

    /// <summary>
    /// Renders the input of the last block, typically the <c>power_display(...)</c> a test ends with.
    /// </summary>
    public static string RenderLast(CompileResult result)
    {
        var last = result.Blocks.Count - 1;
        var wire = result.Wires.Single(w => w.To == last);
        return Render(result, wire.From);
    }
}

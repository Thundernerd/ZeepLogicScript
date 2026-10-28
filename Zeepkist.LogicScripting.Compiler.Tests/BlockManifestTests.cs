using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockManifestTests
{
    public static IEnumerable<object[]> Functions => BlockManifest.All
        .Where(t => !t.IsPlacedOnly && t != BlockManifest.Toggler)
        .Select(t => new object[] { t.Function });

    [Theory]
    [MemberData(nameof(Functions))]
    public void EveryBlockFunction_ButToggler_PlacesItsDefaultBlock(string function)
    {
        BlockManifest.TryGetFunction(function, out var type);
        var arguments = type == BlockManifest.Delay ? "ticks: 1" : string.Empty;

        var result = ScriptCompiler.Compile($"{function}({arguments})", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        var block = Assert.Single(result.Blocks);
        Assert.Equal((type, type.DefaultStyle), (block.Type, block.Style));
    }

    [Fact]
    public void BlockIds_AreUnique()
    {
        var ids = BlockManifest.All.SelectMany(t => t.Styles).Select(s => s.BlockId).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Connectors_AreUniquePerBlock()
    {
        foreach (var type in BlockManifest.All)
        {
            var indices = type.Inputs.Concat(type.Outputs).Select(c => c.Index).ToList();
            Assert.True(indices.Count == indices.Distinct().Count(), type.Function);

            var names = type.Inputs.Select(c => c.Name).ToList();
            Assert.True(names.Count == names.Distinct().Count(), type.Function);
        }
    }

    [Fact]
    public void ModeOutputs_UseTheSameConnectorsInEveryMode()
    {
        foreach (var type in BlockManifest.All.Where(t => t.Modes != null))
        {
            Assert.Equal(type.Modes.Setting.Options.Count, type.Modes.Outputs.Count);
            foreach (var outputs in type.Modes.Outputs)
            {
                Assert.Equal(type.Outputs.Select(o => o.Index), outputs.Select(o => o.Index));
            }
        }
    }

    [Fact]
    public void Names_AreSnakeCase()
    {
        var snakeCase = new Regex("^[a-z][a-z0-9]*(_[a-z0-9]+)*$");
        var names = BlockManifest.All.SelectMany(t =>
            new[] { t.Function }
                .Concat(t.Inputs.Concat(t.Outputs).Select(c => c.Name))
                .Concat(t.Settings.Select(s => s.Name))
                .Concat(t.Settings.Where(s => s.Type == SettingType.Option).SelectMany(s => s.Options))
                .Concat(t.Styles.Where(s => s.Name != null).Select(s => s.Name)));

        Assert.All(names, name => Assert.Matches(snakeCase, name));
    }

    [Fact]
    public void StyledTypes_NameEveryStyle()
    {
        foreach (var type in BlockManifest.All.Where(t => t.Styles.Count > 1))
        {
            Assert.All(type.Styles, s => Assert.NotNull(s.Name));
        }
    }

    [Theory]
    [InlineData("mux", "multiplexer")]
    [InlineData("demux", "demultiplexer")]
    [InlineData("lerp", "math_3_pins")]
    [InlineData("door", "toggler")]
    public void Aliases_CallTheirBlockFunction(string alias, string function)
    {
        Assert.True(BlockManifest.TryGetFunction(alias, out var type));
        Assert.Equal(function, type.Function);
    }

    [Fact]
    public void Aliases_AreReservedLikeBlockFunctions()
    {
        var result = ScriptCompiler.Compile("mux = 1", 0);

        var error = Assert.Single(result.Errors);
        Assert.Equal("'mux' is a Block Function and can't be used as a name", error.Message);
    }

    [Fact]
    public void PlacedOnlyTypes_AreNotReserved()
    {
        var result = ScriptCompiler.Compile("trigger = 1 & 1\npower_display(trigger)", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    [Fact]
    public void PlacedOnlyTypes_AreNotBlockFunctions()
    {
        Assert.All(BlockManifest.All.Where(t => t.IsPlacedOnly), type =>
        {
            Assert.False(BlockManifest.TryGetFunction(type.Function, out _));
            Assert.True(BlockManifest.TryGetPlacedOnly(type.Function, out _));
        });
    }
}

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Analyzers.Tests;

public class NestedTypeFileAnalyzerTests
{
    [Fact]
    public async Task TopLevelType_IsAllowed()
    {
        var messages = await AnalyzeAsync("Outer.cs", "class Outer { }");

        Assert.Empty(messages);
    }

    [Fact]
    public async Task NestedType_InTheContainingTypesFile_IsReported()
    {
        var messages = await AnalyzeAsync("Outer.cs", "partial class Outer { class Inner { } }");

        Assert.Equal(["Move nested type 'Inner' to a partial file named 'Outer.Inner.cs'"], messages);
    }

    [Fact]
    public async Task NestedType_InItsOwnPartialFile_IsAllowed()
    {
        var messages = await AnalyzeAsync("Outer.Inner.cs", "partial class Outer { class Inner { } }");

        Assert.Empty(messages);
    }

    [Fact]
    public async Task NestedType_InADirectory_MatchesOnFileNameOnly()
    {
        var messages = await AnalyzeAsync("Graph/Outer.Inner.cs", "partial class Outer { class Inner { } }");

        Assert.Empty(messages);
    }

    [Fact]
    public async Task SecondNestedType_InAPartialFile_IsReported()
    {
        var messages = await AnalyzeAsync("Outer.Inner.cs", "partial class Outer { class Inner { } struct Other { } }");

        Assert.Equal(["Move nested type 'Other' to a partial file named 'Outer.Other.cs'"], messages);
    }

    [Fact]
    public async Task DeeplyNestedType_IsNamedAfterEveryContainingType()
    {
        var source = "partial class Outer { partial class Inner { class Deeper { } } }";

        var messages = await AnalyzeAsync("Outer.Inner.cs", source);

        Assert.Equal(["Move nested type 'Deeper' to a partial file named 'Outer.Inner.Deeper.cs'"], messages);
    }

    [Fact]
    public async Task ContainingPartialType_InTheFileOfATypeNestedInIt_IsAllowed()
    {
        var source = "partial class Outer { partial class Middle { class Inner { } } }";

        var messages = await AnalyzeAsync("Outer.Middle.Inner.cs", source);

        Assert.Empty(messages);
    }

    [Fact]
    public async Task ContainingNonPartialType_InTheFileOfATypeNestedInIt_IsReported()
    {
        var source = "partial class Outer { class Middle { class Inner { } } }";

        var messages = await AnalyzeAsync("Outer.Middle.Inner.cs", source);

        Assert.Equal(["Move nested type 'Middle' to a partial file named 'Outer.Middle.cs'"], messages);
    }

    [Fact]
    public async Task SiblingOfTheContainingType_InTheFileOfATypeNestedTwoLevels_IsReported()
    {
        var source = "partial class Outer { partial class Middle { class Inner { } } class Other { } }";

        var messages = await AnalyzeAsync("Outer.Middle.Inner.cs", source);

        Assert.Equal(["Move nested type 'Other' to a partial file named 'Outer.Other.cs'"], messages);
    }

    [Theory]
    [InlineData("struct Inner { }")]
    [InlineData("interface Inner { }")]
    [InlineData("enum Inner { }")]
    [InlineData("delegate void Inner();")]
    [InlineData("record Inner;")]
    [InlineData("record struct Inner;")]
    public async Task EveryKindOfNestedType_IsReported(string nested)
    {
        var messages = await AnalyzeAsync("Outer.cs", $"partial class Outer {{ {nested} }}");

        Assert.Single(messages);
    }

    private static async Task<string[]> AnalyzeAsync(string path, string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: path);
        var compilation = CSharpCompilation.Create(
            "Test",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new NestedTypeFileAnalyzer());
        var diagnostics = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();
        return diagnostics.Select(d => d.GetMessage()).ToArray();
    }
}

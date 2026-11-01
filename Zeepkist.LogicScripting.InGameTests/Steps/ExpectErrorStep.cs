using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the window's check found an error at the one-based line and column whose message contains the text,
/// matching case.
/// </summary>
internal sealed class ExpectErrorStep : ErrorCheckStep
{
    public ExpectErrorStep(Position position, string text)
    {
        Position = position;
        Text = text;
    }

    public Position Position { get; }
    public string Text { get; }

    protected override void Check(StepContext context, IReadOnlyList<Diagnostic> errors)
    {
        var expected = $"{Position}: {Text}";
        var listed = errors.Count == 0 ? "(no errors)" : string.Join("; ", errors.Select(e => e.ToString()));
        if (errors.Any(Matches))
            context.Pass(listed, expected, listed);
        else
            context.Fail("no such error", expected, listed);
    }

    private bool Matches(Diagnostic error)
    {
        return error.Position.Line == Position.Line
            && error.Position.Column == Position.Column
            && error.Message.IndexOf(Text, StringComparison.Ordinal) >= 0;
    }
}

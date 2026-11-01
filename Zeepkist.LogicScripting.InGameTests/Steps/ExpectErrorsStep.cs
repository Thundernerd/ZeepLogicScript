using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Counts the errors the window's check found.
/// </summary>
internal class ExpectErrorsStep : ErrorCheckStep
{
    public ExpectErrorsStep(int count)
    {
        Count = count;
    }

    public int Count { get; }

    protected override void Check(StepContext context, IReadOnlyList<Diagnostic> errors)
    {
        var listed = string.Join("; ", errors.Select(e => e.ToString()));
        if (errors.Count == Count)
            context.Pass(listed, Count.ToString(), errors.Count.ToString());
        else
            context.Fail($"{errors.Count} errors", Count.ToString(), errors.Count == 0 ? "0" : $"{errors.Count}: {listed}");
    }
}

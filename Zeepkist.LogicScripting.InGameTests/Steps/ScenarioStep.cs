using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// One line of a scenario. It passes unless <see cref="Run"/> calls <see cref="StepContext.Fail"/>.
/// </summary>
internal abstract class ScenarioStep
{
    public int Line { get; set; }

    /// <summary>
    /// The step as it's written, for the results.
    /// </summary>
    public string Source { get; set; }

    public abstract IEnumerator Run(StepContext context);
}

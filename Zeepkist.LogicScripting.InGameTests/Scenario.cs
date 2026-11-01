using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// One .zlstest file: the steps a run takes in order, named after the file.
/// </summary>
internal sealed class Scenario
{
    public Scenario(string name, IReadOnlyList<ScenarioStep> steps)
    {
        Name = name;
        Steps = steps;
    }

    public string Name { get; }
    public IReadOnlyList<ScenarioStep> Steps { get; }
}

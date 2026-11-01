using System.Collections;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class WaitStep : ScenarioStep
{
    public WaitStep(float seconds)
    {
        Seconds = seconds;
    }

    public float Seconds { get; }

    public override IEnumerator Run(StepContext context)
    {
        yield return new WaitForSecondsRealtime(Seconds);
    }
}

using System.Collections;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Counts the level's Logic Blocks, whether a Script placed them or not: those of a type in the Block Manifest.
/// </summary>
internal sealed class ExpectBlocksStep : ScenarioStep
{
    public ExpectBlocksStep(int count)
    {
        Count = count;
    }

    public int Count { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        var count = 0;
        foreach (var block in central.undoRedo.allBlocksDictionary.Values)
        {
            if (block != null && BlockManifest.TryGetBlock(block.blockID, out _, out _))
                count++;
        }

        if (count == Count)
            context.Pass($"{count} Logic Blocks", Count.ToString(), count.ToString());
        else
            context.Fail($"{count} Logic Blocks", Count.ToString(), count.ToString());
    }
}

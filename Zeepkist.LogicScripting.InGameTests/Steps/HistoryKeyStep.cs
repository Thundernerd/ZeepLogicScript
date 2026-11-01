using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Presses the level editor's undo or redo key once, and fails when the editor's history didn't move, such as with
/// nothing left to undo.
/// </summary>
internal abstract class HistoryKeyStep : ScenarioStep
{
    private const float PressSeconds = 5;

    private readonly EditorKey _key;

    protected HistoryKeyStep(EditorKey key)
    {
        _key = key;
    }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        var before = central.undoRedo.currentHistoryPosition;
        EditorKeys.Press(_key);
        yield return context.WaitUntil(() => !EditorKeys.IsPressed, PressSeconds);
        if (EditorKeys.IsPressed)
        {
            EditorKeys.Release();
            context.Fail($"the level editor didn't read the key within {PressSeconds} seconds");
            yield break;
        }

        var after = central.undoRedo.currentHistoryPosition;
        if (after == before)
            context.Fail($"nothing to {_key.ToString().ToLowerInvariant()}, or the editor was busy");
        else
            context.Detail = $"history position {before} → {after}";
    }
}

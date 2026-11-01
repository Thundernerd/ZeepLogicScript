using System.Collections;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Places a block of a Logic Block type, by its Block Function's name, in its first style on the ground below the
/// camera, and selects it, as one undo step each like doing it by hand. A placed-only type, such as <c>lamp</c>, can be
/// placed too, so a Script can refer to it. With a name, the scenario's later <c>set-text</c> steps can write its UID
/// as <c>{uid:&lt;name&gt;}</c>.
/// </summary>
internal sealed class PlaceBlockStep : ScenarioStep
{
    private const string UndoLabel = "In-game test: place block";
    private const float GridSize = 16;

    public PlaceBlockStep(string function, string name = null)
    {
        Function = function;
        Name = name;
    }

    public string Function { get; }

    /// <summary>
    /// Null when the block isn't remembered.
    /// </summary>
    public string Name { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        var type = BlockManifest.All.FirstOrDefault(t => t.Function == Function);
        if (type == null)
        {
            context.Fail($"no Logic Block type is called '{Function}'");
            yield break;
        }

        // A cell to the side, so it's not where place-start puts the start block
        var camera = central.cam.cameraTransform;
        var position = new Vector3((Mathf.Round(camera.position.x / GridSize) + 1) * GridSize, 0, Mathf.Round(camera.position.z / GridSize) * GridSize);
        var blockId = type.DefaultStyle.BlockId;
        var json = new BlockPropertyJSON
        {
            i = blockId,
            u = PlayerManager.Instance.GenerateUniqueIDforBlocks(blockId.ToString()),
            p = new CV3(position),
            r = new CV3(Vector3.zero),
            s = new CV3(Vector3.one),
            d = new PropertyDictionariesJSON(),
        };

        var placed = BlockBatch.Apply(central, [json], [], [], UndoLabel);
        BlockGroups.Select(central, placed);
        if (Name != null)
            context.PlacedUids[Name] = json.u;

        context.Detail = $"{Function}, block {blockId}, UID \"{json.u}\" at {position}";
        yield return null;
    }
}

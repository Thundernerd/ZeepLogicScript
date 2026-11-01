using System.Collections;
using TNRD.Zeepkist.LogicScripting.Blocks;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Places a start block on the ground below the camera, facing the way it looks, so the soapbox faces the blocks a
/// Compile places in front of the camera. It's one undo step, like placing it by hand.
/// </summary>
internal sealed class PlaceStartStep : ScenarioStep
{
    private const string UndoLabel = "In-game test: place start";
    private const float GridSize = 16;
    private const float QuarterTurn = 90;

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        var blocks = central.manager.loader.globalBlockList.blocks;
        var startId = blocks.FindIndex(block => block != null && block.isStart);
        if (startId < 0)
        {
            context.Fail("the game has no start block");
            yield break;
        }

        var camera = central.cam.cameraTransform;
        var position = new Vector3(Mathf.Round(camera.position.x / GridSize) * GridSize, 0, Mathf.Round(camera.position.z / GridSize) * GridSize);
        var yaw = Mathf.Round(camera.eulerAngles.y / QuarterTurn) * QuarterTurn;
        var json = new BlockPropertyJSON
        {
            i = startId,
            u = PlayerManager.Instance.GenerateUniqueIDforBlocks(startId.ToString()),
            p = new CV3(position),
            r = new CV3(new Vector3(0, yaw, 0)),
            s = new CV3(Vector3.one),
            d = new PropertyDictionariesJSON(),
        };

        BlockBatch.Apply(central, [json], [], [], UndoLabel);
        context.Detail = $"block {startId} at {position}, facing {yaw}°";
        yield return null;
    }
}

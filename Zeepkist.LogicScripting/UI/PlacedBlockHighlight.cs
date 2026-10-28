using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Paints the block under a hovered Placed blocks row with the game's selection paint, and frames it with the camera.
/// </summary>
internal sealed class PlacedBlockHighlight
{
    private const float MinFrameDistance = 32;

    private BlockProperties _block;

    /// <summary>
    /// Null removes the highlight. Only paints on a change, because every paint makes new materials. Blocks in the
    /// selection already wear the paint and are left alone.
    /// </summary>
    public void Set(LEV_LevelEditorCentral central, BlockProperties block)
    {
        if (block == _block)
            return;

        if (central == null)
        {
            _block = null;
            return;
        }

        if (IsUnselected(central, _block))
            central.selection.RestorePaint(_block);

        _block = block;
        if (IsUnselected(central, _block))
            central.selection.SelectionPaint(_block);
    }

    private static bool IsUnselected(LEV_LevelEditorCentral central, BlockProperties block)
    {
        return block != null && !central.selection.list.Contains(block);
    }

    public static void Frame(LEV_LevelEditorCentral central, BlockProperties block)
    {
        var renderers = block.GetComponentsInChildren<Renderer>();
        var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(block.transform.position, Vector3.zero);
        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        var distance = Mathf.Max(MinFrameDistance, bounds.extents.magnitude * 3);
        central.cam.transform.position = bounds.center - (central.cam.cameraTransform.forward * distance);
    }
}

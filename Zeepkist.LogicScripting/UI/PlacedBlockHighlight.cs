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
    /// Moves the highlight to <paramref name="block"/>, or removes it for null. Only paints on a change: every paint
    /// makes new materials. Blocks in the selection already wear the paint and are left alone.
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

        if (_block != null && !central.selection.list.Contains(_block))
            central.selection.RestorePaint(_block);

        _block = block;
        if (_block != null && !central.selection.list.Contains(_block))
            central.selection.SelectionPaint(_block);
    }

    /// <summary>
    /// Moves the camera so the block is in front of it, keeping the camera's rotation and the selection.
    /// </summary>
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

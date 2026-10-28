using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Checks the Block Manifest against the game's blocks, and the game's block creation against the signature
/// <see cref="BlockBatch"/> calls (ADR 0007), so a game update turns off what it changed instead of breaking levels.
/// Runs once, on first entering the editor.
/// </summary>
internal sealed class StartupCheck
{
    public const string UpdateNeededMessage = "Logic Scripting needs an update for this game version";

    private readonly ManualLogSource _logger;
    private readonly HashSet<int> _disabledBlockIds = [];
    private readonly List<string> _disabledBlocks = [];
    private bool _hasRun;

    public StartupCheck(ManualLogSource logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Whether the game's block creation has the signature Compile calls. Without it, Compile still checks Scripts,
    /// but places nothing.
    /// </summary>
    public bool CanPlace { get; private set; } = true;

    /// <summary>
    /// Block IDs that don't match their manifest entry; a Script that places one gets an error.
    /// </summary>
    public IReadOnlyCollection<int> DisabledBlockIds => _disabledBlockIds;

    /// <summary>
    /// The turned-off blocks, as Block Function names (with the style, where a type has several).
    /// </summary>
    public IReadOnlyList<string> DisabledBlocks => _disabledBlocks;

    public void RunOnce()
    {
        if (_hasRun)
            return;

        _hasRun = true;
        try
        {
            CheckBlocks();
            CheckBlockCreation();
        }
        catch (Exception e)
        {
            // Without a finished check nothing is known to match, so nothing is placed
            CanPlace = false;
            _logger.LogError($"{UpdateNeededMessage}: the startup check failed: {e}");
            return;
        }

        if (CanPlace && _disabledBlockIds.Count == 0)
            _logger.LogInfo($"Startup check passed: all {BlockManifest.All.Sum(t => t.Styles.Count)} Logic Blocks match this game version");
    }

    private void CheckBlocks()
    {
        var prefabs = PlayerManager.Instance.loader.globalBlockList.blocks;
        foreach (var type in BlockManifest.All)
        {
            foreach (var style in type.Styles)
            {
                var problems = Compare(type, style, prefabs);
                if (problems.Count == 0)
                    continue;

                var name = type.Describe(style);
                _disabledBlockIds.Add(style.BlockId);
                _disabledBlocks.Add(name);
                _logger.LogWarning($"Block {style.BlockId} ({name}) doesn't match this game version and is turned off: {string.Join("; ", problems)}");
            }
        }
    }

    private static List<string> Compare(BlockType type, BlockStyle style, List<BlockProperties> prefabs)
    {
        if (style.BlockId < 0 || style.BlockId >= prefabs.Count || prefabs[style.BlockId] == null)
            return ["the game has no such block"];

        var prefab = prefabs[style.BlockId];
        if (prefab.blockID != style.BlockId)
            return [$"the game's block {style.BlockId} has ID {prefab.blockID}"];

        // By connection type, not hub class: the Trigger's output is a ConnectorHub_Template, not a ConnectorHub_Logic
        List<GameConnector> connectors = [];
        foreach (var hub in prefab.GetComponentsInChildren<ConnectorHub_Base>(true))
        {
            if (hub.connectionType == ConnectionTypeEnum.Logic_In)
                connectors.Add(new GameConnector(hub.GetConnectorID(), ConnectorDirection.Input));
            else if (hub.connectionType == ConnectionTypeEnum.Logic_Out)
                connectors.Add(new GameConnector(hub.GetConnectorID(), ConnectorDirection.Output));
        }

        return BlockManifestCheck.Compare(type, connectors);
    }

    private void CheckBlockCreation()
    {
        var method = typeof(LEV_UndoRedo).GetMethod(
            nameof(LEV_UndoRedo.GenerateNewBlock),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [typeof(BlockPropertyJSON), typeof(string)],
            null);

        CanPlace = method != null && method.ReturnType == typeof(BlockProperties);
        if (!CanPlace)
            _logger.LogError($"{UpdateNeededMessage}: LEV_UndoRedo.GenerateNewBlock(BlockPropertyJSON, string) changed, so Compile is turned off");
    }
}

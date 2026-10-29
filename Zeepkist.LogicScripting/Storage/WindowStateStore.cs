using BepInEx.Logging;
using ZeepSDK.Storage;

namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// Keeps the <see cref="WindowState"/> in the mod's storage folder, next to the Drafts.
/// </summary>
internal sealed class WindowStateStore
{
    private const string FileName = "window";

    private readonly IModStorage _storage;
    private readonly ManualLogSource _logger;

    public WindowStateStore(IModStorage storage, ManualLogSource logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// The saved state, or a fresh one (a closed, never placed window) when there's none or it can't be read.
    /// </summary>
    public WindowState Load()
    {
        if (!_storage.JsonFileExists(FileName))
            return new WindowState();

        var result = _storage.TryLoadFromJson<WindowState>(FileName);
        if (result.IsSuccess && result.Value != null)
        {
            // A null panel overrides the property's default, so a hand-edited file could otherwise break the window
            var state = result.Value;
            state.Scripts ??= new PanelState();
            state.Blocks ??= new PanelState();
            state.Errors ??= new PanelState();
            return state;
        }

        _logger.LogWarning($"Couldn't read the Script window's state: {result.Error} {result.Exception}");
        return new WindowState();
    }

    public void Save(WindowState state)
    {
        _storage.SaveToJson(FileName, state);
    }
}

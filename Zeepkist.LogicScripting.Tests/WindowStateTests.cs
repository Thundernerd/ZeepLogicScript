using System.Linq;
using System.Reflection;
using TNRD.Zeepkist.LogicScripting.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

/// <summary>
/// Checks every panel against <see cref="WindowState"/>'s panel list, so a panel missing from it fails here rather
/// than being skipped silently when copying, comparing and loading.
/// </summary>
public class WindowStateTests
{
    // Public only, as the non-public panel properties, such as the Level one, just read an old file into a public one
    private const BindingFlags PanelFlags = BindingFlags.Public | BindingFlags.Instance;

    [Theory]
    [MemberData(nameof(PanelNames))]
    public void ACopy_SharesNoPanelWithTheOriginal(string panelName)
    {
        var state = new WindowState();
        var copy = state.Copy();

        Assert.NotSame(Panel(state, panelName), Panel(copy, panelName));
    }

    [Theory]
    [MemberData(nameof(PanelNames))]
    public void ACopy_NoticesThePanelChanging(string panelName)
    {
        var state = new WindowState();
        var copy = state.Copy();

        Panel(state, panelName).ToggleCollapsed();

        Assert.False(state.IsSameAs(copy));
    }

    [Theory]
    [MemberData(nameof(PanelNames))]
    public void ReplacingNulls_GivesANullPanelTheDefaultLayout(string panelName)
    {
        var state = new WindowState();
        Property(panelName).SetValue(state, null);

        state.ReplaceNulls();

        Assert.True(Panel(state, panelName).IsSameAs(new PanelState()));
    }

    public static TheoryData<string> PanelNames()
    {
        var names = new TheoryData<string>();
        foreach (var property in typeof(WindowState).GetProperties(PanelFlags)
                     .Where(property => property.PropertyType == typeof(PanelState)))
        {
            names.Add(property.Name);
        }

        return names;
    }

    private static PanelState Panel(WindowState state, string panelName)
    {
        return (PanelState)Property(panelName).GetValue(state);
    }

    private static PropertyInfo Property(string panelName)
    {
        return typeof(WindowState).GetProperty(panelName, PanelFlags);
    }
}

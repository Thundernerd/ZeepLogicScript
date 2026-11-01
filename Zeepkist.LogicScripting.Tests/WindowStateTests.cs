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
    [Fact]
    public void TheWindow_HasPanels()
    {
        Assert.NotEmpty(Panels());
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void ACopy_SharesNoPanelWithTheOriginal(string panel)
    {
        var state = new WindowState();
        var copy = state.Copy();

        Assert.NotSame(Panel(state, panel), Panel(copy, panel));
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void ACopy_NoticesThePanelChanging(string panel)
    {
        var state = new WindowState();
        var copy = state.Copy();

        Panel(state, panel).ToggleCollapsed();

        Assert.False(state.IsSameAs(copy));
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void ReplacingNulls_GivesANullPanelTheDefaultLayout(string panel)
    {
        var state = new WindowState();
        Property(panel).SetValue(state, null);

        state.ReplaceNulls();

        Assert.True(Panel(state, panel).IsSameAs(new PanelState()));
    }

    /// <summary>
    /// The names of <see cref="WindowState"/>'s public <see cref="PanelState"/> properties.
    /// </summary>
    public static TheoryData<string> Panels()
    {
        var panels = new TheoryData<string>();
        foreach (var property in typeof(WindowState).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(property => property.PropertyType == typeof(PanelState)))
        {
            panels.Add(property.Name);
        }

        return panels;
    }

    private static PanelState Panel(WindowState state, string panel)
    {
        return (PanelState)Property(panel).GetValue(state);
    }

    private static PropertyInfo Property(string panel)
    {
        return typeof(WindowState).GetProperty(panel, BindingFlags.Public | BindingFlags.Instance);
    }
}

using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private bool _hasDefaultHide;
    private Setting _defaultHide;

    /// <summary>
    /// A <c>default</c> line; <c>hide:</c> is the only Setting it can give.
    /// </summary>
    private void VisitDefault(DefaultStatement statement)
    {
        var argument = statement.Setting;
        if (argument.Name != BlockSetting.Hide.Name)
        {
            Error(argument, $"Only {BlockSetting.Hide.Name}: has a default");
            return;
        }

        if (_hasDefaultHide)
        {
            Error(statement, $"default {argument.Name}: is given twice");
            return;
        }

        _hasDefaultHide = true;
        _defaultHide = GetSettingValues(BlockSetting.Hide, argument)?.Single();
    }

    /// <summary>
    /// Gives every block the <c>default</c> Settings it doesn't give itself; a call's own value wins.
    /// </summary>
    private void ApplyDefaults()
    {
        if (_defaultHide == null)
            return;

        foreach (var block in _graph.Blocks)
        {
            if (!block.HasSetting(_defaultHide.Key))
                block.AddSetting(_defaultHide);
        }
    }
}

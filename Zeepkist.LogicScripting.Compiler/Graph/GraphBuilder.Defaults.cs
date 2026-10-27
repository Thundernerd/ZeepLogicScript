using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// The only Setting a <c>default</c> line can give.
    /// </summary>
    private static readonly BlockSetting _defaultable = BlockSetting.Hide;

    private readonly HashSet<string> _givenDefaults = [];
    private Setting _defaultHide;

    private void VisitDefault(DefaultStatement statement)
    {
        var argument = statement.Setting;
        if (argument.Name != _defaultable.Name)
        {
            Error(argument, $"Only {_defaultable.Name}: has a default");
            return;
        }

        if (!_givenDefaults.Add(argument.Name))
        {
            Error(statement, $"default {argument.Name}: is given twice");
            return;
        }

        _defaultHide = GetSettingValue(_defaultable, argument);
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

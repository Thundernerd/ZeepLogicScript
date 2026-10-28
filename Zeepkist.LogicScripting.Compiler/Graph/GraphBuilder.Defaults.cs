using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private bool _hasDefaultHide;
    private Setting _defaultHide;

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

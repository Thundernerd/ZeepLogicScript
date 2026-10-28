using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// The state built up while checking a Block Function call's arguments: the wired inputs, the Settings,
    /// and the names given and meant. Reports the problems it finds with the names.
    /// </summary>
    private sealed class ArgumentCheck
    {
        private readonly Action<SyntaxNode, string> _error;
        private readonly HashSet<string> _givenNames = [];

        /// <summary>
        /// Names a misspelt name was probably meant as, already reported with "did you mean".
        /// </summary>
        private readonly HashSet<string> _meantNames = [];

        public ArgumentCheck(CallExpression call, BlockType type, Action<SyntaxNode, string> error)
        {
            Call = call;
            Type = type;
            Inputs = new WiredInput?[type.Inputs.Count];
            Style = type.DefaultStyle;
            _error = error;
        }

        public CallExpression Call { get; }
        public BlockType Type { get; }
        public WiredInput?[] Inputs { get; }
        /// <summary>
        /// The stored Settings per Setting given; several for a Toggler's targets.
        /// </summary>
        public Dictionary<BlockSetting, IReadOnlyList<Setting>> Settings { get; } = [];

        /// <summary>
        /// The style the call picked, or the type's default.
        /// </summary>
        public BlockStyle Style { get; private set; }

        public bool Failed { get; private set; }

        /// <summary>
        /// Reports a problem with the call and fails.
        /// </summary>
        public void ReportAndFail(SyntaxNode node, string message)
        {
            _error(node, message);
            Failed = true;
        }

        /// <summary>
        /// Fails without reporting, for a problem already reported elsewhere.
        /// </summary>
        public void MarkFailed()
        {
            Failed = true;
        }

        /// <summary>
        /// Adds a named argument's name to the names given; reports it when the call gives it twice.
        /// </summary>
        public bool TryGive(NamedArgument argument)
        {
            if (_givenNames.Add(argument.Name))
                return true;

            ReportAndFail(argument, $"'{argument.Name}' is given twice");
            return false;
        }

        public void WireByPosition(int input, InputFeed? feed)
        {
            Inputs[input] = WiredInput.ByPosition(feed);
        }

        public void WireByName(NamedArgument argument, int input, InputFeed? feed)
        {
            if (Inputs[input]?.IsByPosition == true)
            {
                ReportAndFail(argument, $"Input '{argument.Name}' is already wired by position");
                return;
            }

            Inputs[input] = WiredInput.ByName(feed);
        }

        /// <summary>
        /// Gives a Setting its value; fails when the value was invalid and already reported.
        /// </summary>
        public void GiveSetting(BlockSetting blockSetting, Setting setting)
        {
            GiveSettings(blockSetting, setting == null ? null : [setting]);
        }

        /// <summary>
        /// Gives a Setting stored under several keys; fails when the value was invalid and already reported.
        /// </summary>
        public void GiveSettings(BlockSetting blockSetting, IReadOnlyList<Setting> settings)
        {
            if (settings == null)
                MarkFailed();
            else
                Settings.Add(blockSetting, settings);
        }

        /// <summary>
        /// Picks the block the call places; fails when the style was invalid and already reported.
        /// </summary>
        public void ChooseStyle(BlockStyle style)
        {
            if (style == null)
                MarkFailed();
            else
                Style = style;
        }

        /// <summary>
        /// Reports a name that is neither an input nor a Setting, with the closest one it was probably meant as.
        /// </summary>
        public void ReportUnknownName(NamedArgument argument)
        {
            var message = $"'{Call.Function}' has no input or Setting '{argument.Name}'";
            var closest = Type.FindClosestName(argument.Name);
            if (closest != null)
            {
                message += $"; did you mean {closest}?";
                _meantNames.Add(closest);
            }

            ReportAndFail(argument, message);
        }

        /// <summary>
        /// Reports each required Setting the call leaves out. One a misspelt name was probably meant as has
        /// already been reported.
        /// </summary>
        public void CheckRequiredSettings()
        {
            foreach (var blockSetting in Type.Settings)
            {
                if (blockSetting.IsRequired && !IsGivenOrMeant(blockSetting.Name))
                {
                    ReportAndFail(Call, blockSetting.Type == SettingType.Targets
                        ? $"'{Call.Function}' needs {blockSetting.Name}:, 1 to {BlockSetting.MaxTargets} placed blocks to switch, like {blockSetting.Name}: [door_1, door_2]"
                        : $"'{Call.Function}' needs {blockSetting.Name}:, like {blockSetting.Name}: 1");
                }
            }
        }

        private bool IsGivenOrMeant(string name)
        {
            return _givenNames.Contains(name) || _meantNames.Contains(name);
        }
    }
}

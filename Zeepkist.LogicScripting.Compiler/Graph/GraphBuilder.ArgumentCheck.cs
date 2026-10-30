using System;
using System.Collections.Generic;
using System.Globalization;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
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

        public BlockStyle Style { get; private set; }

        public bool Failed { get; private set; }

        public void ReportAndFail(SyntaxNode node, string message)
        {
            _error(node, message);
            Failed = true;
        }

        /// <summary>
        /// For a problem already reported elsewhere.
        /// </summary>
        public void MarkFailed()
        {
            Failed = true;
        }

        public bool TryGive(NamedArgument argument)
        {
            if (_givenNames.Add(argument.Name))
                return true;

            ReportAndFail(argument, $"'{argument.Name}' is given twice");
            return false;
        }

        public void WireByPosition(int input, InputFeed feed)
        {
            Inputs[input] = WiredInput.ByPosition(feed);
        }

        public void WireByName(NamedArgument argument, int input, InputFeed feed)
        {
            if (Inputs[input]?.IsByPosition == true)
            {
                ReportAndFail(argument, $"Input '{argument.Name}' is already wired by position");
                return;
            }

            Inputs[input] = WiredInput.ByName(feed);
        }

        /// <summary>
        /// Null <paramref name="settings"/> means the value was invalid and already reported.
        /// </summary>
        public void GiveSettings(BlockSetting blockSetting, IReadOnlyList<Setting> settings)
        {
            if (settings == null)
                MarkFailed();
            else
                Settings.Add(blockSetting, settings);
        }

        /// <summary>
        /// A null <paramref name="style"/> was invalid and already reported.
        /// </summary>
        public void ChooseStyle(BlockStyle style)
        {
            if (style == null)
                MarkFailed();
            else
                Style = style;
        }

        public void ReportUnknownName(NamedArgument argument)
        {
            var message = $"'{Call.Function}' has no input or Setting '{argument.Name}'";
            var closest = Type.FindClosestName(argument.Name);
            if (closest != null)
            {
                message += $". Did you mean {closest}?";
                _meantNames.Add(closest);
            }

            ReportAndFail(argument, message);
        }

        /// <summary>
        /// Skips a required Setting that a misspelt name was probably meant as; that name was already reported.
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

        /// <summary>
        /// Reports a <see cref="BlockType.Range"/> whose minimum is above its maximum, unless the input that replaces
        /// the maximum is wired, or a misspelt name was probably meant as it. An omitted Setting counts as 0, the
        /// fresh block's value; one whose value was invalid, or a misspelt name was probably meant as, has already
        /// been reported.
        /// </summary>
        public void CheckRange()
        {
            var range = Type.Range;
            if (range == null || Inputs[Type.IndexOfInput(range.MaximumInput)] != null || IsGivenOrMeant(range.MaximumInput))
                return;

            if (!TryGetNumber(range.Minimum, out var minimum) || !TryGetNumber(range.Maximum, out var maximum))
                return;

            if (minimum <= maximum)
                return;

            var maximumArgument = FindArgument(range.Maximum.Name);
            if (maximumArgument != null)
            {
                ReportAndFail(maximumArgument.Value, $"{range.Maximum.Name} must be at least {range.Minimum.Name} ({minimum})");
            }
            else
            {
                ReportAndFail(
                    FindArgument(range.Minimum.Name).Value,
                    $"{range.Minimum.Name} must be at most {range.Maximum.Name}, which is 0 unless given");
            }
        }

        /// <summary>
        /// A Number Setting's value, 0 when the call leaves it out; false when it was given or meant but has no value.
        /// </summary>
        private bool TryGetNumber(BlockSetting blockSetting, out long value)
        {
            value = 0;
            if (Settings.TryGetValue(blockSetting, out var stored))
                return long.TryParse(stored[0].TextValue, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);

            return !IsGivenOrMeant(blockSetting.Name);
        }

        private NamedArgument FindArgument(string name)
        {
            foreach (var argument in Call.NamedArguments)
            {
                if (argument.Name == name)
                    return argument;
            }

            return null;
        }

        private bool IsGivenOrMeant(string name)
        {
            return _givenNames.Contains(name) || _meantNames.Contains(name);
        }
    }
}

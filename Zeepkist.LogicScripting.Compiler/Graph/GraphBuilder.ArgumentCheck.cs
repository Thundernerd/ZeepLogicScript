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
        private readonly List<Diagnostic> _diagnostics;
        private readonly HashSet<string> _givenNames = [];

        /// <summary>
        /// Names a misspelt name was probably meant as, already reported with "did you mean".
        /// </summary>
        private readonly HashSet<string> _meantNames = [];

        public ArgumentCheck(CallExpression call, BlockType type, List<Diagnostic> diagnostics)
        {
            Call = call;
            Type = type;
            Inputs = new WiredInput?[type.Inputs.Count];
            _diagnostics = diagnostics;
        }

        public CallExpression Call { get; }
        public BlockType Type { get; }
        public WiredInput?[] Inputs { get; }
        public Dictionary<BlockSetting, Setting> Settings { get; } = [];
        public bool Failed { get; set; }

        public void Fail(SyntaxNode node, string message)
        {
            _diagnostics.Add(new Diagnostic(node.Position, message));
            Failed = true;
        }

        /// <summary>
        /// Notes a named argument's name; false, and reported, when the call already gives it.
        /// </summary>
        public bool Give(NamedArgument argument)
        {
            if (_givenNames.Add(argument.Name))
                return true;

            Fail(argument, $"'{argument.Name}' is given twice");
            return false;
        }

        public void WireByName(NamedArgument argument, int input, InputFeed? feed)
        {
            if (Inputs[input]?.IsByPosition == true)
            {
                Fail(argument, $"Input '{argument.Name}' is already wired by position");
                return;
            }

            Inputs[input] = WiredInput.ByName(feed);
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

            Fail(argument, message);
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
                    Fail(Call, $"'{Call.Function}' needs {blockSetting.Name}:, like {blockSetting.Name}: 1");
                }
            }
        }

        private bool IsGivenOrMeant(string name)
        {
            return _givenNames.Contains(name) || _meantNames.Contains(name);
        }
    }
}

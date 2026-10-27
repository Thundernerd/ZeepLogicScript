using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// The state built up while checking a Block Function call's arguments: the input feeds, which inputs are
    /// wired by position, the Settings, and the names given and meant.
    /// </summary>
    private sealed class CallArguments
    {
        public CallArguments(CallExpression call, BlockType type)
        {
            Call = call;
            Type = type;
            Inputs = new InputFeed?[type.Inputs.Count];
            WiredByPosition = new bool[type.Inputs.Count];
        }

        public CallExpression Call { get; }
        public BlockType Type { get; }
        public InputFeed?[] Inputs { get; }
        public bool[] WiredByPosition { get; }
        public Dictionary<BlockSetting, Setting> Settings { get; } = [];
        public HashSet<string> GivenNames { get; } = [];

        /// <summary>
        /// Names a misspelt name was probably meant as, already reported with "did you mean".
        /// </summary>
        public HashSet<string> MeantNames { get; } = [];

        public bool Failed { get; set; }

        public bool IsGivenOrMeant(string name)
        {
            return GivenNames.Contains(name) || MeantNames.Contains(name);
        }
    }
}

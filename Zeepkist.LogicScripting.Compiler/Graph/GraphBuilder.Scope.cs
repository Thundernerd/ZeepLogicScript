using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// The names one part of the Script can see: the top level, or one expansion of a Function's body, which sees only
    /// its inputs and its own definitions. Each follows ADR 0002 on its own.
    /// </summary>
    private sealed class Scope
    {
        public Scope(FunctionDefinition function)
        {
            Function = function;
        }

        /// <summary>
        /// Null for the top level.
        /// </summary>
        public FunctionDefinition Function { get; }

        /// <summary>
        /// The first definition of each name, noted up front, so a use can tell a later definition from no definition.
        /// </summary>
        public Dictionary<string, Definition> Definitions { get; } = [];

        public Dictionary<string, Resolved> Names { get; } = [];
        public HashSet<string> FailedNames { get; } = [];

        public Definition Current { get; set; }

        /// <summary>
        /// How many <c>delay(...)</c> calls the expression being visited is inside.
        /// </summary>
        public int DelayDepth { get; set; }
    }
}

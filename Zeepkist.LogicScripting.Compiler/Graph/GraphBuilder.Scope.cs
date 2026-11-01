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
        public Scope(FunctionDefinition function, LoadedLibrary library, Position? callSite)
        {
            Function = function;
            Library = library;
            CallSite = callSite;
        }

        /// <summary>
        /// Null for the top level.
        /// </summary>
        public FunctionDefinition Function { get; }

        /// <summary>
        /// The Library the body's text comes from, or null for the text being compiled.
        /// </summary>
        public LoadedLibrary Library { get; }

        /// <summary>
        /// Where the text being compiled calls into the Library this scope is in, to report the body's errors at, since
        /// their lines are the Library's. Null outside a Library's Function.
        /// </summary>
        public Position? CallSite { get; }

        /// <summary>
        /// The first definition of each name, noted up front, so a use can tell a later definition from no definition.
        /// </summary>
        public Dictionary<string, Definition> Definitions { get; } = [];

        public Dictionary<string, Resolved> Names { get; } = [];
        public HashSet<string> FailedNames { get; } = [];

        /// <summary>
        /// The constant the call passed to each of the Function's Setting inputs. A Setting input that failed is in
        /// <see cref="FailedNames"/> instead.
        /// </summary>
        public Dictionary<string, SettingConstant> SettingConstants { get; } = [];

        public Definition Current { get; set; }

        /// <summary>
        /// How many <c>delay(...)</c> calls the expression being visited is inside.
        /// </summary>
        public int DelayDepth { get; set; }
    }
}

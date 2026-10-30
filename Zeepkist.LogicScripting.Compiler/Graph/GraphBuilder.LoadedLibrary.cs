using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// A Library's text, parsed once per Compile, and its Functions.
    /// </summary>
    private sealed class LoadedLibrary
    {
        public LoadedLibrary(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public Dictionary<string, FunctionDefinition> Functions { get; } = [];

        /// <summary>
        /// What's wrong in the text outside any one call, at the Library's own lines. Any of them stops every call into
        /// it.
        /// </summary>
        public List<Diagnostic> Errors { get; } = [];
    }
}

using System;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed partial class Parser
{
    private sealed class SyntaxError : Exception
    {
        public SyntaxError(Diagnostic diagnostic, bool continuesOnNextLine)
            : base(diagnostic.Message)
        {
            Diagnostic = diagnostic;
            ContinuesOnNextLine = continuesOnNextLine;
        }

        public Diagnostic Diagnostic { get; }

        /// <summary>
        /// Whether the statement broke off mid-expression at a line break, so the next line likely continues it.
        /// </summary>
        public bool ContinuesOnNextLine { get; }
    }
}

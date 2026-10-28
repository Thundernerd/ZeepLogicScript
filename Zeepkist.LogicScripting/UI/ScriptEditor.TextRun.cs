using TNRD.Zeepkist.LogicScripting.Compiler.Editing;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptEditor
{
    /// <summary>
    /// A piece of one line drawn in one color.
    /// </summary>
    private readonly struct TextRun
    {
        public TextRun(int line, int offset, string text, HighlightKind? kind)
        {
            Line = line;
            Offset = offset;
            Text = text;
            Kind = kind;
        }

        public int Line { get; }

        /// <summary>
        /// Where the piece starts on its line, in characters.
        /// </summary>
        public int Offset { get; }

        public string Text { get; }

        /// <summary>
        /// Null for text the lexer gave no kind.
        /// </summary>
        public HighlightKind? Kind { get; }
    }
}

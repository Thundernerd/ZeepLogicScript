using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What a call passed to an input its Function's body uses as a Setting: a number or a word, checked once it's
    /// substituted into the Setting.
    /// </summary>
    private sealed class SettingConstant
    {
        public SettingConstant(Expression value, Position valuePosition, Scope scope)
        {
            Value = value;
            ValuePosition = valuePosition;
            Scope = scope;
        }

        public Expression Value { get; }
        public Position ValuePosition { get; }

        /// <summary>
        /// Where the constant was written, to report a wrong value there rather than at the body's Setting.
        /// </summary>
        public Scope Scope { get; }
    }
}

using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// A name inside <c>delay(...)</c> that isn't defined yet, to look up in its own scope once every definition has
    /// been visited.
    /// </summary>
    private sealed class DeferredValue : InputFeed
    {
        public DeferredValue(Expression expression, Scope scope)
            : this(expression, scope, [])
        {
        }

        private DeferredValue(Expression expression, Scope scope, IReadOnlyList<MemberExpression> members)
        {
            Expression = expression;
            Scope = scope;
            Members = members;
        }

        public Expression Expression { get; }
        public Scope Scope { get; }

        /// <summary>
        /// The outputs a Function's body picks from the value, in order, once it's looked up. They stay the body's
        /// own expressions, so an error names the body's input rather than the caller's argument.
        /// </summary>
        public IReadOnlyList<MemberExpression> Members { get; }

        /// <summary>
        /// Where to report a value that isn't one output: the last output picked from it, or the name itself.
        /// </summary>
        public Expression Origin => Members.Count > 0 ? Members[Members.Count - 1] : Expression;

        public DeferredValue WithMember(MemberExpression member)
        {
            return new DeferredValue(Expression, Scope, [.. Members, member]);
        }

        public override Resolved SelectMember(GraphBuilder builder, MemberExpression member)
        {
            return WithMember(member);
        }

        public override void Connect(GraphBuilder builder, BlockNode to, int toConnector)
        {
            builder._deferredWires.Add(new DeferredWire(this, to, toConnector));
        }
    }
}

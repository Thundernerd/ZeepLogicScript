using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// The Setting-input analysis walks <see cref="Expression.Children"/> instead of each kind of expression, so a kind
/// whose children leave out one of its sub-expressions would hide an input used as a Setting inside it.
/// </summary>
public class ExpressionChildrenTests
{
    private const string EveryKind = "x = a.b + !f(1, [c], \"d\", s: e)\n";

    [Fact]
    public void Sample_HasEveryKindOfExpression()
    {
        var kinds = typeof(Expression).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Expression)) && !t.IsAbstract)
            .Select(t => t.Name);

        Assert.Equal(kinds.OrderBy(k => k), ParseSample().Select(e => e.GetType().Name).Distinct().OrderBy(k => k));
    }

    [Fact]
    public void Children_AreEverySubExpression()
    {
        foreach (var expression in ParseSample())
        {
            Assert.Equal(GetSubExpressions(expression), expression.Children);
        }
    }

    private static List<Expression> ParseSample()
    {
        var diagnostics = new List<Diagnostic>();
        var statements = Parser.Parse(Lexer.Tokenize(EveryKind, diagnostics), diagnostics);
        Assert.Empty(diagnostics);

        var expressions = new List<Expression>();
        var pending = new Stack<Expression>([((Definition)Assert.Single(statements)).Value]);
        while (pending.Count > 0)
        {
            var expression = pending.Pop();
            expressions.Add(expression);
            foreach (var child in GetSubExpressions(expression))
            {
                pending.Push(child);
            }
        }

        return expressions;
    }

    /// <summary>
    /// Every expression a kind holds in its own properties, found without knowing the kind.
    /// </summary>
    private static List<Expression> GetSubExpressions(Expression expression)
    {
        var subExpressions = new List<Expression>();
        foreach (var property in expression.GetType().GetProperties().Where(p => p.DeclaringType != typeof(Expression)))
        {
            switch (property.GetValue(expression))
            {
                case Expression child:
                    subExpressions.Add(child);
                    break;
                case IEnumerable<Expression> children:
                    subExpressions.AddRange(children);
                    break;
                case IEnumerable<NamedArgument> arguments:
                    subExpressions.AddRange(arguments.Select(a => a.Value));
                    break;
            }
        }

        return subExpressions;
    }
}

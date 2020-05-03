using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler
{
    /// <summary>
    /// A syntax-sugar feloldásával foglalkozik. Jelenleg ilyenek az összetett operátorok.
    /// Példa: x += y -ból x = x + y -t készítünk.
    /// </summary>
    public static class Desugaring
    {
        /// <summary>
        /// Átalakítja az adott szintaxisfát, hogy ne forduljon benne elő syntax-sugar.
        /// </summary>
        /// <param name="node">Az átalakítandó szintaxisfa.</param>
        /// <returns>Az átalakított, syntax-sugar mentes fa.</returns>
        public static AstNode Desugar(AstNode node)
        {
            switch (node)
            {
                case Statement stmt: return DesugarStatement(stmt);
                case Expression expr: return DesugarExpression(expr);
                default: throw new NotImplementedException();
            }
        }

        private static Statement DesugarStatement(Statement stmt)
        {
            switch (stmt)
            {
                case IfStatement s: return new IfStatement
                {
                    Condition = DesugarExpression(s.Condition),
                    Then = DesugarStatement(s.Then),
                    Else = DesugarStatement(s.Else),
                };
                case WhileStatement s: return new WhileStatement
                {
                    Condition = DesugarExpression(s.Condition),
                    Body = DesugarStatement(s.Body),
                };
                case FunctionDefinitionStatement s: return new FunctionDefinitionStatement
                {
                    Name = s.Name,
                    Parameters = s.Parameters,
                    Body = DesugarStatement(s.Body),
                };
                case ReturnStatement s: return new ReturnStatement
                {
                    Value = s.Value == null ? null : DesugarExpression(s.Value),
                };
                case VarDefinitionStatement s: return new VarDefinitionStatement
                {
                    Name = s.Name,
                    Value = DesugarExpression(s.Value),
                };
                case CompoundStatement s: return new CompoundStatement
                {
                    Statements = s.Statements.Select(x => DesugarStatement(x)).ToList(),
                };
                case ExpressionStatement s: return new ExpressionStatement
                {
                    Expression = DesugarExpression(s.Expression),
                };
                default: throw new NotImplementedException();
            }
        }

        private static Expression DesugarExpression(Expression expr)
        {
            switch (expr)
            {
                // Levélelemek
                case IntegerLiteralExpression _:
                case BoolLiteralExpression _:
                case StringLiteralExpression _:
                case VariableExpression _: 
                    return expr;

                case UnaryExpression e: return new UnaryExpression
                {
                    Operator = e.Operator,
                    Subexpression = DesugarExpression(e.Subexpression),
                };

                case BinaryExpression e:
                {
                    // Itt van az érdekesség, ha az operátor összetett
                    TokenType? newOperator = null;
                    switch (e.Operator.Type)
                    {
                        case TokenType.AddAssign: newOperator = TokenType.Add; break;
                        case TokenType.SubtractAssign: newOperator = TokenType.Subtract; break;
                        case TokenType.MultiplyAssign: newOperator = TokenType.Multiply; break;
                        case TokenType.DivideAssign: newOperator = TokenType.Divide; break;
                        case TokenType.ModuloAssign: newOperator = TokenType.Modulo; break;
                    }

                    if (newOperator == null)
                    {
                        // Nincs syntax sugar
                        return new BinaryExpression
                        {
                            Operator = e.Operator,
                            Left = DesugarExpression(e.Left),
                            Right = DesugarExpression(e.Right),
                        };
                    }
                    else
                    {
                        // Van syntax sugar, ketté kell bontanunk a tokeneket
                        var opToken = new Token
                        {
                            Source = e.Operator.Source,
                            Position = e.Operator.Position,
                            Type = newOperator.Value,
                            Value = $"{e.Operator.Value[0]}",
                        };
                        var assignToken = new Token
                        {
                            Source = e.Operator.Source,
                            Position = e.Operator.Position,
                            Type = TokenType.Assign,
                            Value = "=",
                        };
                        /*
                         Ebből:

                                +=
                               /  \
                              x    y

                         Ezt csináljuk:

                                 =
                                / \
                               x   +
                                  / \
                                 x   y
                         */
                        return new BinaryExpression
                        {
                            Operator = assignToken,
                            Left = DesugarExpression(e.Left),
                            Right = new BinaryExpression
                            {
                                Operator = opToken,
                                Left = DesugarExpression(e.Left),
                                Right = DesugarExpression(e.Right),
                            },
                        };
                    }
                }

                case CallExpression e: return new CallExpression
                {
                    Function = DesugarExpression(e.Function),
                    Arguments = e.Arguments.Select(x => DesugarExpression(x)).ToList(),
                };

                default: throw new NotImplementedException();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler
{
    /// <summary>
    /// A syntax-sugar feloldásával foglalkozik. Jelenleg ilyenek az összetett operátorok, vagy a for ciklus.
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
                case ForStatement s:
                {
                    /*
                     Ebből:
                     
                     for i x, y { println(); }

                     Ezt készítjük:

                     {
                         // Egyszeri kiértékelés
                         var `for.from` = x;
                         var `for.to` = y;
                         var i = `for.from`;
                         if i < `for.to` {
                             while i <= `for.to` {
                                 println();
                                 i += 1;
                             }
                         }
                         else {
                             while i >= `for.to` {
                                 println();
                                 i -= 1;
                             }
                         }
                     }

                     Fontos: A nem felhasználó által definiált változóknak olyan nevet adtunk,
                     amit amúgy a felhasználó nem adhat. Ezzel elkerüljük a ritka, de lehetséges
                     névütközéseket a felhasználó által definiált változókkal.
                     */

                    Func<TokenType, string, Token> FakeToken = (tt, val) => new Token
                    {
                        // TODO: Itt csalunk, nem helyes helyadatok
                        Source = s.Counter.Source,
                        Position = s.Counter.Position,
                        Type = tt,
                        Value = val,
                    };
                    var fromToken = FakeToken(TokenType.Identifier, "for.from");
                    var toToken = FakeToken(TokenType.Identifier, "for.to");
                    var lessToken = FakeToken(TokenType.Less, "<");
                    var lessEqToken = FakeToken(TokenType.LessOrEqual, "<=");
                    var greaterToken = FakeToken(TokenType.GreaterOrEqual, ">");
                    var addAssignToken = FakeToken(TokenType.AddAssign, "+=");
                    var subAssignToken = FakeToken(TokenType.SubtractAssign, "-=");
                    var oneToken = FakeToken(TokenType.Integer, "1");

                    // Az egyszerűség kedvéért az egészet mégegyszer átküldjük desugaring-on, hogy használhassunk
                    // például összetett operátorokat.
                    return DesugarStatement(new CompoundStatement { Statements = new List<Statement> 
                    { 
                        new VarDefinitionStatement{ Name = fromToken, Value = s.From },
                        new VarDefinitionStatement{ Name = toToken, Value = s.To },
                        new VarDefinitionStatement{ Name = s.Counter, Value = new VariableExpression { Identifier = fromToken } },
                        new IfStatement
                        {
                            Condition = new BinaryExpression
                            { 
                                Operator = lessEqToken, 
                                Left = new VariableExpression { Identifier = s.Counter },
                                Right = new VariableExpression{ Identifier = toToken },
                            },
                            Then = new WhileStatement
                            {
                                Condition = new BinaryExpression
                                {
                                    Operator = lessToken,
                                    Left = new VariableExpression { Identifier = s.Counter },
                                    Right = new VariableExpression{ Identifier = toToken },
                                },
                                Body = new CompoundStatement{ Statements = new List<Statement>
                                {
                                    s.Body,
                                    new ExpressionStatement{ Expression = new BinaryExpression
                                    {
                                        Operator = addAssignToken,
                                        Left = new VariableExpression{ Identifier = s.Counter },
                                        Right = new IntegerLiteralExpression{ Value = oneToken },
                                    }},
                                }},
                            },
                            Else = new WhileStatement
                            {
                                Condition = new BinaryExpression
                                {
                                    Operator = greaterToken,
                                    Left = new VariableExpression { Identifier = s.Counter },
                                    Right = new VariableExpression{ Identifier = toToken },
                                },
                                Body = new CompoundStatement{ Statements = new List<Statement>
                                {
                                    s.Body,
                                    new ExpressionStatement{ Expression = new BinaryExpression
                                    {
                                        Operator = subAssignToken,
                                        Left = new VariableExpression{ Identifier = s.Counter },
                                        Right = new IntegerLiteralExpression{ Value = oneToken },
                                    }},
                                }},
                            },
                        }
                    } });
                }
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities;

namespace Compiler
{
    /// <summary>
    /// Egy precedencia szint jellemzése bináris matematikai kijezezések elemzéséhez.
    /// </summary>
    public struct Precedence
    {
        public HashSet<TokenType> Operators { get; set; }
        public Associativity Associativity { get; set; }

        public static Precedence Left(params TokenType[] operators) =>
            new Precedence { Associativity = Associativity.Left, Operators = operators.ToHashSet() };

        public static Precedence Right(params TokenType[] operators) =>
            new Precedence { Associativity = Associativity.Right, Operators = operators.ToHashSet() };
    }

    /// <summary>
    /// Egy bináris operátor asszociativitása.
    /// </summary>
    public enum Associativity
    {
        /// <summary>
        /// Bal-asszociáció, például '1 + 2 + 3' == '(1 + 2) + 3'.
        /// </summary>
        Left,
        /// <summary>
        /// Jobb-asszociáció, például 'x = y = z' == 'x = (y = z)'.
        /// </summary>
        Right,
    }

    /// <summary>
    /// A matematikai kifejezések elemzése külön állatfaj, több módszert dolgoztak ki hozzá.
    /// Mi egy egyszerű, metaadat-vezérelt verziót implementálunk. Ezzel igen könnyű további
    /// operátorokkal bővíteni az elemzőt.
    /// </summary>
    public static class ExpressionParser
    {
        private static Precedence[] PrecedenceTable = new Precedence[]
        {
            Precedence.Right(TokenType.Assign),
            Precedence.Left(TokenType.Or),
            Precedence.Left(TokenType.And),
            Precedence.Left(TokenType.Equal, TokenType.NotEqual),
            Precedence.Left(TokenType.Greater, TokenType.GreaterOrEqual, TokenType.Less, TokenType.LessOrEqual),
            Precedence.Left(TokenType.Add, TokenType.Subtract),
            Precedence.Left(TokenType.Multiply, TokenType.Divide, TokenType.Modulo),
        };

        private static HashSet<TokenType> PrefixOperators = new HashSet<TokenType> 
        { 
            TokenType.Add, TokenType.Subtract, TokenType.Not,
        };

        /// <summary>
        /// Kielemez egy matematikai kifejezést.
        /// </summary>
        /// <param name="input">A kielemezentő token nézet.</param>
        /// <param name="result">A kielemzett kifejezés.</param>
        /// <returns>A token nézetet, amiben a kielemzett tokenek már nincsenek benne.</returns>
        public static TokenView Parse(TokenView input, out Expression result)
        {
            return ParseBinary(input, out result, 0);
        }

        private static TokenView ParseBinary(TokenView input, out Expression result, int precedence)
        {
            if (precedence >= PrecedenceTable.Length)
            {
                return ParsePrefix(input, out result);
            }

            var desc = PrecedenceTable[precedence];
            input = ParseBinary(input, out var left, precedence + 1);

            if (desc.Associativity == Associativity.Left)
            {
                result = left;
                while (true)
                {
                    var op = input.Peek();
                    if (desc.Operators.Contains(op.Type))
                    {
                        input = input.Consume();
                        input = ParseBinary(input, out var right, precedence + 1);
                        result = new BinaryExpression { Operator = op, Left = result, Right = right };
                    }
                    else
                    {
                        break;
                    }
                }
                return input;
            }
            else
            {
                var op = input.Peek();
                if (desc.Operators.Contains(op.Type))
                {
                    input = input.Consume();
                    input = ParseBinary(input, out var right, precedence);
                    result = new BinaryExpression { Operator = op, Left = left, Right = right };
                    return input;
                }
                else
                {
                    result = left;
                    return input;
                }
            }
        }

        private static TokenView ParsePrefix(TokenView input, out Expression result)
        {
            var op = input.Peek();
            if (PrefixOperators.Contains(op.Type))
            {
                input = input.Consume();
                input = ParsePrefix(input, out var sub);
                result = new UnaryExpression { Operator = op, Subexpression = sub };
                return input;
            }
            else
            {
                return ParsePostfix(input, out result);
            }
        }

        private static TokenView ParsePostfix(TokenView input, out Expression result)
        {
            input = ParseAtomic(input, out var sub);
            var peek = input.Peek();
            if (peek.Type == TokenType.OpenParen)
            {
                // Call expression
                input = input.Consume();
                var args = new List<Expression>();
                if (input.Peek().Type != TokenType.CloseParen)
                {
                    input = Parse(input, out var arg1);
                    args.Add(arg1);
                    while (input.Peek().Type == TokenType.Comma)
                    {
                        input = input.Consume();
                        input = Parse(input, out var argn);
                        args.Add(argn);
                    }
                }
                input = input.Expect(TokenType.CloseParen);
                result = new CallExpression { Function = sub, Arguments = args };
                return input;
            }
            else
            {
                result = sub;
                return input;
            }
        }

        private static TokenView ParseAtomic(TokenView input, out Expression result)
        {
            var peek = input.Peek();
            if (peek.Type == TokenType.OpenParen)
            {
                input = input.Consume();
                input = Parse(input, out var sub);
                input = input.Expect(TokenType.CloseParen);
                result = sub;
                return input;
            }
            if (peek.Type == TokenType.Identifier)
            {
                input = input.Consume();
                result = new VariableExpression { Identifier = peek };
                return input;
            }
            if (peek.Type == TokenType.Integer)
            {
                input = input.Consume();
                result = new IntegerLiteralExpression { Value = int.Parse(peek.Value) };
                return input;
            }
            if (peek.Type == TokenType.KwTrue || peek.Type == TokenType.KwFalse)
            {
                input = input.Consume();
                result = new BoolLiteralExpression { Value = peek.Type == TokenType.KwTrue };
                return input;
            }
            throw new UnexpectedTokenError(peek);
        }
    }

    public class Parser
    {
        /// <summary>
        /// Kielemzi a programot egy összetett utasításba.
        /// </summary>
        /// <param name="tokens">A kielemezendő tokenek listája.</param>
        /// <returns>A kielemzett program szintaxisfája.</returns>
        public static Statement ParseProgram(List<Token> tokens)
        {
            var input = new TokenView(tokens);
            var statements = new List<Statement>();
            while (input.Peek().Type != TokenType.EndOfSource)
            {
                input = ParseStatement(input, out var stmt);
                statements.Add(stmt);
            }
            return new CompoundStatement { Statements = statements };
        }

        private static TokenView ParseStatement(TokenView input, out Statement result)
        {
            var peek = input.Peek();
            if (peek.Type == TokenType.OpenBrace)
            {
                return ParseBlockStatement(input, out result);
            }
            if (peek.Type == TokenType.KwFunction)
            {
                return ParseFunctionDefinitionStatement(input, out result);
            }
            if (peek.Type == TokenType.KwIf)
            {
                return ParseIfStatement(input, out result);
            }
            if (peek.Type == TokenType.KwWhile)
            {
                return ParseWhileStatement(input, out result);
            }
            if (peek.Type == TokenType.KwVar)
            {
                return ParseVarStatement(input, out result);
            }
            if (peek.Type == TokenType.KwReturn)
            {
                return ParseReturnStatement(input, out result);
            }
            // We default to expressions
            input = ExpressionParser.Parse(input, out var expr);
            input = input.Expect(TokenType.Semicolon);
            result = new ExpressionStatement { Expression = expr };
            return input;
        }

        private static TokenView ParseBlockStatement(TokenView input, out Statement result)
        {
            input = input.Expect(TokenType.OpenBrace);
            var statements = new List<Statement>();
            while (input.Peek().Type != TokenType.CloseBrace)
            {
                input = ParseStatement(input, out var stmt);
                statements.Add(stmt);
            }
            input = input.Expect(TokenType.CloseBrace);
            result = new CompoundStatement { Statements = statements };
            return input;
        }

        private static TokenView ParseFunctionDefinitionStatement(TokenView input, out Statement result)
        {
            input = input.Expect(TokenType.KwFunction);
            var fname = input.Peek().Value;
            input = input.Expect(TokenType.Identifier);
            input = input.Expect(TokenType.OpenParen);
            var parameters = new List<string>();
            if (input.Peek().Type != TokenType.CloseParen)
            {
                parameters.Add(input.Peek().Value);
                input = input.Expect(TokenType.Identifier);
                while (input.Peek().Type == TokenType.Comma)
                {
                    input = input.Expect(TokenType.Comma);
                    parameters.Add(input.Peek().Value);
                    input = input.Expect(TokenType.Identifier);
                }
            }
            input = input.Expect(TokenType.CloseParen);
            input = ParseBlockStatement(input, out var body);
            result = new FunctionDefinitionStatement { Name = fname, Parameters = parameters, Body = body };
            return input;
        }

        private static TokenView ParseIfStatement(TokenView input, out Statement result)
        {
            input = input.Expect(TokenType.KwIf);
            input = ExpressionParser.Parse(input, out var condition);
            input = ParseStatement(input, out var then);
            // To avoid 'null' in the AST
            Statement els = new CompoundStatement { Statements = new List<Statement>() };
            if (input.Peek().Type == TokenType.KwElse)
            {
                input = input.Expect(TokenType.KwElse);
                input = ParseStatement(input, out els);
            }
            result = new IfStatement { Condition = condition, Then = then, Else = els };
            return input;
        }

        private static TokenView ParseWhileStatement(TokenView input, out Statement result)
        {
            input = input.Expect(TokenType.KwWhile);
            input = ExpressionParser.Parse(input, out var condition);
            input = ParseStatement(input, out var body);
            result = new WhileStatement { Condition = condition, Body = body };
            return input;
        }

        private static TokenView ParseVarStatement(TokenView input, out Statement result)
        {
            input = input.Expect(TokenType.KwVar);
            var varname = input.Peek().Value;
            input = input.Expect(TokenType.Identifier);
            input = input.Expect(TokenType.Assign);
            input = ExpressionParser.Parse(input, out var val);
            input = input.Expect(TokenType.Semicolon);
            result = new VarDefinitionStatement { Name = varname, Value = val };
            return input;
        }

        private static TokenView ParseReturnStatement(TokenView input, out Statement result)
        {
            input = input.Expect(TokenType.KwReturn);
            if (input.Peek().Type == TokenType.Semicolon)
            {
                input = input.Expect(TokenType.Semicolon);
                result = new ReturnStatement { Value = null };
                return input;
            }
            input = ExpressionParser.Parse(input, out var expr);
            input = input.Expect(TokenType.Semicolon);
            result = new ReturnStatement { Value = expr };
            return input;
        }
    }

    /// <summary>
    /// Az egyik lehetséges elemzési hiba: Egy adott tokent vártunk (például a záró zárójelet),
    /// de ez nem következett.
    /// </summary>
    public class ExpectedTokenError : CompilerError
    {
        /// <summary>
        /// A várt token típus.
        /// </summary>
        public TokenType Expected { get; }
        /// <summary>
        /// A kapott token.
        /// </summary>
        public Token Got { get; }

        public ExpectedTokenError(TokenType expected, Token got)
        {
            Expected = expected;
            Got = got;
        }

        /// <summary>
        /// Kiírja a konzolra a hibaüzenetet.
        /// </summary>
        public override void Show()
        {
            Console.WriteLine($"Syntax error: Expected {Expected}, but got '{Got.Value}' at {Got.Position}.");
            Console.WriteLine(Got.Source.AnnotateAt(Got.Position));
        }
    }

    /// <summary>
    /// A másik gyakori elemzési hiba: Döntés előtt voltunk, és váratlan bemenetet kaptunk.
    /// </summary>
    public class UnexpectedTokenError : CompilerError
    {
        /// <summary>
        /// A token, amibe ütköztünk.
        /// </summary>
        public Token Got { get; }

        public UnexpectedTokenError(Token got)
        {
            Got = got;
        }

        /// <summary>
        /// Kiírja a konzolra a hibaüzenetet.
        /// </summary>
        public override void Show()
        {
            Console.WriteLine($"Syntax error: Unexpected token '{Got.Value}' at {Got.Position}.");
            Console.WriteLine(Got.Source.AnnotateAt(Got.Position));
        }
    }

    /// <summary>
    /// Egy segédstruktúra elemzéshez, egy token lista csak olvasható, nézet reprezentációja.
    /// </summary>
    public struct TokenView
    {
        private ListView<Token> tokens;

        private TokenView(ListView<Token> tokens)
        {
            this.tokens = tokens;
        }

        /// <summary>
        /// Beburkolja az adott token listát egy token lista nézetbe.
        /// </summary>
        /// <param name="tokens">A beburkolandó token lista.</param>
        public TokenView(List<Token> tokens)
            : this(new ListView<Token>(tokens))
        {
        }

        /// <summary>
        /// Visszaadja a következő tokent a nézetben.
        /// </summary>
        /// <returns>A következő token.</returns>
        public Token Peek()
        {
            return this.tokens.First();
        }

        /// <summary>
        /// Visszaadja azt a token nézetet, melyben az első token már nem szerepel.
        /// </summary>
        /// <returns>Egy olyan nézet, melyben az első token nem szerepel.</returns>
        public TokenView Consume()
        {
            return new TokenView(this.tokens.RemoveFirst());
        }

        /// <summary>
        /// Elvár egy adott típusú tokent a nézet elején. Ha nem az következik, kivételt dob.
        /// </summary>
        /// <param name="type">Az elvárt token típus.</param>
        /// <returns>Egy olyan nézet, melyben az első token nem szerepel.</returns>
        public TokenView Expect(TokenType type)
        {
            if (Peek().Type == type)
            {
                return Consume();
            }
            throw new ExpectedTokenError(type, Peek());
        }
    }
}

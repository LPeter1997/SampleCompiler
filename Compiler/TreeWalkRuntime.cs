using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    /// <summary>
    /// Az értelmezés egyik legegyszerűbb módja: A fa bejárása. A legtöbb
    /// értelmezett nyelv inkább valami hatékonyabb reprezentációra fordít,
    /// de egy kezdeti lépésnek tökéletes ez is.
    /// </summary>
    public class TreeWalkInterpreter
    {
        private SymbolTable symbols = new SymbolTable();

        /// <summary>
        /// Futtatja az adott programot a szintaxisfa alapján.
        /// </summary>
        /// <param name="program">A futtatandó program szintaxisfája.</param>
        public static void RunProgram(Statement program)
        {
            var interpreter = new TreeWalkInterpreter();
            interpreter.AddDefaultNativeFunctions();
            interpreter.Execute(program, true);
        }

        /// <summary>
        /// Regisztrál egy új natív függvényt a globális szkópba.
        /// </summary>
        /// <param name="name">A függvény neve.</param>
        /// <param name="func">A regisztrálandó C# függvény.</param>
        public void AddNativeFunction(string name, Func<List<Value>, Value> func)
        {
            var sym = new NativeFunctionValue { Function = func };
            symbols.Global.Define(name, new VariableSymbol { IsVariable = false, Value = sym });
        }

        /// <summary>
        /// Regisztrál néhány beépített függvényt.
        /// </summary>
        public void AddDefaultNativeFunctions()
        {
            AddNativeFunction("print", DefaultNativeFunctions.Print);
            AddNativeFunction("println", DefaultNativeFunctions.Println);
        }

        /// <summary>
        /// Lefuttatja az adott utasítás szintaxisfáját.
        /// </summary>
        /// <param name="stmt">A lefuttatandó utasítás szintaxisfája.</param>
        public void Execute(Statement stmt, bool suppressScope = false)
        {
            switch (stmt)
            {
                case IfStatement s:
                {
                    var condition = Evaluate(s.Condition);
                    if (condition.AsBool().Value)
                    {
                        Execute(s.Then);
                    }
                    else
                    {
                        Execute(s.Else);
                    }
                    return;
                }

                case WhileStatement s:
                {
                    while (Evaluate(s.Condition).AsBool().Value)
                    {
                        Execute(s.Body);
                    }
                    return;
                }

                case FunctionDefinitionStatement s:
                {
                    var fval = new FunctionValue { Node = s };
                    symbols.DefineSymbol(s.Name, new VariableSymbol { IsVariable = false, Value = fval });
                    return;
                }

                case ReturnStatement s:
                {
                    Value value = VoidValue.Instance;
                    if (s.Value != null)
                    {
                        value = Evaluate(s.Value);
                    }
                    throw new ReturnValue { Value = value };
                }

                case VarDefinitionStatement s:
                {
                    var value = Evaluate(s.Value);
                    symbols.DefineSymbol(s.Name.Value, new VariableSymbol { IsVariable = true, Value = value });
                    return;
                }

                case CompoundStatement s:
                {
                    if (suppressScope)
                    {
                        foreach (var substmt in s.Statements)
                        {
                            Execute(substmt);
                        }
                    }
                    else
                    {
                        symbols.Enscope(() =>
                        {
                            foreach (var substmt in s.Statements)
                            {
                                Execute(substmt);
                            }
                        });
                    }
                    return;
                }

                case ExpressionStatement s:
                {
                    Evaluate(s.Expression);
                    return;
                }
            }
        }

        /// <summary>
        /// Kiértékel egy kifejezést.
        /// </summary>
        /// <param name="expr">A kifejezés szintaxisfája.</param>
        /// <returns>A kiértékelt kifejezés értéke.</returns>
        public Value Evaluate(Expression expr)
        {
            switch (expr)
            {
                case IntegerLiteralExpression e: return new IntegerValue { Value = int.Parse(e.Value.Value) };
                case BoolLiteralExpression e: return new BoolValue { Value = e.Value.Type == TokenType.KwTrue };

                case StringLiteralExpression e: return new StringValue { Value = e.Unescape() };

                case VariableExpression e:
                {
                    var symbol = symbols.ReferenceSymbol(e.Identifier);
                    return symbol.AsVariable().Value;
                }

                case UnaryExpression e:
                {
                    var sub = Evaluate(e.Subexpression);
                    switch (e.Operator.Type)
                    {
                        case TokenType.Add: return Value.OperatorPonate(sub);
                        case TokenType.Subtract: return Value.OperatorNegate(sub);
                        case TokenType.Not: return Value.OperatorNot(sub);
                        default: throw new RuntimeError { Description = $"Unknown unary operator {e.Operator.Type}" };
                    }
                }

                case BinaryExpression e:
                {
                    if (e.Operator.Type == TokenType.Assign)
                    {
                        // Special case, we want left-hand-side to be a lvalue
                        if (e.Left is VariableExpression lhs)
                        {
                            var rhs = Evaluate(e.Right);
                            var sym = symbols.ReferenceSymbol(lhs.Identifier).AsVariable();
                            if (!sym.IsVariable)
                            {
                                throw new RuntimeError { Description = $"Can't change the value of constant '{lhs.Identifier}'" };
                            }
                            sym.Value = rhs;
                            return rhs;
                        }
                        else
                        {
                            throw new RuntimeError { Description = "Lvalue expected on left-hand-side of the assignment!" };
                        }
                    }
                    if (e.Operator.Type == TokenType.And || e.Operator.Type == TokenType.Or)
                    {
                        // Special case, we want lazy-evaluation
                        var lhs = Evaluate(e.Left).AsBool();
                        if (e.Operator.Type == TokenType.And)
                        {
                            // &&
                            if (lhs.Value)
                            {
                                return Evaluate(e.Right).AsBool();
                            }
                            Value.Bool(false);
                        }
                        else
                        {
                            // ||
                            if (lhs.Value)
                            {
                                return lhs;
                            }
                            return Evaluate(e.Right).AsBool();
                        }
                    }
                    var left = Evaluate(e.Left);
                    var right = Evaluate(e.Right);
                    switch (e.Operator.Type)
                    {
                        case TokenType.Add: return Value.OperatorAdd(left, right);
                        case TokenType.Subtract: return Value.OperatorSubtract(left, right);
                        case TokenType.Multiply: return Value.OperatorMultiply(left, right);
                        case TokenType.Divide: return Value.OperatorDivide(left, right);
                        case TokenType.Modulo: return Value.OperatorModulo(left, right);

                        case TokenType.Greater: return Value.OperatorGreater(left, right);
                        case TokenType.GreaterOrEqual: return Value.OperatorGreaterOrEqual(left, right);
                        case TokenType.Less: return Value.OperatorLess(left, right);
                        case TokenType.LessOrEqual: return Value.OperatorLessOrEqual(left, right);

                        case TokenType.Equal: return Value.OperatorEqual(left, right);
                        case TokenType.NotEqual: return Value.OperatorNotEqual(left, right);
                        
                        default: throw new NotImplementedException();
                    }
                }

                case CallExpression e:
                {
                    var value = Evaluate(e.Function);
                    var args = new List<Value>();
                    foreach (var param in e.Arguments)
                    {
                        args.Add(Evaluate(param));
                    }
                    if (value.IsFunction())
                    {
                        var f = value.AsFunction();
                        if (f.Node.Parameters.Count != e.Arguments.Count)
                        {
                            throw new RuntimeError { Description = $"Wrong number of arguments! Expected {f.Node.Parameters.Count}, but got {e.Arguments.Count}!" };
                        }
                        return EvaluateCall(f, args);
                    }
                    if (value.IsNativeFunction())
                    {
                        var f = value.AsNativeFunction();
                        return f.Function(args);
                    }
                    throw new RuntimeError { Description = "Can't call a non-function expression!" };
                }

                default: throw new NotImplementedException();
            }
        }

        private Value EvaluateCall(FunctionValue f, List<Value> args)
        {
            Value returnValue = null;
            symbols.Call(() =>
            {
                // Push parameters
                for (int i = 0; i < args.Count; ++i)
                {
                    symbols.DefineSymbol(f.Node.Parameters[i], new VariableSymbol { IsVariable = true, Value = args[i] });
                }
                returnValue = VoidValue.Instance;
                try
                {
                    Execute(f.Node.Body);
                }
                catch (ReturnValue rv)
                {
                    returnValue = rv.Value;
                }
            });
            return returnValue;
        }
    }

    /// <summary>
    /// A visszatérési értékkel picit csalunk, kivételként juttatjuk a hívóhoz.
    /// </summary>
    class ReturnValue : Exception
    {
        /// <summary>
        /// A visszatérési érték.
        /// </summary>
        public Value Value { get; set; }
    }

    public static class DefaultNativeFunctions
    {
        public static Value Print(List<Value> args)
        {
            foreach (var arg in args)
            {
                if (arg.IsInteger())
                {
                    Console.Write(arg.AsInteger().Value);
                }
                else if (arg.IsBool())
                {
                    Console.Write(arg.AsBool().Value);
                }
                else if (arg.IsString())
                {
                    Console.Write(arg.AsString().Value);
                }
                else if (arg.IsFunction())
                {
                    Console.Write("<function>");
                }
                else if (arg.IsNativeFunction())
                {
                    Console.Write("<native function>");
                }
                else
                {
                    throw new RuntimeError { Description = "Can't print type!" };
                }
            }
            return VoidValue.Instance;
        }

        public static Value Println(List<Value> args)
        {
            var ret = Print(args);
            Console.WriteLine();
            return ret;
        }

        public static Value Space(List<Value> args)
        {
            ExpectArgc(args, 0);
            Console.Write(' ');
            return VoidValue.Instance;
        }

        public static Value PlotX(List<Value> args)
        {
            ExpectArgc(args, 1);
            var plot = args[0].AsBool().Value;
            Console.Write(plot ? 'x' : ' ');
            return VoidValue.Instance;
        }

        private static void ExpectArgc(List<Value> args, int cnt)
        {
            if (args.Count != cnt)
            {
                throw new RuntimeError { Description = $"Native function expected {cnt} number of arguments, but got {args.Count}" };
            }
        }
    }

    /// <summary>
    /// Egy generikus, futásidejű hiba.
    /// </summary>
    public class RuntimeError : CompilerError
    {
        public string Description { get; set; }

        public override void Show()
        {
            Console.WriteLine($"Runtime error: {Description}!");
        }
    }
}

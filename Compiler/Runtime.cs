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
        private Scope globalScope;
        private Scope currentScope;

        /// <summary>
        /// Létrehoz egy új fa alapú értelmezőt egy üres, globólis szkóppal.
        /// </summary>
        public TreeWalkInterpreter()
        {
            this.globalScope = new Scope();
            this.currentScope = this.globalScope;
        }

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
            globalScope.Define(name, new VariableSymbol { IsVariable = false, Value = sym });
        }

        /// <summary>
        /// Regisztrál néhány beépített függvényt.
        /// </summary>
        public void AddDefaultNativeFunctions()
        {
            AddNativeFunction("print", DefaultNativeFunctions.Print);
            AddNativeFunction("println", DefaultNativeFunctions.Println);
            AddNativeFunction("space", DefaultNativeFunctions.Space);
            AddNativeFunction("plot_x", DefaultNativeFunctions.PlotX);
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
                    var fval = new FunctionValue { Parameters = s.Parameters, Body = s.Body };
                    currentScope.Define(s.Name, new VariableSymbol { IsVariable = false, Value = fval });
                    return;
                }

                case ReturnStatement s:
                {
                    Value value = new VoidValue();
                    if (s.Value != null)
                    {
                        value = Evaluate(s.Value);
                    }
                    throw new ReturnValue { Value = value };
                }

                case VarDefinitionStatement s:
                {
                    var value = Evaluate(s.Value);
                    currentScope.Define(s.Name, new VariableSymbol { IsVariable = true, Value = value });
                    return;
                }

                case CompoundStatement s:
                {
                    if (!suppressScope)
                    {
                        PushScope();
                    }
                    foreach (var substmt in s.Statements)
                    {
                        Execute(substmt);
                    }
                    if (!suppressScope)
                    {
                        PopScope();
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
                case IntegerLiteralExpression e: return new IntegerValue { Value = e.Value };
                case BoolLiteralExpression e: return new BoolValue { Value = e.Value };
                
                case VariableExpression e:
                {
                    var symbol = this.currentScope.Reference(e.VariableName);
                    return symbol.AsVariable().Value;
                }

                case UnaryExpression e:
                {
                    var sub = Evaluate(e.Subexpression);
                    switch (e.Operator.Type)
                    {
                        case TokenType.Add: return new IntegerValue { Value = sub.AsInteger().Value };
                        case TokenType.Subtract: return new IntegerValue { Value = -sub.AsInteger().Value };
                        case TokenType.Not: return new BoolValue { Value = !sub.AsBool().Value };
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
                            var sym = currentScope.Reference(lhs.VariableName).AsVariable();
                            if (!sym.IsVariable)
                            {
                                throw new RuntimeError { Description = $"Can't change the value of constant '{lhs.VariableName}'" };
                            }
                            sym.Value = rhs;
                            return rhs;
                        }
                        else
                        {
                            throw new RuntimeError { Description = "Lvalue expected on left-hand-side of the assignment!" };
                        }
                    }
                    var left = Evaluate(e.Left);
                    var right = Evaluate(e.Right);
                    switch (e.Operator.Type)
                    {
                        case TokenType.Add: return new IntegerValue { Value = left.AsInteger().Value + right.AsInteger().Value };
                        case TokenType.Subtract: return new IntegerValue { Value = left.AsInteger().Value - right.AsInteger().Value };
                        case TokenType.Multiply: return new IntegerValue { Value = left.AsInteger().Value * right.AsInteger().Value };
                        case TokenType.Divide: return new IntegerValue { Value = left.AsInteger().Value / right.AsInteger().Value };
                        case TokenType.Modulo: return new IntegerValue { Value = left.AsInteger().Value % right.AsInteger().Value };

                        case TokenType.Greater: return new BoolValue { Value = left.AsInteger().Value > right.AsInteger().Value };
                        case TokenType.GreaterOrEqual: return new BoolValue { Value = left.AsInteger().Value >= right.AsInteger().Value };
                        case TokenType.Less: return new BoolValue { Value = left.AsInteger().Value < right.AsInteger().Value };
                        case TokenType.LessOrEqual: return new BoolValue { Value = left.AsInteger().Value <= right.AsInteger().Value };

                        // NOTE: These are not lazy!
                        case TokenType.And: return new BoolValue { Value = left.AsBool().Value && right.AsBool().Value };
                        case TokenType.Or: return new BoolValue { Value = left.AsBool().Value || right.AsBool().Value };

                        case TokenType.Equal:
                        case TokenType.NotEqual:
                        {
                            if (left.IsInteger())
                            {
                                var value = left.AsInteger().Value == right.AsInteger().Value;
                                if (e.Operator.Type == TokenType.NotEqual)
                                {
                                    value = !value;
                                }
                                return new BoolValue { Value = value };
                            }
                            if (left.IsBool())
                            {
                                var value = left.AsBool().Value == right.AsBool().Value;
                                if (e.Operator.Type == TokenType.NotEqual)
                                {
                                    value = !value;
                                }
                                return new BoolValue { Value = value };
                            }
                            throw new RuntimeError { Description = "Unexpected type in equality operation!" };
                        }

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
                        if (f.Parameters.Count != e.Arguments.Count)
                        {
                            throw new RuntimeError { Description = $"Wrong number of arguments! Expected {f.Parameters.Count}, but got {e.Arguments.Count}!" };
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
            var oldScope = SwapScope(new Scope(globalScope));
            // Push parameters
            for (int i = 0; i < args.Count; ++i)
            {
                currentScope.Define(f.Parameters[i], new VariableSymbol { IsVariable = true, Value = args[i] });
            }
            Value returnValue = new VoidValue();
            try
            {
                Execute(f.Body);
            }
            catch (ReturnValue rv)
            {
                returnValue = rv.Value;
            }
            SwapScope(oldScope);
            return returnValue;
        }

        private Scope SwapScope(Scope newScope)
        {
            var result = this.currentScope;
            this.currentScope = newScope;
            return result;
        }

        private void PushScope()
        {
            this.currentScope = new Scope(this.currentScope);
        }

        private void PopScope()
        {
            this.currentScope = this.currentScope.Parent;
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
            bool first = true;
            foreach (var arg in args)
            {
                if (!first)
                {
                    Console.Write(", ");
                }
                first = false;
                if (arg.IsInteger())
                {
                    Console.Write(arg.AsInteger().Value);
                }
                else if (arg.IsBool())
                {
                    Console.Write(arg.AsBool().Value);
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
            return new VoidValue();
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
            return new VoidValue();
        }

        public static Value PlotX(List<Value> args)
        {
            ExpectArgc(args, 1);
            var plot = args[0].AsBool().Value;
            Console.Write(plot ? 'x' : ' ');
            return new VoidValue();
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

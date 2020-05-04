using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Compiler
{
    /// <summary>
    /// Egy egyszerű, verem (stack) alapú virtuális gép. Ezek sokkal egyszerűbbek, de némivel
    /// lassabbak, mint a regiszter alapú virtuális gépek.
    /// </summary>
    public class VM
    {
        /// <summary>
        /// A futtatandó bytekód.
        /// </summary>
        private Bytecode code;
        /// <summary>
        /// A hívási verem.
        /// </summary>
        private Stack<Frame> callStack = new Stack<Frame>();
        /// <summary>
        /// A globális regiszterek.
        /// </summary>
        public Value[] Globals { get; set; }

        /// <summary>
        /// Létrehoz egy új virtuális gépet.
        /// </summary>
        /// <param name="code">A futtatandó bytekód.</param>
        public VM(Bytecode code)
        {
            this.code = code;
        }

        /// <summary>
        /// Futtatja a virtuális gép kódját.
        /// </summary>
        public void Execute()
        {
            callStack.Push(new Frame { });
            while (callStack.Count > 0)
            {
                ExecuteInstruction();
            }
        }

        private void ExecuteInstruction()
        {
            var top = callStack.Peek();
            var stk = top.ComputationStack;
            var instr = (Opcode)code.Code[top.InstructionPointer];
            top.InstructionPointer += 1;
            switch (instr)
            {
                case Opcode.GAlloc:
                {
                    Debug.Assert(Globals == null);
                    var len = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    Globals = new Value[len];
                } break;

                case Opcode.GLoad:
                {
                    var idx = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    stk.Push(Globals[idx]);
                } break;

                case Opcode.GStore:
                {
                    var idx = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    var value = stk.Pop();
                    Globals[idx] = value;
                } break;

                case Opcode.Alloc:
                {
                    Debug.Assert(top.Registers == null);
                    var len = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    top.Registers = new Value[len];
                } break;

                case Opcode.Load:
                {
                    var idx = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    stk.Push(top.Registers[idx]);
                } break;

                case Opcode.Store:
                {
                    var idx = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    var value = stk.Pop();
                    top.Registers[idx] = value;
                } break;

                case Opcode.Pop:
                {
                    stk.Pop();
                } break;

                case Opcode.Pushi:
                {
                    var value = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    stk.Push(new IntegerValue { Value = value });
                } break;

                case Opcode.Pushb:
                {
                    var value = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    stk.Push(new BoolValue { Value = value != 0 });
                } break;

                case Opcode.Pushs:
                {
                    var index = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    stk.Push(new StringValue { Value = code.Constants[index] as string });
                } break;

                case Opcode.Pushf:
                {
                    var value = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    stk.Push(new FunctionValue { Address = value });
                } break;

                case Opcode.Pushnf:
                {
                    var constIndex = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    var value = code.Constants[constIndex];
                    stk.Push(new NativeFunctionValue { Function = value as Func<List<Value>, Value> });
                } break;

                case Opcode.Call:
                {
                    var argc = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;

                    var args = new List<Value>();
                    for (int i = 0; i < argc; ++i)
                    {
                        args.Add(stk.Pop());
                    }

                    var func = stk.Pop();
                    if (func.IsFunction())
                    {
                        var f = func.AsFunction();
                        var newFrame = new Frame { InstructionPointer = f.Address.Value };
                        for (int i = argc - 1; i >= 0; --i)
                        {
                            newFrame.ComputationStack.Push(args[i]);
                        }
                        callStack.Push(newFrame);
                    }
                    else
                    {
                        var nf = func.AsNativeFunction();
                        args.Reverse();
                        var ret = nf.Function(args);
                        stk.Push(ret);
                    }
                } break;

                case Opcode.Jump:
                {
                    var addr = code.Code[top.InstructionPointer];
                    top.InstructionPointer = addr;
                } break;

                case Opcode.JumpIf:
                {
                    var addr = code.Code[top.InstructionPointer];
                    top.InstructionPointer += 1;
                    var cond = stk.Pop().AsBool();
                    if (cond.Value)
                    {
                        top.InstructionPointer = addr;
                    }
                } break;

                case Opcode.Return:
                {
                    Value returnValue = VoidValue.Instance;
                    if (stk.Count > 0)
                    {
                        returnValue = stk.Pop();
                    }
                    callStack.Pop();
                    if (callStack.Count > 0)
                    {
                        top = callStack.Peek();
                        top.ComputationStack.Push(returnValue);
                    }
                } break;

                case Opcode.Add:
                {
                    var right = stk.Pop();
                    var left = stk.Pop();
                    stk.Push(Value.OperatorAdd(left, right));
                } break;

                case Opcode.Sub:
                {
                    var right = stk.Pop();
                    var left = stk.Pop();
                    stk.Push(Value.OperatorSubtract(left, right));
                } break;

                case Opcode.Mul:
                {
                    var right = stk.Pop();
                    var left = stk.Pop();
                    stk.Push(Value.OperatorMultiply(left, right));
                } break;

                case Opcode.Div:
                {
                    var right = stk.Pop();
                    var left = stk.Pop();
                    stk.Push(Value.OperatorDivide(left, right));
                } break;

                case Opcode.Mod:
                {
                    var right = stk.Pop();
                    var left = stk.Pop();
                    stk.Push(Value.OperatorModulo(left, right));
                } break;

                case Opcode.Eq:
                {
                    var right = stk.Pop();
                    var left = stk.Pop();
                    stk.Push(Value.OperatorEqual(left, right));
                } break;

                case Opcode.Greater:
                {
                    var right = stk.Pop();
                    var left = stk.Pop();
                    stk.Push(Value.OperatorGreater(left, right));
                } break;

                case Opcode.Less:
                {
                    var right = stk.Pop();
                    var left = stk.Pop();
                    stk.Push(Value.OperatorLess(left, right));
                } break;

                case Opcode.Not:
                {
                    var op = stk.Pop();
                    stk.Push(Value.OperatorNot(op));
                } break;

                case Opcode.Neg:
                {
                    var op = stk.Pop();
                    stk.Push(Value.OperatorNegate(op));
                } break;

                default: throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// A hívási verem egy eleme.
    /// </summary>
    public class Frame
    {
        /// <summary>
        /// A lefoglalt regiszterek.
        /// </summary>
        public Value[] Registers { get; set; }
        /// <summary>
        /// A számítási verem.
        /// </summary>
        public Stack<Value> ComputationStack { get; set; } = new Stack<Value>();
        /// <summary>
        /// Az aktuális utasítás címe.
        /// </summary>
        public int InstructionPointer { get; set; }
    }

    /// <summary>
    /// Lefordított bytekód.
    /// </summary>
    public class Bytecode
    {
        /// <summary>
        /// Maga a futtatható kód.
        /// </summary>
        public int[] Code { get; set; }
        /// <summary>
        /// Konstansok.
        /// </summary>
        public object[] Constants { get; set; }
    }

    /// <summary>
    /// A VM bytekód fordítója.
    /// </summary>
    public class Compiler
    {
        private SymbolTable symbols = new SymbolTable();
        private List<int> code = new List<int>();
        private List<object> constants = new List<object>();

        /// <summary>
        /// Visszaadja a lefordított utasítások bytekód reprezentációját.
        /// </summary>
        public Bytecode Bytecode => new Bytecode
        {
            Code = code.ToArray(),
            Constants = constants.ToArray(),
        };

        /// <summary>
        /// Lefordítja az adott programot.
        /// </summary>
        /// <param name="stmt">A lefordítandó program szintaxisfája.</param>
        /// <returns></returns>
        public static Bytecode CompileProgram(Statement stmt)
        {
            var compiler = new Compiler();
            compiler.Write(Opcode.GAlloc);
            var gallocPos = compiler.BlankAddress();
            compiler.AddDefaultNativeFunctions();
            compiler.Compile(stmt, true);
            compiler.Write(Opcode.Return);
            compiler.FillAddress(gallocPos, compiler.symbols.SymbolCount);
            return compiler.Bytecode;
        }

        /// <summary>
        /// Regisztrál egy új natív függvényt a globális szkópba.
        /// </summary>
        /// <param name="name">A függvény neve.</param>
        /// <param name="func">A regisztrálandó C# függvény.</param>
        public void AddNativeFunction(string name, Func<List<Value>, Value> func)
        {
            Debug.Assert(symbols.IsGlobalScope());

            var sym = new VariableSymbol { IsVariable = false };
            var regIdx = symbols.DefineSymbol(name, sym);
            sym.RegisterIndex = regIdx;

            var constIdx = AddConstant(func);

            Write(Opcode.Pushnf, constIdx);
            Write(Opcode.GStore, regIdx);
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
        /// Lefordítja az adott utasítást.
        /// </summary>
        /// <param name="stmt">A lefordítandó utasítás szintaxisfája.</param>
        public void Compile(Statement stmt, bool suppressScope = false)
        {
            switch (stmt)
            {
                case IfStatement s:
                {
                    /*
                        <kifejezés kiértékelés, eredmény a verem tetején>
                        JumpIf then_block
                        Jump else_block
                    then_block:
                        <then blokk fordítása>
                        Jump end
                    else_block:
                        <else blokk fordítása>
                    end:
                    */

                    Compile(s.Condition);

                    Write(Opcode.JumpIf);
                    int thenPos = BlankAddress();

                    Write(Opcode.Jump);
                    int elsePos = BlankAddress();

                    var thenAddr = CurrentAddress;
                    Compile(s.Then);

                    Write(Opcode.Jump);
                    var endPos = BlankAddress();

                    var elseAddr = CurrentAddress;
                    Compile(s.Else);

                    var endAddr = CurrentAddress;

                    // Feltöltjük a kihagyott címeket
                    FillAddress(thenPos, thenAddr);
                    FillAddress(elsePos, elseAddr);
                    FillAddress(endPos, endAddr);
                } break;

                case WhileStatement s:
                {
                    /*
                        Jump condition
                    body:
                        <blokk fordítása>
                    condition:
                        <kifejezés kiértékelés, eredmény a verem tetején>
                        JumpIf body
                    */

                    Write(Opcode.Jump);
                    var condPos = BlankAddress();

                    var bodyAddr = CurrentAddress;
                    Compile(s.Body);

                    var condAddr = CurrentAddress;
                    Compile(s.Condition);
                    Write(Opcode.JumpIf, bodyAddr);

                    FillAddress(condPos, condAddr);
                } break;

                case FunctionDefinitionStatement s:
                {
                    // Az egyszerűség kedvéért inline fordítunk és átugorjuk
                    // Egy rendes fordító nem csinálna ilyet!
                    /*
                    Jump after_f_code
                    func:
                        Alloc <változók száma>
                        <változók hozzárendelése>
                        <blokk fordítása>
                        Return
                    after_f_code:
                    */

                    var sym = new VariableSymbol { IsVariable = false };
                    var idx = symbols.DefineSymbol(s.Name, sym);
                    sym.RegisterIndex = idx;
                    int funcAddress = 0;
                    symbols.Call(() =>
                    {
                        Write(Opcode.Jump);
                        var afterPos = BlankAddress();
                        funcAddress = CurrentAddress;
                        Write(Opcode.Alloc);
                        var allocCntPos = BlankAddress();

                        // Változók hozzárendelése
                        // Visszafele, hiszen fordított sorrendbe kerülnek a verem tetejére!
                        for (int i = s.Parameters.Count - 1; i >= 0; --i)
                        {
                            var sym = new VariableSymbol { IsVariable = true };
                            var idx = symbols.DefineSymbol(s.Parameters[i], sym);
                            sym.RegisterIndex = idx;
                            Write(Opcode.Store, idx);
                        }

                        Compile(s.Body);

                        FillAddress(afterPos, CurrentAddress);
                        FillAddress(allocCntPos, symbols.SymbolCount);
                    });
                    // Eltároljuk a függvvényt az adott regiszterben
                    Write(Opcode.Pushf, funcAddress);
                    Write(symbols.IsGlobalScope() ? Opcode.GStore : Opcode.Store, idx);
                } break;

                case ReturnStatement s:
                {
                    if (s.Value != null)
                    {
                        Compile(s.Value);
                    }
                    Write(Opcode.Return);
                } break;

                case VarDefinitionStatement s:
                {
                    /*
                    <kifejezés kiértékelés, eredmény a verem tetején>
                    Store <index>
                    */

                    Compile(s.Value);

                    // A világ legegyszerűbb regiszter allokációja
                    var sym = new VariableSymbol { IsVariable = true };
                    var symIndex = symbols.DefineSymbol(s.Name.Value, sym);
                    sym.RegisterIndex = symIndex;

                    Write(symbols.IsGlobalScope() ? Opcode.GStore : Opcode.Store, symIndex);
                } break;

                case CompoundStatement s:
                {
                    if (suppressScope)
                    {
                        // Elég csak lefordítanunk az összes utasítást egymás után
                        foreach (var st in s.Statements)
                        {
                            Compile(st);
                        }
                    }
                    else
                    {
                        symbols.Enscope(() =>
                        {
                            // Elég csak lefordítanunk az összes utasítást egymás után
                            foreach (var st in s.Statements)
                            {
                                Compile(st);
                            }
                        });
                    }
                } break;

                case ExpressionStatement s:
                {
                    /*
                    <kifejezés kiértékelés, eredmény a verem tetején>
                    Pop
                    */
                    Compile(s.Expression);
                    Write(Opcode.Pop);
                } break;

                default: throw new NotImplementedException();
            }
        }

        private void Compile(Expression expr)
        {
            switch (expr)
            {
                case IntegerLiteralExpression e:
                {
                    /*
                    Pushi <szám>
                    */
                    Write(Opcode.Pushi, int.Parse(e.Value.Value));
                } break;

                case BoolLiteralExpression e:
                {
                    /*
                    Pushb <0 vagy 1>
                    */
                    Write(Opcode.Pushb, e.Value.Type == TokenType.KwTrue ? 1 : 0);
                } break;

                case StringLiteralExpression e:
                {
                    /*
                    Pushs <konstans index> 
                    */
                    int idx = AddConstant(e.Unescape());
                    Write(Opcode.Pushs, idx);
                } break;

                case VariableExpression e:
                {
                    /*
                    Load <regiszter index>
                    */
                    var sym = symbols.ReferenceSymbol(e.Identifier).AsVariable();
                    int idx = sym.RegisterIndex.Value;
                    Write(sym.IsGlobal ? Opcode.GLoad : Opcode.Load, idx);
                } break;

                case UnaryExpression e:
                {
                    /*
                    <kifejezés kiértékelés, eredmény a verem tetején>
                    <megfelelő utasítás>
                    */
                    Compile(e.Subexpression);
                    switch (e.Operator.Type)
                    {
                        case TokenType.Add: break; // No-op
                        case TokenType.Subtract: Write(Opcode.Neg); break;
                        case TokenType.Not: Write(Opcode.Not); break;
                    }
                } break;

                case BinaryExpression e:
                {
                    if (e.Operator.Type == TokenType.Assign)
                    {
                        // Speciális eset
                        if (e.Left is VariableExpression ident)
                        {
                            /*
                            <jobboldali kifejezés kiértékelés, eredmény a verem tetején> 
                            Store <regiszter index>
                            Load <regiszter index> // Hogy a stackre kerüljön a kiértékelt
                            */
                            var sym = symbols.ReferenceSymbol(ident.Identifier).AsVariable();
                            if (!sym.IsVariable)
                            {
                                // TODO
                                throw new NotImplementedException("Can't assign to non-variable!");
                            }
                            var idx = sym.RegisterIndex.Value;
                            Compile(e.Right);
                            Write(symbols.IsGlobalScope() ? Opcode.GStore : Opcode.Store, idx);
                            Write(symbols.IsGlobalScope() ? Opcode.GLoad: Opcode.Load, idx);
                        }
                        else
                        {
                            // TODO
                            throw new NotImplementedException("Lvalue expected on left-hand-side of the assignment!");
                        }
                        break;
                    }
                    if (e.Operator.Type == TokenType.Or)
                    {
                        // Speciális eset, lusta kiértékelés
                        // TODO
                        throw new NotImplementedException();
                        break;
                    }
                    if (e.Operator.Type == TokenType.And)
                    {
                        // Speciális eset, lusta kiértékelés
                        // TODO
                        throw new NotImplementedException();
                        break;
                    }

                    /*
                    <baloldali kifejezés kiértékelés, eredmény a verem tetején>
                    <jobboldali kifejezés kiértékelés, eredmény a verem tetején>
                    <megfelelő utasítás>
                    */
                    Compile(e.Left);
                    Compile(e.Right);
                    switch (e.Operator.Type)
                    {
                        // Aritmetika
                        case TokenType.Add: Write(Opcode.Add); break;
                        case TokenType.Subtract: Write(Opcode.Sub); break;
                        case TokenType.Multiply: Write(Opcode.Mul); break;
                        case TokenType.Divide: Write(Opcode.Div); break;
                        case TokenType.Modulo: Write(Opcode.Mod); break;

                        // Hasonlítás
                        case TokenType.Less: Write(Opcode.Less); break;
                        case TokenType.Greater: Write(Opcode.Greater); break;
                        case TokenType.Equal: Write(Opcode.Eq); break;

                        // A többi visszavezetése erre a kettőre

                        case TokenType.LessOrEqual:
                            Write(Opcode.Greater);
                            Write(Opcode.Not);
                            break;

                        case TokenType.GreaterOrEqual:
                            Write(Opcode.Less);
                            Write(Opcode.Not);
                            break;

                        case TokenType.NotEqual:
                            Write(Opcode.Eq);
                            Write(Opcode.Not);
                            break;
                    }
                } break;

                case CallExpression e:
                {
                    Compile(e.Function);
                    foreach (var arg in e.Arguments)
                    {
                        Compile(arg);
                    }
                    Write(Opcode.Call, e.Arguments.Count);
                } break;

                default: throw new NotImplementedException();
            }
        }

        private int CurrentAddress => code.Count;

        private void Write(Opcode opcode)
        {
            code.Add((int)opcode);
        }

        private void Write(Opcode opcode, int num)
        {
            code.Add((int)opcode);
            code.Add(num);
        }

        private int BlankAddress()
        {
            int ret = CurrentAddress;
            code.Add(0);
            return ret;
        }

        private void FillAddress(int pos, int addr)
        {
            code[pos] = addr;
        }

        private int AddConstant(object value)
        {
            // Lehetséges optimalizáció: Ha már hozzáadtuk, ne tároljuk mégegyszer
            // Egy további lehetséges optimalizáció az összes sztringet egybe fűzni
            // és az opkódot Pushs <index> <hossz> formára alakítani.
            int idx = constants.Count;
            constants.Add(value);
            return idx;
        }
    }

    /// <summary>
    /// Az utasítások, melyekkel a VM dolgozik.
    /// </summary>
    public enum Opcode
    {
        /// <summary>
        /// Visszatérés. Stack felső értéke a visszatérési érték, ha van.
        /// </summary>
        Return,
        /// <summary>
        /// Allokál egy adott mennyiségű globális regisztert. Operandus az opkód után.
        /// </summary>
        GAlloc,
        /// <summary>
        /// Eltárolja a verem tetején levő értéket egy globális regiszterbe. Regiszter címe az utasítás után.
        /// </summary>
        GStore,
        /// <summary>
        /// Egy globális regiszter érték betöltése, felrakása a verem tetejére.
        /// </summary>
        GLoad,
        /// <summary>
        /// Allokál egy adott mennyiségű regisztert. Operandus az opkód után.
        /// </summary>
        Alloc,
        /// <summary>
        /// Eltárolja a verem tetején levő értéket egy regiszterbe. Regiszter címe az utasítás után.
        /// </summary>
        Store,
        /// <summary>
        /// Egy regiszter érték betöltése, felrakása a verem tetejére.
        /// </summary>
        Load,
        /// <summary>
        /// Egész konstans stack-re rakása. Operandus az opkód után.
        /// </summary>
        Pushi,
        /// <summary>
        /// Igaz-hamis konstans stack-re rakása. Operandus az opkód után.
        /// </summary>
        Pushb,
        /// <summary>
        /// String konstans stack-re rakása. Konstans indexe az opkód után.
        /// </summary>
        Pushs,
        /// <summary>
        /// Függvény címének stack-re rakása. Operandus az opkód után.
        /// </summary>
        Pushf,
        /// <summary>
        /// Natív függvény konstans indexének stack-re rakása. Operandus az opkód után.
        /// </summary>
        Pushnf,
        /// <summary>
        /// Leszedi az értéket a verem tetejéről.
        /// </summary>
        Pop,
        /// <summary>
        /// Feltétel nélküli ugrás. Cím az utasítás után.
        /// </summary>
        Jump,
        /// <summary>
        /// Feltételes ugrás. Cím az utasítás után, feltétel a verem tetején.
        /// </summary>
        JumpIf,
        /// <summary>
        /// Függvényhívás. Cím és paraméterek a verem tetején. Paraméterszám az opkód után.
        /// </summary>
        Call,
        // Bináris műveletek: Operandus a két felső érték a veremben (ezek ki is kerülnek a veremből).
        // Az eredmény a verem tetejére kerül.
        Add, Sub, Mul, Div, Mod, Less, Greater, Eq, 
        // Unáris műveletek
        Not, Neg,
    }
}

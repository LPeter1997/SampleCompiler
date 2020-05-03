using System;
using System.Collections.Generic;
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
        /// A futtatandó kód.
        /// </summary>
        private int[] code;
        /// <summary>
        /// A kódban megtalálható szöveges konstansok.
        /// </summary>
        private string[] constants;
        /// <summary>
        /// A hívási verem.
        /// </summary>
        private Stack<Frame> callStack = new Stack<Frame>();
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
        public Stack<Value> ComputationStack { get; set; }
        /// <summary>
        /// A visszatérési cím.
        /// </summary>
        public int ReturnAddress { get; set; }
    }

    /// <summary>
    /// A VM bytekód fordítója.
    /// </summary>
    public class Compiler
    {
        private List<int> code = new List<int>();
        private Scope globalScope;
        private Scope currentScope;

        public Compiler()
        {
            this.globalScope = new Scope();
            this.currentScope = this.globalScope;
        }

        /// <summary>
        /// Lefordítja az adott utasítást.
        /// </summary>
        /// <param name="stmt">A lefordítandó utasítás.</param>
        public void Compile(Statement stmt)
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
                    var endPos = CurrentAddress;
                    code.Add(0);

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
                        <blokk fordítása>
                        Return
                    after_f_code:
                    */
                    // TODO
                    throw new NotImplementedException();
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
                    // TODO
                    throw new NotImplementedException();
                } break;

                default: throw new NotImplementedException();
            }
        }

        private void Compile(Expression expr)
        {
            switch (expr)
            {
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
    /// Az utasítások, melyekkel a VM dolgozik.
    /// </summary>
    public enum Opcode
    {
        /// <summary>
        /// Visszatérés. Stack felső értéke a visszatérési érték, ha van.
        /// </summary>
        Return,
        /// <summary>
        /// Allokál egy adott mennyiségű regisztert. Operandus az opkód után.
        /// </summary>
        Alloc,
        /// <summary>
        /// Egész konstans stack-re rakása. Operandus az opkód után.
        /// </summary>
        Pushi,
        /// <summary>
        /// Megduplázza a verem legfelső értékét.
        /// </summary>
        Duplicate,
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
        Add, Sub, Mul, Div, Mod,
        Not, Neg,
    }
}

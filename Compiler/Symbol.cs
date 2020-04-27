using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    /// <summary>
    /// Szimbólum minden, aminek neve van és később hivatkozunk rá: változók, típusok, 
    /// függvények, stb.
    /// </summary>
    public abstract class Symbol
    {
        public VariableSymbol AsVariable() 
        {
            if (this is VariableSymbol v)
            {
                return v;
            }
            // TODO
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Egy változó szimbóluma egy dinamikus nyelvben általában az értékét tárolja.
    /// </summary>
    public class VariableSymbol : Symbol
    {
        /// <summary>
        /// Nem szeretnénk, hogy a fix nevű függvények értékét változtatgassuk, ezért 
        /// tároljuk, hogy ez a szimbólum változtatható-e.
        /// </summary>
        public bool IsVariable { get; set; }

        /// <summary>
        /// A szimbólum aktuális értéke.
        /// </summary>
        public Value Value { get; set; }
    }

    /// <summary>
    /// Egy szkóp egy adott blokkban látható szimbólumok gyűjteménye. Ha a szkóp egy külső
    /// szkóp része, egy mutatót is tárol az ősre. Így a szkópok gyakorlatilag fát alkotnak.
    /// 
    /// Példa, az alábbi kód:
    /// 
    /// {
    ///     var x;
    ///     {
    ///         var y;
    ///         var z;
    ///         { var q; }
    ///     }
    ///     { 
    ///         var q; 
    ///     }
    ///     var w;
    /// }
    /// 
    /// Szkóp fája:
    /// 
    ///           [x, w]
    ///           /    \
    ///       [y, z]   [q]
    ///          |
    ///         [q]
    ///         
    /// Látható, hogy a külső szkópok szimbólumai is elérhetők a belsőkből, illetve a belső 
    /// szkópok névütközését elkerüli a fa.
    /// </summary>
    public class Scope
    {
        private Scope parent;
        private Dictionary<string, Symbol> symbols = new Dictionary<string, Symbol>();

        /// <summary>
        /// A szkóp őse.
        /// </summary>
        public Scope Parent => parent;

        /// <summary>
        /// Létrehoz egy új szkópot az adott ős szkóppal.
        /// </summary>
        /// <param name="parent">Az ős szkóp.</param>
        public Scope(Scope parent = null)
        {
            this.parent = parent;
        }

        /// <summary>
        /// Definiálja az adott szimbólumot ebben a szkópban.
        /// </summary>
        /// <param name="name">A definiálandó szimbólum neve.</param>
        /// <param name="symbol">A definiálandó szimbólum.</param>
        public void Define(string name, Symbol symbol)
        {
            symbols.Add(name, symbol);
        }

        /// <summary>
        /// Megkeres egy szimbólumot az adott név alatt. Ha nem található, kivételt dob.
        /// </summary>
        /// <param name="name">A megkeresendő szimbólum neve.</param>
        /// <returns>Az adott nevű szimbólum.</returns>
        public Symbol Reference(string name)
        {
            if (symbols.TryGetValue(name, out var result))
            {
                return result;
            }
            if (parent != null)
            {
                return parent.Reference(name);
            }
            throw new SymbolNotFoundError { Name = name };
        }
    }

    /// <summary>
    /// Mivel egy dinamikus típusú nyelvet készítünk, az értékek is többalakúak kellenek 
    /// hogy legyenek.
    /// </summary>
    public abstract class Value
    {
        // Típus lekérdezések

        public bool IsVoid() { return (this as VoidValue) != null; }
        public bool IsInteger() { return (this as IntegerValue) != null; }
        public bool IsBool() { return (this as BoolValue) != null; }
        public bool IsFunction() { return (this as FunctionValue) != null; }
        public bool IsNativeFunction() { return (this as NativeFunctionValue) != null; }

        // Típus elérések

        public IntegerValue AsInteger()
        {
            if (!IsInteger())
            {
                // TODO: Janky error
                throw new TypeError { Expected = "integer", Got = this.GetType().Name };
            }
            return this as IntegerValue;
        }

        public BoolValue AsBool()
        {
            if (!IsBool())
            {
                // TODO: Janky error
                throw new TypeError { Expected = "bool", Got = this.GetType().Name };
            }
            return this as BoolValue;
        }

        public FunctionValue AsFunction()
        {
            if (!IsFunction())
            {
                // TODO: Janky error
                throw new TypeError { Expected = "function", Got = this.GetType().Name };
            }
            return this as FunctionValue;
        }

        public NativeFunctionValue AsNativeFunction()
        {
            if (!IsNativeFunction())
            {
                // TODO: Janky error
                throw new TypeError { Expected = "native function", Got = this.GetType().Name };
            }
            return this as NativeFunctionValue;
        }
    }

    /// <summary>
    /// A "nem érték" érték.
    /// </summary>
    public class VoidValue : Value
    {
    }

    /// <summary>
    /// Egész szám érték.
    /// </summary>
    public class IntegerValue : Value
    {
        public int Value { get; set; }
    }

    /// <summary>
    /// Logikai érték.
    /// </summary>
    public class BoolValue : Value
    {
        public bool Value { get; set; }
    }

    /// <summary>
    /// Függvény érték.
    /// </summary>
    public class FunctionValue : Value
    {
        /// <summary>
        /// A függvény paramétereinek a neve.
        /// </summary>
        public List<string> Parameters { get; set; }
        /// <summary>
        /// A függvénytest.
        /// </summary>
        public Statement Body { get; set; }
    }

    /// <summary>
    /// Egy natív, C# függvény érték, mely hívható a kis nyelvünkből. Gyakorlatilag az FFI
    /// (Foreign Function Interface) magja.
    /// </summary>
    public class NativeFunctionValue : Value
    {
        /// <summary>
        /// A C# függvény, amit hívunk.
        /// </summary>
        public Func<List<Value>, Value> Function { get; set; }
    }

    // TODO: Ideiglenes megoldás, mert nincsen explicit típus leíró.
    /// <summary>
    /// Dinamikus nyelveknél tipikus futásidejű hiba: Típushiba.
    /// </summary>
    public class TypeError : CompilerError
    {
        /// <summary>
        /// Az elvárt típus neve.
        /// </summary>
        public string Expected { get; set; }
        /// <summary>
        /// A kapott típus neve.
        /// </summary>
        public string Got { get; set; }

        public override void Show()
        {
            Console.WriteLine($"Type error: can't cast {Got} to {Expected}!");
        }
    }

    // TODO: Helyadatok?
    /// <summary>
    /// Szintén tipikus futásidejű hiba: Ismeretlen szimbólum.
    /// </summary>
    public class SymbolNotFoundError : CompilerError
    {
        /// <summary>
        /// A nem talált szimbólum neve.
        /// </summary>
        public string Name { get; set; }

        public override void Show()
        {
            Console.WriteLine($"Error: no such symbol '{Name}'!");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Utilities;

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
        /// <summary>
        /// A szkóp őse.
        /// </summary>
        public Scope Parent { get; }

        private Dictionary<string, Symbol> symbols = new Dictionary<string, Symbol>();

        /// <summary>
        /// Létrehoz egy új szkópot az adott ős szkóppal.
        /// </summary>
        /// <param name="parent">Az ős szkóp.</param>
        public Scope(Scope parent = null)
        {
            Parent = parent;
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
        public Symbol Reference(Token name)
        {
            if (symbols.TryGetValue(name.Value, out var result))
            {
                return result;
            }
            if (Parent != null)
            {
                return Parent.Reference(name);
            }
            throw new SymbolNotFoundError(name);
        }
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

    /// <summary>
    /// Szintén tipikus futásidejű hiba: Ismeretlen szimbólum.
    /// </summary>
    public class SymbolNotFoundError : CompilerError
    {
        /// <summary>
        /// A nem talált szimbólum azonosítója a szintaxisfában.
        /// </summary>
        public Token Name { get; }

        public SymbolNotFoundError(Token name)
        {
            Name = name;
        }

        public override void Show()
        {
            Console.WriteLine($"Error: no such symbol '{Name.Value}' referenced at {Name.Position}!");
            Console.WriteLine(Name.Source.AnnotateAt(Name.Position));
        }
    }
}

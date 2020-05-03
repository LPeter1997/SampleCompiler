using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace Compiler
{
    /// <summary>
    /// A token egy absztrakció a forrászszöveg fölött, az elezés szempontjából egységnek tekinthető
    /// elemek. Ilyen egy zárójel, kulcsszó, azonosító, stb.
    /// 
    /// Részletesebb magyarázathoz lásd a Lexer dokumentációját.
    /// </summary>
    public class Token
    {
        /// <summary>
        /// A token forrása.
        /// </summary>
        public PosTextReader Source { get; set; }
        /// <summary>
        /// A token szöveges pozíciója.
        /// </summary>
        public Position Position { get; set; }
        /// <summary>
        /// A token kategóriája (vagy típusa).
        /// </summary>
        public TokenType Type { get; set; }
        /// <summary>
        /// A token szöveges értéke.
        /// </summary>
        public string Value { get; set; }

        public override string ToString()
        {
            return $"'{Value}' - {Type} ({Position})";
        }
    }

    /// <summary>
    /// A lehetséges token kategóriák (vagy token típusok).
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// Egy szimbólumot azonosító név.
        /// </summary>
        Identifier,

        /// <summary>
        /// Egész szám.
        /// </summary>
        Integer,

        /// <summary>
        /// Karakterlánc.
        /// </summary>
        String,
        
        // Kulcsszavak
        KwFunction, // 'function'
        KwIf,       // 'if'
        KwElse,     // 'else'
        KwWhile,    // 'while'
        KwFor,      // 'for'
        KwVar,      // 'var'
        KwReturn,   // 'return'

        KwTrue,  // 'true'
        KwFalse, // 'false'

        // Elválasztók
        Comma, Semicolon, // , ;
        
        // Páros jelek
        OpenBrace, CloseBrace, // { }
        OpenParen, CloseParen, // ( )
        
        // Relációk
        Greater, GreaterOrEqual, // > >=
        Less, LessOrEqual,       // < <=
        Equal, NotEqual,         // == !=

        // Egy, vagy kétoperandusú műveleti jelek
        Assign,                   // =
        Add, Subtract,            // + -
        Multiply, Divide, Modulo, // * / %
        Not, And, Or,             // ! && ||

        // Összetett (angolul compound) operátorok
        AddAssign, SubtractAssign, // += -=
        MultiplyAssign, DivideAssign, ModuloAssign, // *= /= %=

        /// <summary>
        /// Forrásszöveg vége.
        /// </summary>
        EndOfSource,
    }
}

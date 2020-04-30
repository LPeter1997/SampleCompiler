﻿using System;
using System.IO;
using Utilities;

namespace Compiler
{
    /// <summary>
    /// Hibát reprezentál valamilyen fordítási szakaszban.
    /// </summary>
    public abstract class CompilerError : Exception
    {
        /// <summary>
        /// Kiírja a hibaüzenetet a konzolra.
        /// </summary>
        public abstract void Show();
    }

    class Program
    {
        static void Main(string[] args)
        {
            string source = File.ReadAllText("Samples/factorial.silang");

            try
            {
                var tokens = Lexer.Lex(source);
                var ast = Parser.ParseProgram(tokens);
                //Console.WriteLine(ast.ToJson());
                TreeWalkInterpreter.RunProgram(ast);
            }
            catch (CompilerError e)
            {
                e.Show();
            }
        }
    }
}

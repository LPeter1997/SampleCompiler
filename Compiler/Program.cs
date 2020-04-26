using System;
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
            string source = @"
function max(x, y) {
    if x > y {
        return x;
    }
    else {
        return y;
    }
}
";

            try
            {
                var tokens = Lexer.Lex(source);
                var ast = Parser.ParseProgram(tokens);
                Console.WriteLine(ast.ToJson());
            }
            catch (CompilerError e)
            {
                e.Show();
            }
        }
    }
}

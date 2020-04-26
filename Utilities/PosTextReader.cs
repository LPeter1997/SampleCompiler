using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Utilities
{
    /// <summary>
    /// Szöveges pozíció ábrázolása.
    /// </summary>
    public struct Position
    {
        /// <summary>
        /// Kezdőpozíció, első sor első karaktere.
        /// </summary>
        public static Position Zero => new Position { Line = 0, Character = 0 };

        /// <summary>
        /// Az oszlop, 0-tól kezdve.
        /// </summary>
        public int Character { get; set; }
        /// <summary>
        /// A sor, 0-tül kezdve.
        /// </summary>
        public int Line { get; set; }

        public override string ToString()
        {
            return $"line {Line + 1}, character {Character + 1}";
        }
    }

    /// <summary>
    /// Egy szöveg olvasó, mely számontartja az aktuális karakter szöveges pozícióját.
    /// Minden újsor szekvenciát a UNIX-on megszokott '\n'-el helyettesít.
    /// </summary>
    public class PosTextReader
    {
        private string text;
        private int index;
        private Position position;
        private List<int> lineStarts;

        /// <summary>
        /// Igaz, ha a szöveg végére értünk.
        /// </summary>
        public bool IsEmpty => text.Length <= index;

        /// <summary>
        /// Az aktuális szöveges pozíció.
        /// </summary>
        public Position Position => position;

        /// <summary>
        /// Egy szöveg olvasóva burkolja az adott szöveget.
        /// </summary>
        /// <param name="text">A beburkolandó szöveg.</param>
        public PosTextReader(string text)
        {
            this.text = NormalizeNewlines(text);
            this.index = 0;
            this.position = Position.Zero;
            this.lineStarts = FindLineStarts(this.text);
        }

        private static string NormalizeNewlines(string text)
        {
            return text
                // Windows
                .Replace("\r\n", "\n")
                // OS-X 9 and before
                .Replace("\r", "\n");
        }

        private static List<int> FindLineStarts(string text)
        {
            List<int> result = new List<int> { 0 };
            int last = 0;
            while (true)
            {
                int next = text.IndexOf('\n', last);
                if (next == -1)
                {
                    break;
                }
                last = next + 1;
                result.Add(last);
            }
            return result;
        }

        /// <summary>
        /// Előretekint a bemenetben adott mennyiségű karakterrel.
        /// </summary>
        /// <param name="offset">Az előretekintett karakterek száma az aktuális pozíciótól.</param>
        /// <returns>Az előretekintett karakter.</returns>
        public char Peek(int offset = 0)
        {
            return this.text[this.index + offset];
        }

        /// <summary>
        /// Előre mozdítja a pozíciót adott mennyiségű karakterrel.
        /// </summary>
        /// <param name="amount">Az előrelépés mennyisége.</param>
        /// <returns>A levágott szövegrész.</returns>
        public string Consume(int amount = 1)
        {
            string result = string.Empty;
            for (int i = 0; i < amount; ++i)
            {
                char ch = Peek();
                result += ch;
                ++this.index;
                if (ch == '\n')
                {
                    this.position.Character = 0;
                    ++this.position.Line;
                }
                else
                {
                    ++this.position.Character;
                }
            }
            return result;
        }

        /// <summary>
        /// Visszatér az adott indexű sorral.
        /// </summary>
        /// <param name="line">A kért sor sorszáma.</param>
        /// <returns>A kért sor.</returns>
        public string Line(int line)
        {
            if (line >= this.lineStarts.Count)
            {
                return string.Empty;
            }
            int from = this.lineStarts[line];
            int to = this.text.Length;
            if (line + 1 < this.lineStarts.Count)
            {
                to = this.lineStarts[line + 1];
            }
            return this.text.Substring(from, to - from);
        }

        /// <summary>
        /// Ellenőrzi, hogy a szöveg elején egy adott szövegrész áll-e.
        /// </summary>
        /// <param name="text">Az elvárt szövegrész.</param>
        /// <returns>Igaz, ha egyezik a szöveg eleje és a szövegrész.</returns>
        public bool StartsWith(string text)
        {
            // NOTE: Quite inefficient
            return this.text.Substring(this.index).StartsWith(text);
        }

        /// <summary>
        /// Megpróbál egy reguláris kifejezést illeszteni a forrésszöveg elejére.
        /// </summary>
        /// <param name="regex">Az illesztendő reguláris kifejezés.</param>
        /// <param name="length">Siker esetén az illesztett szöveg hossza.</param>
        /// <returns>Igaz, ha az illesztés sikeres.</returns>
        public bool MatchesStart(Regex regex, out int length)
        {
            length = 0;
            if (!regex.IsMatch(this.text, this.index))
            { 
                return false; 
            }
            var match = regex.Match(this.text, this.index);
            if (match.Success && match.Index == this.index)
            {
                length = match.Length;
                return true;
            }
            return false;
        }
    }
}

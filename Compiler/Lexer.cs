using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Utilities;

namespace Compiler
{
    /// <summary>
    /// A lexer, vagy lexikai elemző feladata a forrást olyan részekre bontani, melyekről
    /// érdemes logikai egységként beszélni a bonyolultabb nyelvtani elemzéskor. Ezeket a
    /// logikai egységeket hívjuk tokeneknek. 
    /// 
    /// Tipikusan ilyen egy kulcsszó, egy zárójel vagy egy név.
    /// 
    /// A tokeneket általában ellátjuk egy kategóriával is, hogy a további szintaktikai 
    /// elemzéseknek ne a szöveges reprezentációval kelljen dolgozni. A szintaktikai elemzőt
    /// például nem érdekli hogy ha számot vár, akkor "123"-mat, vagy "678"-at írtunk, csupán
    /// az érdekli, hogy szám.
    /// 
    /// Ebben a lépésben hagyjuk ki a nyelv számára nem lényeges elemeket, mint a szünetet,
    /// új sort, vagy kommenteket. Ezután a lépés után a szintaktikai elemzésnek ilyesmivel
    /// már nem kell foglalkoznia.
    /// 
    /// Példa, az alábbi C forrást:
    /// 
    /// ```
    /// int addone(int x) { return x + 1; }
    /// ```
    /// 
    /// Ezekre a (lehetséges) tokenekre bonthatjuk (szöveges érték - kategória):
    ///   'int'    - Azonosító
    ///   'addone' - Azonosító
    ///   '('      - Nyitó zárójel
    ///   'int'    - Azonosító
    ///   'x'      - Azonosító
    ///   ')'      - Záró zárójel
    ///   '{'      - Nyitó kapocs
    ///   'return' - Visszatér kulcsszó
    ///   'x'      - Azonosító
    ///   '+'      - Összeadás jel
    ///   '1'      - Egész szám
    ///   ';'      - Pontosvessző
    ///   '}'      - Záró kapocs
    ///   
    /// A klasszikus matematikai modell a lexikai analízisre a véges állapotú gép, illetve ehhez
    /// kapcsolódóan a reguláris kifejezések. A gyakorlatban igen egyszerű 'manuálisan' leprogramozni,
    /// ami az esetek többségében sokkal hatékonyabb lesz, illetve kezel nemreguláris eseteket, például
    /// az egyre népszerűbb beágyazott kommenteket.
    /// 
    /// Megjegyzés 1: Egy szofisztikáltabb fordító vagy nyelvi eszköz nem feltétlenül dobja el
    /// a kommenteket. Egy formázó például nem teheti ezt meg. Az ilyen nyelvi eszközök sokkal
    /// komolyabb szintaxisfa dizájnt is igényelnek.
    /// 
    /// Megjegyzés 2: Ezoterikus esetekben a szintaktikai elemzés következő lépésénél is szükség 
    /// lehet a szünetekre. Ilyen nyelv például a Python. Ezeket "whitespace" nyelveknek hívjuk.
    /// 
    /// Megjegyzés 3: A 'true' és 'false' kulcsszó, vagy konstans? Mindkettő racionális hozzáállás,
    /// a nyelv filozófiájától függ. Konstansként kifejezve a nyelv megpróbálja a saját építőköveit
    /// definiálni, melynek ugyanúgy megvannak az előnyei, mint a hátrányai.
    /// </summary>
    public class Lexer
    {
        private List<Regex> ignores = new List<Regex>();
        private SortedList<string, TokenType> keywords = new SortedList<string, TokenType>(new StringLengthComparer());
        private List<(Regex, TokenType)> regexes = new List<(Regex, TokenType)>();

        /// <summary>
        /// Tokenekre bontja az adot forrásszöveget az alapvető token szabályok segítségével.
        /// </summary>
        /// <param name="source">A felbontandó forrásszöveg.</param>
        public static List<Token> Lex(string source)
        {
            var lexer = new Lexer();
            lexer.AddBasicRules();
            return lexer.Tokenize(source);
        }

        /// <summary>
        /// Regisztrálja az alapvető token szabályokat.
        /// </summary>
        /// <param name="source"></param>
        public void AddBasicRules()
        {
            AddIgnore(" |\n|\r|\t");
            AddIgnore("//.+\n");

            AddRegex("[A-Za-z_][A-Za-z0-9_]*", TokenType.Identifier);
            AddRegex("[0-9]+", TokenType.Integer);
            AddRegex(@"'(\\.|[^'])*'", TokenType.String);

            AddKeyword("function", TokenType.KwFunction);
            AddKeyword("if", TokenType.KwIf);
            AddKeyword("else", TokenType.KwElse);
            AddKeyword("while", TokenType.KwWhile);
            AddKeyword("for", TokenType.KwFor);
            AddKeyword("var", TokenType.KwVar);
            AddKeyword("return", TokenType.KwReturn);

            AddKeyword("true", TokenType.KwTrue);
            AddKeyword("false", TokenType.KwFalse);

            AddKeyword(",", TokenType.Comma);
            AddKeyword(";", TokenType.Semicolon);

            AddKeyword("{", TokenType.OpenBrace);
            AddKeyword("}", TokenType.CloseBrace);
            AddKeyword("(", TokenType.OpenParen);
            AddKeyword(")", TokenType.CloseParen);

            AddKeyword(">", TokenType.Greater);
            AddKeyword(">=", TokenType.GreaterOrEqual);
            AddKeyword("<", TokenType.Less);
            AddKeyword("<=", TokenType.LessOrEqual);
            AddKeyword("==", TokenType.Equal);
            AddKeyword("!=", TokenType.NotEqual);

            AddKeyword("+=", TokenType.AddAssign);
            AddKeyword("-=", TokenType.SubtractAssign);
            AddKeyword("*=", TokenType.MultiplyAssign);
            AddKeyword("/=", TokenType.DivideAssign);
            AddKeyword("%=", TokenType.ModuloAssign);

            AddKeyword("=", TokenType.Assign);
            AddKeyword("+", TokenType.Add);
            AddKeyword("-", TokenType.Subtract);
            AddKeyword("*", TokenType.Multiply);
            AddKeyword("/", TokenType.Divide);
            AddKeyword("%", TokenType.Modulo);

            AddKeyword("!", TokenType.Not);
            AddKeyword("&&", TokenType.And);
            AddKeyword("||", TokenType.Or);
        }

        /// <summary>
        /// Regisztrál egy kihagyandó reguláris kifejezést, mely nem eredményez tokent. A legtöbb
        /// nyelvben ilyen a szóköz, újsor vagy a kommentek.
        /// </summary>
        /// <param name="regex">A kihagyandó szövegrész reguláris kifejezése.</param>
        public void AddIgnore(string regex)
        {
            ignores.Add(new Regex(regex, RegexOptions.Compiled));
        }

        /// <summary>
        /// Regisztrál egy új tokent ami pontos szöveges egyezésre érvényesül. Ilyenek lehetnek a
        /// kulcsszavak, mint az 'if', vagy az egyéb jelek, mint a '(', '.', stb.
        /// </summary>
        /// <param name="keyword">A pontos szöveg a token egyezéséhez.</param>
        /// <param name="type">Az eredményezett token típus.</param>
        public void AddKeyword(string keyword, TokenType type)
        {
            keywords.Add(keyword, type);
        }

        /// <summary>
        /// Regisztrál egy új tokent ami illeszkedik az adott reguláris kifejezésre. Tipikusan ilyenek
        /// az azonosítók vagy számok.
        /// </summary>
        /// <param name="regex">Az illesztő reguláris kifejezés.</param>
        /// <param name="type">Az eredményezett token típus.</param>
        public void AddRegex(string regex, TokenType type)
        {
            regexes.Add((new Regex(regex, RegexOptions.Compiled), type));
        }

        /// <summary>
        /// Tokenekre bontja az adott forrásszöveget a regisztrált szabályok segítségével.
        /// </summary>
        /// <param name="source">A felbontandó forrásszöveg.</param>
        /// <returns>A felbontott tokenek listája.</returns>
        public List<Token> Tokenize(string source)
        {
            List<Token> result = new List<Token>();
            PosTextReader reader = new PosTextReader(source);
            while (true)
            {
                var tok = NextToken(reader);
                result.Add(tok);
                if (tok.Type == TokenType.EndOfSource)
                {
                    break;
                }
            }
            return result;
        }

        private Token NextToken(PosTextReader reader)
        {
            Func<TokenType, int, Token> NewToken = (type, length) =>
            {
                var pos = reader.Position;
                var val = reader.Consume(length);
                return new Token
                {
                    Source = reader,
                    Position = pos,
                    Value = val,
                    Type = type,
                };
            };
            while (true)
            {
                start:
                if (reader.IsEmpty)
                {
                    return NewToken(TokenType.EndOfSource, 0);
                }
                foreach (var ignore in this.ignores)
                {
                    if (reader.MatchesStart(ignore, out var len))
                    {
                        reader.Consume(len);
                        goto start;
                    }
                }
                foreach (var kw in this.keywords)
                {
                    if (reader.StartsWith(kw.Key))
                    {
                        return NewToken(kw.Value, kw.Key.Length);
                    }
                }
                foreach (var rx in this.regexes)
                {
                    if (reader.MatchesStart(rx.Item1, out var len))
                    {
                        return NewToken(rx.Item2, len);
                    }
                }
                throw new UnknownCharacterError(reader.Peek(), reader.Position, reader);
            }
        }

        private class StringLengthComparer : IComparer<string>
        {
            public int Compare([AllowNull] string x, [AllowNull] string y)
            {
                var cmp1 = y.Length.CompareTo(x.Length);
                if (cmp1 != 0)
                {
                    return cmp1;
                }
                return x.CompareTo(y);
            }
        }
    }

    /// <summary>
    /// Gyakran az egyetlen igazi lexikai elemzési hiba, egy ismeretlen karakter.
    /// </summary>
    public class UnknownCharacterError : CompilerError
    {
        /// <summary>
        /// Az ismeretlen karakter.
        /// </summary>
        public char Token { get; }
        /// <summary>
        /// Az ismeretlen karakter szöveges pozíciója.
        /// </summary>
        public Position Position { get; }
        /// <summary>
        /// A forrás olvasó, amiből a hiba ered.
        /// </summary>
        new public PosTextReader Source { get; }

        public UnknownCharacterError(char token, Position position, PosTextReader source)
        {
            Token = token;
            Position = position;
            Source = source;
        }

        /// <summary>
        /// Kiírja a konzolra a hibaüzenetet.
        /// </summary>
        public override void Show()
        {
            Console.WriteLine($"Syntax error: Unknown token {Token} (code: {(int)Token}) at {Position}.");
            Console.WriteLine(Source.AnnotateAt(Position));
        }
    }
}

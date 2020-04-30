using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
{
    /// <summary>
    /// A fordítók egyik legfontosabb adatszerkezete az ún. absztrakt szintaxisfa, röviden AST
    /// (az Abstract Syntax Tree elnevezésből).
    /// Az AST a forrás tokenek olyan fa-szerkezetbe rendezése, hogy a nyelvi elemek között
    /// egyértelmű a reláció. Például egy if-else elágazásnál egyértelmű, hogy mi a feltétel,
    /// mi a 'then' kódblokk és mi az 'else' kódblokk.
    /// 
    /// Az AST-t általában az ún. Parser, vagy elemző állítja elő a tokenek listájából.
    /// 
    /// Példa az alábbi C kód:
    /// 
    /// ```
    /// if (x > 0) {
    ///     return x;
    /// }
    /// else {
    ///     return -x;
    /// }
    /// ```
    /// 
    /// Egy lehetséges szintaxisfája:
    /// 
    ///            ____if______
    ///           /     |      \
    ///          >   return  return 
    ///         / \     |       |
    ///        x   0    x       -
    ///                         |
    ///                         x
    ///        
    /// Megjegyzés 1: A szintaxisfa egyértelműen definiálja a matematikai műveletek 
    /// precedenciáját és asszociativitását is. Például az 5 + 6 * 4 + 2 szintaxisfája:
    /// 
    ///          +
    ///         / \
    ///        +   2
    ///       / \
    ///      5   *
    ///         / \
    ///        6   4
    ///        
    /// Látható, a magasabb precedencia a fában mélyebb elhelyezkedést eredményez, míg a
    /// bal-asszociáció balra-mélyülő fát. A kiértékeléshez mindőssze a fa aljából 
    /// kiindulva kell kiértékelnünk a műveleteket.
    /// 
    /// Nézzünk egy jobb-asszociatív operátort, mint a hozzárendelés. Az 
    /// x = y = z = w kifejezés szintaxisfája:
    /// 
    ///        =
    ///       / \
    ///      x   =
    ///         / \
    ///        y   =
    ///           / \
    ///          z   w
    ///          
    /// Milyen érdekes, a fa jobbra mélyül!
    /// 
    /// Megjegyzés 2: Szokás különbséget tenni szintaxisfa (parse-tree) és absztrakt 
    /// szintaxisfa (AST) között. Az előbbi általában egy nyers elemzés eredménye, mely
    /// minden nyelvi részletet megtart. Az utóbbi elvonatkoztat a nyelv szintaktikájától 
    /// (ettől absztrakt), és a redundáns jelöléseket elveti. Fölölsleges például a 
    /// zárójeleket tárolni egy matematikai művelethez, mikor a fa struktúrája egyértelműen
    /// kódolja a kiértékelés sorrendjét.
    /// </summary>
    public abstract class AstNode
    {
        /// <summary>
        /// A legtöbb esetben jó, ha vizualizálhatjuk a szintaxisfánkat. Erre tökéletes
        /// a JSON és például ez az online tool: http://jsonviewer.stack.hu/
        /// </summary>
        /// <param name="node">A szintaxisfa csomópont amit JSON-ná akarunk alakítani.</param>
        /// <returns>A JSON reprezentációja a (rész)fának.</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    /// <summary>
    /// Állításnak, utasításnak vagy parancsnak hívjuk azokat a szerkezeteket a programban, 
    /// melyek nem járnak eredménnyel, csupán mellékhatással, vagy a számítási útvonal 
    /// befolyásolásával.
    /// Klasszikusan a C-szerű nyelvekben ilyen az elágazás, a ciklusok, változó definiálás,
    /// stb.
    /// </summary>
    public abstract class Statement : AstNode
    {
    }

    /// <summary>
    /// A kifejezések olyan szerkezetek, melyek valamilyen eredményt adnak vissza. Ilyen 
    /// egy összeadás, vagy egy függvényhívás.
    /// </summary>
    public abstract class Expression : AstNode
    {
    }

    // Alább a kifejezések definíciója következik /////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Egy egyszerű egész szám.
    /// </summary>
    public class IntegerLiteralExpression : Expression
    {
        /// <summary>
        /// A számértéket reprezentáló token.
        /// </summary>
        public Token Value { get; set; }
    }

    /// <summary>
    /// Egy igaz-hamis érték, mely a kódban egy 'true' vagy 'false' literálként jelent meg.
    /// </summary>
    public class BoolLiteralExpression : Expression
    {
        /// <summary>
        /// A logikai értékek reprezentáló token.
        /// </summary>
        public Token Value { get; set; }
    }

    /// <summary>
    /// Egy karakterlánc literál.
    /// </summary>
    public class StringLiteralExpression : Expression
    {
        /// <summary>
        /// A karakterláncot reprezentáló token.
        /// </summary>
        public Token Value { get; set; }

        /// <summary>
        /// A literál karakterláncot egy natív C# karakterlánccá alakítja.
        /// </summary>
        /// <returns></returns>
        public string Unescape()
        {
            string result = string.Empty;
            for (int i = 1; i < Value.Value.Length - 1;)
            {
                char ch = Value.Value[i];
                if (ch == '\\')
                {
                    // Escaped character
                    char esc = '\0';
                    switch (Value.Value[i + 1])
                    {
                        case '\'': esc = '\''; break;
                        case '0': esc = '\0'; break;
                        case 't': esc = '\t'; break;
                        case 'n': esc = '\n'; break;
                    }
                    result += esc;
                    i += 2;
                }
                else
                {
                    result += ch;
                    ++i;
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Egy változó nevének leírása a forrásban.
    /// </summary>
    public class VariableExpression : Expression
    {
        /// <summary>
        /// A hivatkozott változó neve.
        /// </summary>
        public Token Identifier { get; set; }
    }

    /// <summary>
    /// Egy egyoperandusú művelet, mint a '-x', '+x' vagy 'not x'.
    /// 
    /// Megjegyzés: Én itt lusta módon egybe vettem az összes egyoperandusú kifejezést,
    /// pedig egy teljesen jogos hozzáállás, hogy ezeknek külön típusoknak kéne lennie, mint
    /// `NotExpression`, `NegateExpression`, stb... Mindkettőnek megvan a maga előnye és 
    /// hátránya.
    /// </summary>
    public class UnaryExpression : Expression
    {
        /// <summary>
        /// Az operátor.
        /// </summary>
        public Token Operator { get; set; }
        /// <summary>
        /// A kifejezés, amin elvégezzük a műveletet.
        /// </summary>
        public Expression Subexpression { get; set; }
    }

    /// <summary>
    /// Egy kétoperandusú művelet, mint az 'x + y'.
    /// 
    /// Ugyanaz a megjegyzés vonatkozik ide is, mint az egyoperandusú csomópont definiálásánál.
    /// </summary>
    public class BinaryExpression : Expression
    {
        /// <summary>
        /// Az operátor.
        /// </summary>
        public Token Operator { get; set; }
        /// <summary>
        /// A bal oldali kifejezés.
        /// </summary>
        public Expression Left { get; set; }
        /// <summary>
        /// A jobb oldali kifejezés.
        /// </summary>
        public Expression Right { get; set; }
    }

    /// <summary>
    /// Egy függvényhívás.
    /// 
    /// Megjegyzés: Ha nagyon nagyfiús fordítót írnánk operátor túlterheléssel és típus inferenciával,
    /// az egyoperandusú és kétoperandusú műveleteket is érdemes volna függvényhívásként reprezentálni.
    /// </summary>
    public class CallExpression : Expression
    {
        /// <summary>
        /// A hívott függvény.
        /// </summary>
        public Expression Function { get; set; }
        /// <summary>
        /// Az átadott argumentum értékek.
        /// </summary>
        public List<Expression> Arguments { get; set; }
    }

    // Alább az utasítások definíciója következik /////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Egy elágazást reprezentál.
    /// </summary>
    public class IfStatement : Statement
    {
        /// <summary>
        /// A feltétel, ami eldönti, melyik ágat futtatjuk le.
        /// </summary>
        public Expression Condition { get; set; }
        /// <summary>
        /// Az ág, ami abban az esetben fut le, ha a feltétel igaz.
        /// </summary>
        public Statement Then { get; set; }
        /// <summary>
        /// Az ág, ami abban az esetben fut le, ha a feltétel hamis.
        /// </summary>
        public Statement Else { get; set; }
    }

    /// <summary>
    /// Egy feltételes ciklus.
    /// </summary>
    public class WhileStatement : Statement
    {
        /// <summary>
        /// A feltétel, ami eldönti, lefuttatjuk-e a tárolt ciklus testet.
        /// </summary>
        public Expression Condition { get; set; }
        /// <summary>
        /// Az ismételt kódblokk a ciklusban.
        /// </summary>
        public Statement Body { get; set; }
    }

    /// <summary>
    /// Egy függvény definíciója.
    /// 
    /// Megjegyzés: Ez egy játék dizájn. A legtöbb mai nyelv a sorrend-független
    /// definíciókat elkülöníti és nem utasításként dolgozza fel. Nekünk ideiglenesen 
    /// megfelel.
    /// </summary>
    public class FunctionDefinitionStatement : Statement
    {
        /// <summary>
        /// A definiált függvény neve.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// A definiált függvény paramétereinek a neve.
        /// </summary>
        public List<string> Parameters { get; set; }
        /// <summary>
        /// A függvény teste.
        /// </summary>
        public Statement Body { get; set; }
    }

    /// <summary>
    /// Egy visszatérési utasítás lehetséges visszatérési értékkel.
    /// </summary>
    public class ReturnStatement : Statement
    {
        /// <summary>
        /// A visszatérési érték, vagy null ha nincs.
        /// </summary>
        public Expression Value { get; set; }
    }

    /// <summary>
    /// Egy változó definiálása és inicializálása egy kezdeti értékkel.
    /// </summary>
    public class VarDefinitionStatement : Statement
    {
        /// <summary>
        /// A definiált változó neve.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// A definiált változó értéke.
        /// </summary>
        public Expression Value { get; set; }
    }

    /// <summary>
    /// Egy összetett utasítás, mely több utasításból áll. Így például egy
    /// elágazás ágai ugyanúgy tartalmazhatnak egy egyszerű utasítást, vagy akár
    /// egy utasítás blokkot, melyet összetett utasításként tárolunk.
    /// </summary>
    public class CompoundStatement : Statement
    {
        /// <summary>
        /// Az utasítások, melyekből az összetett utasítás áll.
        /// </summary>
        public List<Statement> Statements { get; set; }
    }

    /// <summary>
    /// Egy utasításba burkolt kifejezés. Sokszor írunk le kifejezést utasítás szerepébe,
    /// gondoljunk csak egy függvényhívásra, melynek csak mellékhatása van. A nyelv szerint
    /// kifejezés, viszont nem egy változó definíció jobb oldalára fogjuk tenni, hanem 
    /// önmagában szerepeltetjük, mint utasítást.
    /// 
    /// Érdekesség: Szinte az összes klasszikus fordító definiál valami hasonlót.
    /// </summary>
    public class ExpressionStatement : Statement
    {
        /// <summary>
        /// A kiértékelendő kifejezés.
        /// </summary>
        public Expression Expression { get; set; }
    }
}

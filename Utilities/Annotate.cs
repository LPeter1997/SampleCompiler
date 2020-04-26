using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities
{
    public static class AnnotationExtensions
    {
        /// <summary>
        /// Annotálja a forrásszöveg adott pozícióját. Főleg hibaüzenetekhez,
        /// figyelmeztetésekhez.
        /// </summary>
        /// <param name="this">A szöveg olvasó, ami az annotálandó forrásszöveget tartalmazza.</param>
        /// <param name="position">Az annotálandó szöveges pozíció.</param>
        /// <returns>Az annotált szöveg.</returns>
        public static string AnnotateAt(this PosTextReader @this, Position position)
        {
            var lineText = @this.Line(position.Line);
            var annotText = new string('_', position.Character) + '^';
            return lineText + annotText;
        }
    }
}

using System;
using System.Collections.Generic;

namespace Utilities
{
    /// <summary>
    /// Egy egyszerű, csak olvasható lista nézet.
    /// </summary>
    /// <typeparam name="T">A listaelem típusa.</typeparam>
    public struct ListView<T>
    {
        private List<T> list;
        private int offset;
        private int length;

        /// <summary>
        /// A lista nézet által elérhető elemek száma.
        /// </summary>
        public int Count => length;

        private ListView(List<T> list, int offset, int length)
        {
            this.list = list;
            this.offset = offset;
            this.length = length;
        }

        /// <summary>
        /// Becsomagol egy listát egy lista nézetbe.
        /// </summary>
        /// <param name="list">A becsomagolandó C# lista.</param>
        public ListView(List<T> list)
            : this(list, 0, list.Count)
        {
        }

        /// <summary>
        /// Visszaadja a nézet első elemét, vagy egy alapértelmezett értéket, ha a lista üres.
        /// </summary>
        /// <param name="default">Az alapértelmezett érték üres lista esetén.</param>
        /// <returns>A lista nézet első eleme, vagy az alapértelmezett érték, ha a lista üres.</returns>
        public T FirstOrDefault(T @default)
        {
            if (this.length <= 0)
            {
                return @default;
            }
            return this.list[this.offset];
        }

        /// <summary>
        /// Visszaadja a nézet utolsó elemét, vagy egy alapértelmezett értéket, ha a lista üres.
        /// </summary>
        /// <param name="default">Az alapértelmezett érték üres lista esetén.</param>
        /// <returns>A lista nézet utolsó eleme, vagy az alapértelmezett érték, ha a lista üres.</returns>
        public T LastOrDefault(T @default)
        {
            if (this.length <= 0)
            {
                return @default;
            }
            return this.list[this.offset + this.length - 1];
        }

        /// <summary>
        /// Visszaad egy lista nézetet, melyben az első adott elemek törlésre kerülnek.
        /// Ha az eltávolítandó elemek száma nagyobb vagy egyenlő a lista nézet elemszámával,
        /// üres lista lesz az eredmény.
        /// </summary>
        /// <param name="amount">Az eltávolítandó elemek száma.</param>
        /// <returns>Az új lista nézet az első elemek nélkül.</returns>
        public ListView<T> RemoveFirst(int amount = 1)
        {
            amount = Math.Min(amount, this.length);
            return new ListView<T>(this.list, this.offset + amount, this.length - amount);
        }
    }
}

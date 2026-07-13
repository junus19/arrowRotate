using System.Collections.Generic;

namespace ArrowRotate.Core
{
    /// <summary>
    /// Prototipteki mulberry32 PRNG'nin birebir portu (SKILL.md §7).
    /// Aynı seed → prototiple aynı rastgele dizi (level parity + hata reprosu için).
    /// </summary>
    public sealed class Mulberry32
    {
        private uint _t;

        public Mulberry32(int seed)
        {
            unchecked { _t = (uint)seed; }
        }

        /// <summary>[0, 1) aralığında double. JS: mulberry32 gövdesinin birebir karşılığı.</summary>
        public double NextDouble()
        {
            unchecked
            {
                _t += 0x6D2B79F5u;
                uint r = (_t ^ (_t >> 15)) * (1u | _t);
                r ^= r + ((r ^ (r >> 7)) * (61u | r));
                return (r ^ (r >> 14)) / 4294967296.0;
            }
        }

        /// <summary>[a, b] kapalı aralıkta tamsayı (prototipteki ri).</summary>
        public int RangeInclusive(int a, int b) => a + (int)(NextDouble() * (b - a + 1));

        /// <summary>Fisher-Yates (prototipteki shuffle ile aynı çekiliş sırası).</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = (int)(NextDouble() * (i + 1));
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public T Pick<T>(IReadOnlyList<T> list) => list[(int)(NextDouble() * list.Count)];
    }
}

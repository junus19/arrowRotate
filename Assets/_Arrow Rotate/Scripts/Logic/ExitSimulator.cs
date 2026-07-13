using System.Collections.Generic;

namespace ArrowRotate.Logic
{
    /// <summary>
    /// Statik çözülebilirlik simülasyonu (SKILL.md §7 adım 7 / prototip assignIce.valid birebir).
    /// Kural: bir ok çıkabilir ⇔ buz eşiği dolu (freezeAt ≤ çıkan sayısı) VE tüm blockedBy'ları çıkmış.
    /// Bu kuralla tüm oklar sırayla çıkabiliyorsa level (buz dahil) çözülebilirdir.
    /// </summary>
    public static class ExitSimulator
    {
        public static bool CanExitAll(IReadOnlyList<HashSet<int>> blockedBy, IReadOnlyList<int> freezeAt)
        {
            int n = blockedBy.Count;
            var exited = new HashSet<int>();
            int guard = 0;
            while (exited.Count < n && guard++ < n + 2)
            {
                bool progress = false;
                for (int c = 0; c < n; c++)
                {
                    if (exited.Contains(c)) continue;
                    if (freezeAt[c] > exited.Count) continue; // hâlâ buzlu

                    bool allBlockersOut = true;
                    foreach (int b in blockedBy[c])
                    {
                        if (!exited.Contains(b)) { allBlockersOut = false; break; }
                    }
                    if (allBlockersOut)
                    {
                        exited.Add(c);
                        progress = true;
                    }
                }
                if (!progress) return false;
            }
            return exited.Count == n;
        }
    }
}

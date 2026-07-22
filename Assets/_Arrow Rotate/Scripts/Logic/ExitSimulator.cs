using System.Collections.Generic;
using ArrowRotate.Core;

namespace ArrowRotate.Logic
{
    /// <summary>
    /// Statik çözülebilirlik simülasyonu (SKILL.md §7 adım 7 / prototip assignIce.valid birebir).
    /// Kural: bir ok çıkabilir ⇔ buz eşiği dolu (freezeAt ≤ çıkan sayısı) VE tüm blockedBy'ları çıkmış.
    /// Bu kuralla tüm oklar sırayla çıkabiliyorsa level (buz dahil) çözülebilirdir.
    /// KATMANLI leveller için CanExitAllLayered kullan — engelleme dinamiktir (yüzeye çıkan hücre
    /// yeni engel olabilir), statik blockedBy grafiği yetmez.
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

        /// <summary>
        /// KATMANLI dinamik çözülebilirlik: verilen level'ın ÇÖZÜLMÜŞ kopyası (tüm rot=0) üzerinde
        /// tam simülasyon. Bir ok çıkabilir ⇔ buz eşiği dolu VE tüm hücreleri YÜZEYDE VE ışını temiz
        /// (o anki yüzeye göre — RayScanner). Çıkan okun hücreleri silinir, altındakiler terfi eder.
        /// Orijinal level'a dokunmaz. Katmansız levellerde klasik kuralla aynı sonucu verir.
        /// </summary>
        public static bool CanExitAllLayered(HexaLevel source)
        {
            // ── çözülmüş kopya (rot=0) ─────────────────────────────────────────
            var sim = new HexaLevel();
            foreach (var a in source.Arrows)
            {
                var na = new Arrow { ArrowId = a.ArrowId, Palette = a.Palette, FreezeAt = a.FreezeAt, ExitDir = a.ExitDir };
                na.Cells.AddRange(a.Cells);
                sim.Arrows.Add(na);
            }
            void Copy(Cell c) => sim.AddCell(new Cell
            {
                Q = c.Q, R = c.R, ArrowId = c.ArrowId, Type = c.Type, A = c.A, B = c.B, Rot = 0, Layer = c.Layer
            });
            foreach (var c in source.Cells.Values) Copy(c);
            foreach (var list in source.Buried.Values) foreach (var c in list) Copy(c);

            // ── dinamik döngü ─────────────────────────────────────────────────
            int n = sim.Arrows.Count, exitedCount = 0, guard = 0;
            while (exitedCount < n && guard++ <= n + 2)
            {
                bool progress = false;
                foreach (var arrow in sim.Arrows)
                {
                    if (arrow.Exited) continue;
                    if (arrow.FreezeAt > exitedCount) continue;         // hâlâ buzlu
                    if (!sim.IsFullySurfaced(arrow)) continue;          // hücreleri hâlâ gömülü

                    var head = sim.GetCell(arrow.HeadPos);
                    if (!RayScanner.IsClear(sim, arrow.ArrowId, head, head.WorldB)) continue;

                    // çıkar: yüzey hücrelerini sil + terfileri işle
                    arrow.Exited = true;
                    exitedCount++;
                    progress = true;
                    foreach (var pos in arrow.Cells)
                    {
                        sim.Cells.Remove(pos);
                        sim.PromoteAt(pos);
                    }
                }
                if (!progress) return false;
            }
            return exitedCount == n;
        }
    }
}

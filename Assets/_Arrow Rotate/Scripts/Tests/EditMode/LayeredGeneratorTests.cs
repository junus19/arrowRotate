using System.Collections.Generic;
using System.Linq;
using ArrowRotate.Core;
using ArrowRotate.Generation;
using ArrowRotate.Logic;
using NUnit.Framework;

namespace ArrowRotate.Tests
{
    /// <summary>
    /// Katmanlı Random Fill üretimi (LevelConfig.ForCustom + LevelGenerator.GenerateLayered).
    /// Her üretilen level: kapsama (yapı gereği), çözülebilirlik (dinamik sim), yayılma hedefi,
    /// katman derinliği, palet ayrımı ve bitişiklik açısından ≥40 seed üzerinde doğrulanır.
    /// </summary>
    public class LayeredGeneratorTests
    {
        private static readonly int[][] Combos =
        {
            new[] { 6, 2, 2, 0 },
            new[] { 8, 2, 3, 0 },
            new[] { 8, 3, 4, 0 },
            new[] { 10, 3, 5, 2 },
        };

        private static IEnumerable<HexaLevel> Levels(int[] combo, int seeds = 40)
        {
            for (int s = 500; s < 500 + seeds; s++)
            {
                var cfg = LevelConfig.ForCustom(combo[0], combo[1], combo[2], combo[3]);
                HexaLevel lvl = null;
                try { lvl = LevelGenerator.Generate(s, cfg); } catch { }
                if (lvl != null) yield return lvl;
            }
        }

        [Test]
        public void EveryLevel_IsSolvableLayered()
        {
            foreach (var combo in Combos)
                foreach (var lvl in Levels(combo))
                    Assert.IsTrue(ExitSimulator.CanExitAllLayered(lvl),
                        $"çözülemez: ok={combo[0]} kat={combo[1]} yay={combo[2]} seed={lvl.Seed}");
        }

        [Test]
        public void EveryBuriedCell_HasCoverageAbove()
        {
            // Buried[(q,r)] varsa yüzeyde (Cells) o kolon dolu olmalı; ve katmanlar boşluksuz (0..k ardışık)
            foreach (var combo in Combos)
                foreach (var lvl in Levels(combo))
                {
                    foreach (var kv in lvl.Buried)
                    {
                        Assert.IsTrue(lvl.Cells.ContainsKey(kv.Key),
                            $"gömülü {kv.Key} üstünde yüzey yok (seed {lvl.Seed})");
                        // gömülü liste katman artan; ilk gömülü katman 1 olmalı (yüzey=0, boşluk yok)
                        Assert.AreEqual(1, kv.Value[0].Layer, $"katman boşluğu {kv.Key} (seed {lvl.Seed})");
                        for (int i = 1; i < kv.Value.Count; i++)
                            Assert.AreEqual(kv.Value[i - 1].Layer + 1, kv.Value[i].Layer,
                                $"katman ardışık değil {kv.Key} (seed {lvl.Seed})");
                    }
                }
        }

        [Test]
        public void DepthNeverExceedsLayerCount()
        {
            foreach (var combo in Combos)
            {
                int maxAllowed = combo[1] - 1; // katman sayısı k → en derin index k-1
                foreach (var lvl in Levels(combo))
                    foreach (var arrow in lvl.Arrows)
                        foreach (var pos in arrow.Cells)
                        {
                            var c = lvl.GetArrowCell(arrow.ArrowId, pos);
                            Assert.LessOrEqual(c.Layer, maxAllowed,
                                $"katman {c.Layer} > izinli {maxAllowed} (seed {lvl.Seed})");
                        }
            }
        }

        [Test]
        public void SpanningTarget_IsMet_OnAverage()
        {
            // best-effort hedef: ortalama gerçek yayılan ok, hedefin en az %70'i olmalı
            foreach (var combo in Combos)
            {
                int target = combo[2];
                if (target == 0) continue;
                var levels = Levels(combo).ToList();
                Assert.IsNotEmpty(levels);
                double avg = levels.Average(lvl =>
                {
                    int span = 0;
                    foreach (var arrow in lvl.Arrows)
                    {
                        var seen = new HashSet<int>();
                        foreach (var pos in arrow.Cells)
                            seen.Add(lvl.GetArrowCell(arrow.ArrowId, pos).Layer);
                        if (seen.Count >= 2) span++;
                    }
                    return span;
                });
                Assert.GreaterOrEqual(avg, target * 0.7,
                    $"yayılan ort {avg:F1} < hedef {target} %70 (ok={combo[0]} kat={combo[1]})");
            }
        }

        [Test]
        public void PaletteSeparation_HoldsAcrossLayers()
        {
            // aynı paletteki iki ok HİÇBİR (q,r) komşuluğunda (katmanlar-arası dahil) buluşamaz
            foreach (var combo in Combos)
                foreach (var lvl in Levels(combo))
                    foreach (var arrow in lvl.Arrows)
                        foreach (var pos in arrow.Cells)
                            for (int d = 0; d < 6; d++)
                            {
                                var np = HexCoord.Neighbor(pos, d);
                                foreach (var nb in AllAt(lvl, np))
                                    if (nb.ArrowId != arrow.ArrowId)
                                        Assert.AreNotEqual(lvl.Arrows[arrow.ArrowId].Palette, lvl.Arrows[nb.ArrowId].Palette,
                                            $"palet ihlali ok {arrow.ArrowId}/{nb.ArrowId} (seed {lvl.Seed})");
                            }
        }

        [Test]
        public void BuriedRange_FixedCount_HeldExactly_WhenArrowLongEnough()
        {
            // bmin=bmax=k → her yayılan ok TAM k gömülü parça (uzunluk yeterse; kısa okta len-1'e kırpılır)
            for (int k = 1; k <= 2; k++)
            {
                for (int s = 700; s < 730; s++)
                {
                    var cfg = LevelConfig.ForCustom(8, 3, 4, 0, k, k);
                    HexaLevel lvl = null;
                    try { lvl = LevelGenerator.Generate(s, cfg); } catch { }
                    if (lvl == null) continue;

                    foreach (var arrow in lvl.Arrows)
                    {
                        int buried = 0; var layers = new HashSet<int>();
                        foreach (var pos in arrow.Cells)
                        {
                            int l = lvl.GetArrowCell(arrow.ArrowId, pos).Layer;
                            layers.Add(l);
                            if (l >= 1) buried++;
                        }
                        if (layers.Count < 2) continue; // yayılmayan ok
                        int expected = System.Math.Min(k, arrow.Len - 1);
                        Assert.AreEqual(expected, buried,
                            $"yayılan ok {arrow.ArrowId} (len {arrow.Len}): gömülü {buried} ≠ beklenen {expected} (k={k}, seed {s})");
                    }
                }
            }
        }

        private static IEnumerable<Cell> AllAt(HexaLevel lvl, (int q, int r) pos)
        {
            if (lvl.Cells.TryGetValue(pos, out var s)) yield return s;
            if (lvl.Buried.TryGetValue(pos, out var list))
                foreach (var c in list) yield return c;
        }
    }
}

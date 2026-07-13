using System.Collections.Generic;
using System.Linq;
using ArrowRotate.Core;
using ArrowRotate.Generation;
using ArrowRotate.Logic;
using NUnit.Framework;

namespace ArrowRotate.Tests
{
    /// <summary>
    /// SKILL.md §10 — port sırasında YAZILMASI ZORUNLU 6 test sınıfı, her biri ≥100 seed.
    /// Bunların her biri prototip geliştirilirken gerçek bug yakaladı; daima yeşil kalmalı.
    /// </summary>
    [TestFixture]
    public class GeneratorSpecTests
    {
        private const int SeedCount = 100;

        // Zorluk tablosundaki 4 ayrı konfigürasyon (level 1, 2, 3-4, 5+)
        private static readonly int[] ConfigLevels = { 1, 2, 3, 5 };

        private static readonly List<(int level, int seed, HexaLevel lvl)> Cache = new List<(int, int, HexaLevel)>();

        [OneTimeSetUp]
        public void GenerateAll()
        {
            Cache.Clear();
            foreach (int n in ConfigLevels)
            {
                var cfg = LevelConfig.ForLevel(n);
                for (int i = 0; i < SeedCount; i++)
                {
                    int seed = n * 100000 + i;
                    Cache.Add((n, seed, LevelGenerator.Generate(seed, cfg)));
                }
            }
        }

        // ── Test 1: Çözülebilirlik — tüm rot=0 iken HER ok bağlı olmalı ──
        [Test]
        public void Solvability_AllRotZero_EveryArrowConnected()
        {
            foreach (var (n, seed, _) in Cache)
            {
                // üretim deterministik → taze kopya al, karıştırılmış rot'ları sıfırla
                var lvl = LevelGenerator.Generate(seed, LevelConfig.ForLevel(n));
                foreach (var cell in lvl.Cells.Values) cell.Rot = 0;
                foreach (var arrow in lvl.Arrows)
                {
                    Assert.IsTrue(ConnectionTracer.Trace(lvl, arrow.ArrowId).Connected,
                        $"L{n} seed={seed} ok={arrow.ArrowId}: rot=0'da bağlı değil");
                }
            }
        }

        // ── Test 2: Karışıklık — level başlangıcında HİÇBİR ok bağlı olmamalı ──
        [Test]
        public void Scramble_NoArrowConnectedAtStart()
        {
            foreach (var (n, seed, lvl) in Cache)
            {
                foreach (var arrow in lvl.Arrows)
                {
                    Assert.IsFalse(ConnectionTracer.Trace(lvl, arrow.ArrowId).Connected,
                        $"L{n} seed={seed} ok={arrow.ArrowId}: level başında bağlı geldi");
                }
            }
        }

        // ── Test 3: Deadlock yok — buz kuralları dahil tüm oklar çıkabilmeli ──
        [Test]
        public void NoDeadlock_SimulationExitsAllArrows_IncludingIce()
        {
            foreach (var (n, seed, lvl) in Cache)
            {
                var freeze = lvl.Arrows.Select(a => a.FreezeAt).ToArray();
                Assert.IsTrue(ExitSimulator.CanExitAll(lvl.BlockedBy, freeze),
                    $"L{n} seed={seed}: simülasyon kilitlendi (buz dahil)");
            }
        }

        // ── Test 4: Bitişiklik — ardışık hücreler axial komşu, hücre sayısı = uzunluk toplamı ──
        [Test]
        public void Contiguity_ConsecutiveCellsAreNeighbors_TotalMatchesLengths()
        {
            foreach (var (n, seed, lvl) in Cache)
            {
                var cfg = LevelConfig.ForLevel(n);
                foreach (var arrow in lvl.Arrows)
                {
                    for (int i = 0; i + 1 < arrow.Cells.Count; i++)
                    {
                        Assert.GreaterOrEqual(HexCoord.DirBetween(arrow.Cells[i], arrow.Cells[i + 1]), 0,
                            $"L{n} seed={seed} ok={arrow.ArrowId}: {i} ve {i + 1}. hücreler komşu değil");
                    }
                }
                int totalLen = cfg.Lengths.Sum();
                Assert.AreEqual(totalLen, lvl.Cells.Count, $"L{n} seed={seed}: hücre sayısı uzunluk toplamına eşit değil");
                Assert.AreEqual(totalLen, lvl.Arrows.Sum(a => a.Len), $"L{n} seed={seed}: ok uzunlukları toplamı yanlış");
                CollectionAssert.AreEquivalent(cfg.Lengths, lvl.Arrows.Select(a => a.Len).ToArray(),
                    $"L{n} seed={seed}: uzunluk dağılımı config ile eşleşmiyor");
            }
        }

        // ── Test 5: Yapı — tam 1 tail (ilk), tam 1 head (son), arası mid, hepsi aynı arrowId ──
        [Test]
        public void Structure_TailFirstHeadLastMidsBetween_SameArrowId()
        {
            foreach (var (n, seed, lvl) in Cache)
            {
                foreach (var arrow in lvl.Arrows)
                {
                    for (int i = 0; i < arrow.Cells.Count; i++)
                    {
                        var cell = lvl.GetCell(arrow.Cells[i]);
                        Assert.IsNotNull(cell, $"L{n} seed={seed} ok={arrow.ArrowId}: hücre {i} grid'de yok");
                        Assert.AreEqual(arrow.ArrowId, cell.ArrowId, $"L{n} seed={seed}: hücre {i} yanlış arrowId");
                        var expected = i == 0 ? CellType.Tail
                                     : i == arrow.Cells.Count - 1 ? CellType.Head
                                     : CellType.Mid;
                        Assert.AreEqual(expected, cell.Type, $"L{n} seed={seed} ok={arrow.ArrowId} hücre {i}");
                    }
                }
            }
        }

        // ── Test 6: Palet ayrımı — aynı paletteki iki ok hiçbir hücrede komşu değil; buz = 3 ok, eşikler {1,2,3} ──
        [Test]
        public void PaletteSeparation_AndIceAssignment()
        {
            foreach (var (n, seed, lvl) in Cache)
            {
                foreach (var cell in lvl.Cells.Values)
                {
                    for (int d = 0; d < 6; d++)
                    {
                        var nb = lvl.GetCell(HexCoord.Neighbor(cell.Pos, d));
                        if (nb == null || nb.ArrowId == cell.ArrowId) continue;
                        Assert.AreNotEqual(lvl.Arrows[cell.ArrowId].Palette, lvl.Arrows[nb.ArrowId].Palette,
                            $"L{n} seed={seed}: aynı paletteki oklar ({cell.ArrowId},{nb.ArrowId}) komşu — 'kopuk ok' bug'ı!");
                    }
                }

                var cfg = LevelConfig.ForLevel(n);
                var frozen = lvl.Arrows.Where(a => a.FreezeAt > 0).ToList();
                if (cfg.IceCount > 0)
                {
                    Assert.AreEqual(cfg.IceCount, frozen.Count, $"L{n} seed={seed}: buzlu ok sayısı");
                    CollectionAssert.AreEquivalent(
                        Enumerable.Range(1, cfg.IceCount),
                        frozen.Select(a => a.FreezeAt),
                        $"L{n} seed={seed}: buz eşikleri {{1..{cfg.IceCount}}} olmalı");
                }
                else
                {
                    Assert.IsEmpty(frozen, $"L{n} seed={seed}: buzsuz configde buzlu ok var");
                }
            }
        }

        // ── Ek: determinizm — aynı seed + config her zaman aynı level'ı üretmeli ──
        [Test]
        public void Determinism_SameSeedProducesIdenticalLevel()
        {
            foreach (int n in ConfigLevels)
            {
                var cfg = LevelConfig.ForLevel(n);
                int seed = n * 100000;
                var a = LevelGenerator.Generate(seed, cfg);
                var b = LevelGenerator.Generate(seed, cfg);

                Assert.AreEqual(a.Cells.Count, b.Cells.Count);
                foreach (var kv in a.Cells)
                {
                    var cb = b.GetCell(kv.Key);
                    Assert.IsNotNull(cb, $"L{n}: hücre {kv.Key} ikinci üretimde yok");
                    Assert.AreEqual(kv.Value.Type, cb.Type);
                    Assert.AreEqual(kv.Value.ArrowId, cb.ArrowId);
                    Assert.AreEqual(kv.Value.A, cb.A);
                    Assert.AreEqual(kv.Value.B, cb.B);
                    Assert.AreEqual(kv.Value.Rot, cb.Rot);
                }
                for (int i = 0; i < a.Arrows.Count; i++)
                {
                    Assert.AreEqual(a.Arrows[i].Palette, b.Arrows[i].Palette);
                    Assert.AreEqual(a.Arrows[i].FreezeAt, b.Arrows[i].FreezeAt);
                    Assert.AreEqual(a.Arrows[i].ExitDir, b.Arrows[i].ExitDir);
                }
            }
        }
    }
}

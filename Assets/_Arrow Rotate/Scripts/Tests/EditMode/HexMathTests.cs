using System;
using ArrowRotate.Core;
using NUnit.Framework;

namespace ArrowRotate.Tests
{
    /// <summary>
    /// Y-ekseni aynalama sözleşmesini sabitleyen testler (SKILL.md §2 uyarısı).
    /// Bu testler kırılıyorsa görsel katmandaki tüm açı/dönüş varsayımları da kırılır.
    /// </summary>
    public class HexMathTests
    {
        [Test]
        public void Opp_IsInvolutionAndOffsetByThree()
        {
            for (int d = 0; d < 6; d++)
            {
                Assert.AreEqual((d + 3) % 6, HexCoord.Opp(d));
                Assert.AreEqual(d, HexCoord.Opp(HexCoord.Opp(d)));
            }
        }

        [Test]
        public void DirBetween_MatchesDirsTable()
        {
            var origin = (q: 0, r: 0);
            for (int d = 0; d < 6; d++)
            {
                var nb = HexCoord.Neighbor(origin, d);
                Assert.AreEqual(d, HexCoord.DirBetween(origin, nb));
                Assert.AreEqual(HexCoord.Opp(d), HexCoord.DirBetween(nb, origin));
            }
            Assert.AreEqual(-1, HexCoord.DirBetween(origin, (2, 0)), "komşu olmayan çift -1 döndürmeli");
        }

        [Test]
        public void NeighborCenters_LieAtDirAngle_YAxisMirrorContract()
        {
            const float s = 1f;
            var (ox, oy) = HexMetrics.Center(0, 0, s);
            for (int d = 0; d < 6; d++)
            {
                var nb = HexCoord.Neighbor((0, 0), d);
                var (nx, ny) = HexMetrics.Center(nb.q, nb.r, s);
                float actual = (float)(Math.Atan2(ny - oy, nx - ox) * 180.0 / Math.PI);
                float expected = HexMetrics.DirAngleDeg(d);
                float diff = ((actual - expected) % 360f + 540f) % 360f - 180f;
                Assert.LessOrEqual(Math.Abs(diff), 0.01f,
                    $"d={d}: komşu merkezi {actual}°'de, DirAngleDeg {expected}° bekliyordu");
            }
        }

        [Test]
        public void Dir4_PointsUpOnScreen()
        {
            // Spec tablosu: d=4 (0,-1) = "yukarı". Unity'de (y yukarı) komşu merkezi +y'de olmalı.
            var (_, oy) = HexMetrics.Center(0, 0, 1f);
            var nb = HexCoord.Neighbor((0, 0), 4);
            var (_, ny) = HexMetrics.Center(nb.q, nb.r, 1f);
            Assert.Greater(ny, oy, "d=4 ekranda yukarı bakmalı");
        }

        [Test]
        public void RotationZ_IsMinus60PerTap_ScreenClockwise()
        {
            // Ekranda saat yönü 60° = Unity z'de -60° (y-up aynalama sözleşmesi).
            Assert.AreEqual(-60f, HexMetrics.RotationZDeg(1), 1e-4f);
            Assert.AreEqual(-300f, HexMetrics.RotationZDeg(5), 1e-4f);
        }

        [Test]
        public void WorldToAxial_RoundTripsCellCenters_WithJitter()
        {
            const float s = 0.5f;
            float apo = HexMetrics.Apothem(s);
            var rng = new Mulberry32(12345);
            for (int q = -6; q <= 6; q++)
            {
                for (int r = -6; r <= 6; r++)
                {
                    var (cx, cy) = HexMetrics.Center(q, r, s);
                    for (int j = 0; j < 4; j++)
                    {
                        float jx = (float)(rng.NextDouble() * 2 - 1) * apo * 0.4f;
                        float jy = (float)(rng.NextDouble() * 2 - 1) * apo * 0.4f;
                        var (rq, rr) = HexMetrics.WorldToAxial(cx + jx, cy + jy, s);
                        Assert.AreEqual((q, r), (rq, rr),
                            $"({q},{r}) merkez+jitter yanlış hücreye yuvarlandı: ({rq},{rr})");
                    }
                }
            }
        }

        [Test]
        public void EdgeMid_IsSharedBetweenNeighbors()
        {
            const float s = 1f;
            for (int d = 0; d < 6; d++)
            {
                var nb = HexCoord.Neighbor((0, 0), d);
                var (ax, ay) = HexMetrics.EdgeMid(0, 0, d, s);
                var (bx, by) = HexMetrics.EdgeMid(nb.q, nb.r, HexCoord.Opp(d), s);
                Assert.AreEqual(ax, bx, 1e-4f);
                Assert.AreEqual(ay, by, 1e-4f);
            }
        }
    }

    public class Mulberry32Tests
    {
        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new Mulberry32(42);
            var b = new Mulberry32(42);
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.NextDouble(), b.NextDouble());
        }

        [Test]
        public void DifferentSeeds_DifferentSequences()
        {
            var a = new Mulberry32(1);
            var b = new Mulberry32(2);
            bool anyDiff = false;
            for (int i = 0; i < 10; i++) if (Math.Abs(a.NextDouble() - b.NextDouble()) > 1e-12) anyDiff = true;
            Assert.IsTrue(anyDiff);
        }

        [Test]
        public void NextDouble_StaysInUnitInterval()
        {
            var rng = new Mulberry32(7);
            for (int i = 0; i < 10000; i++)
            {
                double v = rng.NextDouble();
                Assert.GreaterOrEqual(v, 0.0);
                Assert.Less(v, 1.0);
            }
        }

        [Test]
        public void RangeInclusive_CoversBothEnds()
        {
            var rng = new Mulberry32(99);
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < 2000; i++)
            {
                int v = rng.RangeInclusive(0, 5);
                Assert.That(v, Is.InRange(0, 5));
                seen.Add(v);
            }
            Assert.AreEqual(6, seen.Count, "0..5 aralığının tamamı üretilmeli");
        }

        [Test]
        public void Shuffle_IsPermutation()
        {
            var rng = new Mulberry32(5);
            var list = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            rng.Shuffle(list);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, list);
        }
    }
}

using ArrowRotate.Core;
using ArrowRotate.Logic;
using NUnit.Framework;

namespace ArrowRotate.Tests
{
    /// <summary>
    /// Katman mekaniği testleri: yüzey/gömülü ayrımı, terfi (PromoteAt), kısmen gömülü okun
    /// bağlanamaması, sahte-tail koruması ve katmanlı çözülebilirlik simülasyonu.
    /// </summary>
    public class LayerMechanicTests
    {
        // d=0 → (+1,0). Basit 2 hücreli ok: tail(B=0) → head(A=3, B=uçuş 0).
        private static void AddStraightArrow(HexaLevel level, int arrowId, int q, int r, int layer)
        {
            var arrow = new Arrow { ArrowId = arrowId, Palette = arrowId, ExitDir = 0 };
            level.Arrows.Add(arrow);
            level.AddCell(new Cell { Q = q, R = r, ArrowId = arrowId, Type = CellType.Tail, A = -1, B = 0, Layer = layer });
            level.AddCell(new Cell { Q = q + 1, R = r, ArrowId = arrowId, Type = CellType.Head, A = 3, B = 0, Layer = layer });
            arrow.Cells.Add((q, r));
            arrow.Cells.Add((q + 1, r));
        }

        [Test]
        public void AddCell_RoutesByLayer()
        {
            var level = new HexaLevel();
            AddStraightArrow(level, 0, 0, 0, layer: 0);
            AddStraightArrow(level, 1, 0, 0, layer: 1); // aynı pozisyonlar, bir alt katman

            Assert.AreEqual(0, level.GetCell(0, 0).ArrowId, "yüzeyde ok 0 olmalı");
            Assert.IsTrue(level.Buried.ContainsKey((0, 0)), "ok 1'in tail'i gömülü olmalı");
            Assert.AreEqual(1, level.Buried[(0, 0)][0].ArrowId);
        }

        [Test]
        public void PromoteAt_RaisesTopAndShiftsRest()
        {
            var level = new HexaLevel();
            AddStraightArrow(level, 0, 0, 0, layer: 0);
            AddStraightArrow(level, 1, 0, 0, layer: 1);
            AddStraightArrow(level, 2, 0, 0, layer: 2);

            // yüzey doluyken terfi olmaz
            Assert.IsNull(level.PromoteAt((0, 0)));

            level.Cells.Remove((0, 0));
            var promoted = level.PromoteAt((0, 0));

            Assert.IsNotNull(promoted);
            Assert.AreEqual(1, promoted.ArrowId, "katman 1'deki terfi etmeli");
            Assert.AreEqual(0, promoted.Layer);
            Assert.AreSame(promoted, level.GetCell(0, 0));
            Assert.AreEqual(1, level.Buried[(0, 0)].Count, "katman 2'deki tek başına kalmalı");
            Assert.AreEqual(1, level.Buried[(0, 0)][0].Layer, "katman 2 → 1'e kaymalı");

            // ikinci terfi: yığın boşalır
            level.Cells.Remove((0, 0));
            var second = level.PromoteAt((0, 0));
            Assert.AreEqual(2, second.ArrowId);
            Assert.IsFalse(level.Buried.ContainsKey((0, 0)), "yığın tükenince anahtar silinmeli");
        }

        [Test]
        public void PartiallyBuriedArrow_CannotConnect()
        {
            var level = new HexaLevel();
            AddStraightArrow(level, 0, 0, 0, layer: 0);   // yüzey oku (0,0)-(1,0)
            // ok 1: tail (0,0) GÖMÜLÜ, head (2,0)'da... araya bitişiklik için (1,0) gömülü mid koy
            var a1 = new Arrow { ArrowId = 1, Palette = 1, ExitDir = 0 };
            level.Arrows.Add(a1);
            level.AddCell(new Cell { Q = 0, R = 0, ArrowId = 1, Type = CellType.Tail, A = -1, B = 0, Layer = 1 });
            level.AddCell(new Cell { Q = 1, R = 0, ArrowId = 1, Type = CellType.Mid, A = 3, B = 0, Layer = 1 });
            level.AddCell(new Cell { Q = 2, R = 0, ArrowId = 1, Type = CellType.Head, A = 3, B = 0, Layer = 0 }); // yüzeyde
            a1.Cells.Add((0, 0)); a1.Cells.Add((1, 0)); a1.Cells.Add((2, 0));

            Assert.IsFalse(level.IsFullySurfaced(a1));
            // rot=0 (çözük) olsa bile bağlanamaz: tail/mid gömülü — VE yüzeydeki (0,0) başka okun
            // (sahte-tail koruması: tracer başkasının hücresinden yürümeye başlamamalı)
            Assert.IsFalse(ConnectionTracer.Trace(level, 1).Connected);

            // yüzey oku ise normal bağlanır (rot=0)
            Assert.IsTrue(ConnectionTracer.Trace(level, 0).Connected);
        }

        [Test]
        public void FullySurfacedAfterPromotion_Connects()
        {
            var level = new HexaLevel();
            AddStraightArrow(level, 0, 0, 0, layer: 0);
            AddStraightArrow(level, 1, 0, 0, layer: 1);

            Assert.IsFalse(ConnectionTracer.Trace(level, 1).Connected, "gömülüyken bağlanamaz");

            // ok 0 çıkmış gibi: yüzey hücrelerini sil + terfi et
            foreach (var pos in level.Arrows[0].Cells)
            {
                level.Cells.Remove(pos);
                level.PromoteAt(pos);
            }

            Assert.IsTrue(level.IsFullySurfaced(level.Arrows[1]));
            Assert.IsTrue(ConnectionTracer.Trace(level, 1).Connected, "tam yüzeye çıkınca bağlanmalı");
        }

        [Test]
        public void CanExitAllLayered_SolvableStack()
        {
            var level = new HexaLevel();
            AddStraightArrow(level, 0, 0, 0, layer: 0);
            AddStraightArrow(level, 1, 0, 0, layer: 1);
            AddStraightArrow(level, 2, 0, 0, layer: 2);
            Assert.IsTrue(ExitSimulator.CanExitAllLayered(level), "üst üste 3 ok sırayla çıkabilmeli");
        }

        [Test]
        public void CanExitAllLayered_DetectsBuriedDeadlock()
        {
            var level = new HexaLevel();
            AddStraightArrow(level, 0, 0, 0, layer: 0);
            AddStraightArrow(level, 1, 0, 0, layer: 1);
            // ok 1'in buz eşiği 2: çıkabilmesi için 2 ok çıkmış olmalı ama toplam 2 ok var
            // → ok 0 çıkınca exitedCount=1 < 2, ok 1 asla çıkamaz → deadlock
            level.Arrows[1].FreezeAt = 2;
            Assert.IsFalse(ExitSimulator.CanExitAllLayered(level), "eşik dolamayacağı için deadlock");
        }

        [Test]
        public void CanExitAllLayered_PromotedCellBlocksRay()
        {
            var level = new HexaLevel();
            // ok 0: (0,0)→(1,0), uçuş d=0 (+q yönü) — ışını (2,0),(3,0)... üzerinden geçer
            AddStraightArrow(level, 0, 0, 0, layer: 0);
            // ok 1: (2,0)→(3,0) GÖMÜLÜ (katman 1) — üstünde ok 2 var
            AddStraightArrow(level, 1, 2, 0, layer: 1);
            // ok 2: (2,0)→(3,0) yüzeyde, ok 0'ın ışığını zaten kesiyor
            AddStraightArrow(level, 2, 2, 0, layer: 0);

            // sıra: ok 2 çıkar → ok 1 terfi eder ve ışını yine keser → ok 1 çıkar → ok 0 çıkar
            // dinamik simülasyon bunu çözebilmeli (statik grafik terfi engelini göremezdi)
            Assert.IsTrue(ExitSimulator.CanExitAllLayered(level));
        }
    }
}

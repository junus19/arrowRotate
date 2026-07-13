using ArrowRotate.Core;
using ArrowRotate.Logic;
using NUnit.Framework;

namespace ArrowRotate.Tests
{
    /// <summary>El yapımı mini levellarla bağlantı/ışın kuralları (SKILL.md §4).</summary>
    public class ConnectionLogicTests
    {
        private static HexaLevel NewLevel(int arrowCount)
        {
            var level = new HexaLevel();
            for (int i = 0; i < arrowCount; i++) level.Arrows.Add(new Arrow { ArrowId = i });
            return level;
        }

        private static Cell AddCell(HexaLevel level, int arrowId, int q, int r, CellType type, int a, int b, int rot = 0)
        {
            var cell = new Cell { Q = q, R = r, ArrowId = arrowId, Type = type, A = a, B = b, Rot = rot };
            level.Cells[(q, r)] = cell;
            level.Arrows[arrowId].Cells.Add((q, r));
            return cell;
        }

        // tail(0,0)-B0 → head(1,0)-A3: dir 0 komşuluğu, çözülmüş iki hücreli ok
        private static HexaLevel TwoCellArrow(out Cell tail, out Cell head)
        {
            var level = NewLevel(1);
            tail = AddCell(level, 0, 0, 0, CellType.Tail, -1, 0);
            head = AddCell(level, 0, 1, 0, CellType.Head, 3, 0);
            level.Arrows[0].ExitDir = 0;
            return level;
        }

        [Test]
        public void Trace_SolvedTwoCellArrow_Connected()
        {
            var level = TwoCellArrow(out _, out var head);
            var res = ConnectionTracer.Trace(level, 0);
            Assert.IsTrue(res.Connected);
            Assert.AreEqual(0, res.ExitDir);
            Assert.AreSame(head, res.HeadCell);
        }

        [Test]
        public void Trace_RotatedHead_NotConnected()
        {
            var level = TwoCellArrow(out _, out var head);
            head.Rot = 1; // worldA=4 ≠ opp(0)=3
            Assert.IsFalse(ConnectionTracer.Trace(level, 0).Connected);
        }

        [Test]
        public void Trace_ThreeCellWithMid_ConnectedAndExitDirIsWorld()
        {
            var level = NewLevel(1);
            AddCell(level, 0, 0, 0, CellType.Tail, -1, 0);
            AddCell(level, 0, 1, 0, CellType.Mid, 3, 1);
            AddCell(level, 0, 1, 1, CellType.Head, 4, 0, rot: 1); // worldA=(4+1)%6=5? hayır: bağlanmamalı
            var res = ConnectionTracer.Trace(level, 0);
            Assert.IsFalse(res.Connected, "head rot=1 iken giriş uymamalı");

            level.GetCell(1, 1).Rot = 0;
            res = ConnectionTracer.Trace(level, 0);
            Assert.IsTrue(res.Connected);
            Assert.AreEqual(0, res.ExitDir);
        }

        [Test]
        public void Trace_StraightMid_BothRotationsConnect()
        {
            // Düz mid (B−A ≡ 3): rot=0 ve rot=3 aynı geometriyi verir — ikisi de bağlanmalı (spec notu).
            var level = NewLevel(1);
            AddCell(level, 0, 0, 0, CellType.Tail, -1, 0);
            var mid = AddCell(level, 0, 1, 0, CellType.Mid, 3, 0);
            AddCell(level, 0, 2, 0, CellType.Head, 3, 0);

            Assert.IsTrue(ConnectionTracer.Trace(level, 0).Connected, "rot=0 bağlanmalı");
            mid.Rot = 3;
            Assert.IsTrue(ConnectionTracer.Trace(level, 0).Connected, "rot=3 (düz mid) de bağlanmalı");
            mid.Rot = 1;
            Assert.IsFalse(ConnectionTracer.Trace(level, 0).Connected);
        }

        [Test]
        public void Trace_ForeignCellInChain_NotConnected()
        {
            var level = NewLevel(2);
            AddCell(level, 0, 0, 0, CellType.Tail, -1, 0);
            AddCell(level, 1, 1, 0, CellType.Head, 3, 0); // araya başka okun hücresi
            AddCell(level, 0, 2, 0, CellType.Head, 3, 0); // arrow0: tail(0,0) + head(2,0)
            Assert.IsFalse(ConnectionTracer.Trace(level, 0).Connected);
        }

        [Test]
        public void Ray_DetectsForeignBlocker_AtCorrectDistance()
        {
            var level = TwoCellArrow(out _, out var head);
            level.Arrows.Add(new Arrow { ArrowId = 1 });
            var blockerCell = new Cell { Q = 3, R = 0, ArrowId = 1, Type = CellType.Mid, A = 0, B = 3 };
            level.Cells[(3, 0)] = blockerCell;
            level.Arrows[1].Cells.Add((3, 0));

            var blockers = RayScanner.Blockers(level, 0, head, 0);
            Assert.AreEqual(1, blockers.Count);
            Assert.AreEqual(1, blockers[0].ArrowId);
            Assert.AreEqual(2, blockers[0].Dist); // head(1,0) → (2,0)=1 → (3,0)=2
        }

        [Test]
        public void Ray_IgnoresOwnCells_AndExitedArrows()
        {
            var level = TwoCellArrow(out var tail, out var head);
            // Kendi kuyruğu ışının üzerinde değil ama kendi hücresi olsa da sayılmaz:
            var ownExtra = new Cell { Q = 2, R = 0, ArrowId = 0, Type = CellType.Mid, A = 0, B = 3 };
            level.Cells[(2, 0)] = ownExtra;
            Assert.IsTrue(RayScanner.IsClear(level, 0, head, 0), "kendi hücresi engel sayılmamalı");

            level.Arrows.Add(new Arrow { ArrowId = 1, Exited = true });
            level.Cells[(4, 0)] = new Cell { Q = 4, R = 0, ArrowId = 1, Type = CellType.Mid, A = 0, B = 3 };
            Assert.IsTrue(RayScanner.IsClear(level, 0, head, 0), "exited ok engel sayılmamalı");
        }

        [Test]
        public void ExitSimulator_DetectsDeadlockAndSuccess()
        {
            // A→B→C zinciri (C kimseyi beklemiyor): çözülebilir.
            var chain = new System.Collections.Generic.List<System.Collections.Generic.HashSet<int>>
            {
                new System.Collections.Generic.HashSet<int> { 1 },
                new System.Collections.Generic.HashSet<int> { 2 },
                new System.Collections.Generic.HashSet<int>()
            };
            Assert.IsTrue(ExitSimulator.CanExitAll(chain, new[] { 0, 0, 0 }));

            // Buz eşiği kilitlenmesi: ilk çıkması gereken ok (2) eşik 3 ile donarsa deadlock.
            Assert.IsFalse(ExitSimulator.CanExitAll(chain, new[] { 0, 0, 3 }));
            // Doğru sıra eşiği: 2 çıkar (eşiksiz), 1'in eşiği 1 dolu, 0'ın eşiği 2 dolu.
            Assert.IsTrue(ExitSimulator.CanExitAll(chain, new[] { 2, 1, 0 }));

            // Karşılıklı kilit (döngü) her zaman deadlock.
            var cycle = new System.Collections.Generic.List<System.Collections.Generic.HashSet<int>>
            {
                new System.Collections.Generic.HashSet<int> { 1 },
                new System.Collections.Generic.HashSet<int> { 0 }
            };
            Assert.IsFalse(ExitSimulator.CanExitAll(cycle, new[] { 0, 0 }));
        }
    }
}

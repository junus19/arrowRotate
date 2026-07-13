using System;
using System.Collections.Generic;
using System.Linq;
using ArrowRotate.Core;
using ArrowRotate.Logic;

namespace ArrowRotate.Generation
{
    /// <summary>
    /// Deterministik level üretimi — prototip genLevel/assignIce'ın birebir portu (SKILL.md §7).
    /// Adımlar: self-avoiding walk yerleştirme → segment kurulumu → palet çizge boyaması →
    /// engelleme grafiği + Kahn DAG şartı → (en iyi attempt) → bitişiklik öz-denetimi →
    /// karıştırma (hiçbir ok baştan bağlı olamaz) → buz ataması (tam simülasyonla doğrulanır).
    /// </summary>
    public static class LevelGenerator
    {
        private const int MaxAttempts = 400;
        private const int MaxPlaceTries = 80;
        private const int MaxRegens = 32;

        public static HexaLevel Generate(int seed, LevelConfig cfg)
        {
            int curSeed = seed;
            for (int regen = 0; regen < MaxRegens; regen++)
            {
                var rng = new Mulberry32(curSeed);
                var best = TryGenerate(rng, cfg);
                if (best == null)
                {
                    curSeed += 7919;   // prototip: genLevel(seed+7919)
                    continue;
                }
                if (!IsContiguous(best))
                {
                    curSeed += 31337;  // prototip: bitişiklik hatası → genLevel(seed+31337)
                    continue;
                }

                Scramble(rng, best);
                if (cfg.IceCount > 0) AssignIce(rng, best, cfg.IceCount);

                best.Seed = curSeed;
                return best;
            }
            throw new InvalidOperationException($"Level üretimi başarısız: seed={seed}, R={cfg.Radius}, oklar={cfg.ArrowCount}");
        }

        // ── attempt döngüsü: en iyi (en çok engelleme kenarlı) geçerli konfigürasyonu seç ──
        private static HexaLevel TryGenerate(Mulberry32 rng, LevelConfig cfg)
        {
            int numArrows = cfg.ArrowCount;
            HexaLevel best = null;

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var paths = PlacePaths(rng, cfg);
                if (paths == null) continue;

                var level = BuildCells(rng, paths, numArrows);

                if (!AssignPalettes(rng, level, cfg.PaletteCount)) continue;

                BuildBlockingGraph(level, cfg.Radius);
                if (!IsDag(level)) continue; // döngü = çözülemez, pazarlıksız atılır

                if (best == null || level.EdgeCount > best.EdgeCount) best = level;
                if (best.EdgeCount >= cfg.MinEdges && attempt > 25) break;
            }
            return best;
        }

        // ── 1. yol yerleştirme: self-avoiding random walk, kompakt küme ──
        private static List<List<(int q, int r)>> PlacePaths(Mulberry32 rng, LevelConfig cfg)
        {
            int radius = cfg.Radius;
            var used = new HashSet<(int, int)>();
            var usedOrder = new List<(int q, int r)>(); // JS Map insertion-order eşleniği
            var paths = new List<List<(int q, int r)>>();

            var lens = cfg.Lengths.ToList();
            rng.Shuffle(lens);

            for (int c = 0; c < cfg.ArrowCount; c++)
            {
                int targetLen = lens[c];
                bool placed = false;

                for (int tryN = 0; tryN < MaxPlaceTries && !placed; tryN++)
                {
                    // başlangıç: ilk yol merkeze yakın; sonrakiler kümeye komşu boş hücre
                    (int q, int r) start;
                    if (usedOrder.Count == 0)
                    {
                        start = (rng.RangeInclusive(-1, 1), rng.RangeInclusive(-1, 1));
                    }
                    else
                    {
                        var cand = new List<(int q, int r)>(); // tekrarlar bilinçli (JS ile aynı dağılım)
                        foreach (var u in usedOrder)
                        {
                            for (int d = 0; d < 6; d++)
                            {
                                var nb = HexCoord.Neighbor(u, d);
                                if (HexCoord.InRegion(nb.q, nb.r, radius) && !used.Contains(nb))
                                    cand.Add(nb);
                            }
                        }
                        if (cand.Count == 0) break;
                        start = rng.Pick(cand);
                    }

                    var path = new List<(int q, int r)> { start };
                    var vis = new HashSet<(int, int)> { start };
                    bool ok = true;

                    while (path.Count < targetLen)
                    {
                        var last = path[path.Count - 1];
                        var dirOrder = new List<int> { 0, 1, 2, 3, 4, 5 };
                        rng.Shuffle(dirOrder);
                        var opts = new List<(int q, int r)>();
                        foreach (int d in dirOrder)
                        {
                            var nb = HexCoord.Neighbor(last, d);
                            if (HexCoord.InRegion(nb.q, nb.r, radius) && !used.Contains(nb) && !vis.Contains(nb))
                                opts.Add(nb);
                        }
                        if (opts.Count == 0) { ok = false; break; }
                        var nx = opts[0];
                        path.Add(nx);
                        vis.Add(nx);
                    }

                    if (!ok) continue;
                    paths.Add(path);
                    foreach (var p in path) { used.Add(p); usedOrder.Add(p); }
                    placed = true;
                }

                if (!placed) return null;
            }
            return paths;
        }

        // ── 2. segment kurulumu: çözüm hali rot=0 ⇒ lokal = dünya yönü ──
        private static HexaLevel BuildCells(Mulberry32 rng, List<List<(int q, int r)>> paths, int numArrows)
        {
            var level = new HexaLevel();

            for (int c = 0; c < numArrows; c++)
            {
                var p = paths[c];
                var arrow = new Arrow { ArrowId = c };

                for (int i = 0; i < p.Count; i++)
                {
                    var (q, r) = p[i];
                    Cell cell;
                    if (i == 0)
                    {
                        cell = new Cell { Q = q, R = r, ArrowId = c, Type = CellType.Tail, A = -1, B = HexCoord.DirBetween(p[0], p[1]), Rot = 0 };
                    }
                    else if (i == p.Count - 1)
                    {
                        int a = HexCoord.DirBetween(p[i], p[i - 1]);
                        // uçuş yönü: %55 düz devam (girişin karşısı), yoksa girişten farklı rastgele
                        int b = rng.NextDouble() < 0.55 ? HexCoord.Opp(a) : -1;
                        if (b < 0)
                        {
                            var os = new List<int>();
                            for (int d = 0; d < 6; d++) if (d != a) os.Add(d);
                            rng.Shuffle(os);
                            b = os[0];
                        }
                        cell = new Cell { Q = q, R = r, ArrowId = c, Type = CellType.Head, A = a, B = b, Rot = 0 };
                        arrow.ExitDir = b;
                    }
                    else
                    {
                        cell = new Cell
                        {
                            Q = q, R = r, ArrowId = c, Type = CellType.Mid,
                            A = HexCoord.DirBetween(p[i], p[i - 1]),
                            B = HexCoord.DirBetween(p[i], p[i + 1]),
                            Rot = 0
                        };
                    }
                    level.Cells[(q, r)] = cell;
                    arrow.Cells.Add((q, r));
                }
                level.Arrows.Add(arrow);
            }
            return level;
        }

        // ── 3. palet ataması = çizge boyama: aynı paletteki iki ok HİÇBİR hücrede değemez ──
        private static bool AssignPalettes(Mulberry32 rng, HexaLevel level, int paletteCount)
        {
            int n = level.Arrows.Count;
            var adj = new HashSet<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new HashSet<int>();

            foreach (var cell in AllCellsInOrder(level))
            {
                for (int d = 0; d < 6; d++)
                {
                    var nb = level.GetCell(HexCoord.Neighbor(cell.Pos, d));
                    if (nb != null && nb.ArrowId != cell.ArrowId)
                    {
                        adj[cell.ArrowId].Add(nb.ArrowId);
                        adj[nb.ArrowId].Add(cell.ArrowId);
                    }
                }
            }

            var order = Enumerable.Range(0, n).ToList();
            rng.Shuffle(order);
            var pal = Enumerable.Repeat(-1, n).ToArray();
            var useCnt = new int[paletteCount];

            foreach (int i in order)
            {
                var banned = new HashSet<int>();
                foreach (int j in adj[i]) if (pal[j] >= 0) banned.Add(pal[j]);

                var opts = Enumerable.Range(0, paletteCount).ToList();
                rng.Shuffle(opts);
                var pick = opts.Where(p => !banned.Contains(p))
                               .OrderBy(p => useCnt[p]) // stabil sıralama: eşitlikte shuffle sırası korunur
                               .ToList();
                if (pick.Count == 0) return false;
                pal[i] = pick[0];
                useCnt[pick[0]]++;
            }

            for (int i = 0; i < n; i++) level.Arrows[i].Palette = pal[i];
            return true;
        }

        // ── 4. engelleme grafiği (çözüm halinde) ──
        private static void BuildBlockingGraph(HexaLevel level, int radius)
        {
            int n = level.Arrows.Count;
            level.BlockedBy = new List<HashSet<int>>(n);
            for (int i = 0; i < n; i++) level.BlockedBy.Add(new HashSet<int>());

            foreach (var arrow in level.Arrows)
            {
                var head = level.GetCell(arrow.HeadPos);
                var (dq, dr) = HexCoord.Dirs[arrow.ExitDir];
                int q = head.Q, r = head.R;
                for (int k = 0; k < 2 * radius + 4; k++)
                {
                    q += dq; r += dr;
                    var cc = level.GetCell(q, r);
                    if (cc != null && cc.ArrowId != arrow.ArrowId)
                        level.BlockedBy[arrow.ArrowId].Add(cc.ArrowId);
                }
            }
            level.EdgeCount = level.BlockedBy.Sum(s => s.Count);
        }

        // ── 5. DAG şartı (Kahn): döngü varsa çözülemez ──
        private static bool IsDag(HexaLevel level)
        {
            int n = level.Arrows.Count;
            var indeg = new int[n];
            var rev = new List<int>[n];
            for (int i = 0; i < n; i++) { indeg[i] = level.BlockedBy[i].Count; rev[i] = new List<int>(); }
            for (int i = 0; i < n; i++) foreach (int b in level.BlockedBy[i]) rev[b].Add(i);

            var queue = new Queue<int>();
            for (int i = 0; i < n; i++) if (indeg[i] == 0) queue.Enqueue(i);
            int seen = 0;
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                seen++;
                foreach (int m in rev[cur]) if (--indeg[m] == 0) queue.Enqueue(m);
            }
            return seen == n;
        }

        // ── 6. öz-denetim: her okun ardışık hücreleri axial komşu mu? ──
        private static bool IsContiguous(HexaLevel level)
        {
            foreach (var arrow in level.Arrows)
            {
                for (int i = 0; i + 1 < arrow.Cells.Count; i++)
                {
                    if (HexCoord.DirBetween(arrow.Cells[i], arrow.Cells[i + 1]) < 0) return false;
                }
            }
            return true;
        }

        // ── 7. karıştırma: rastgele rot; HİÇBİR ok baştan bağlı olamaz ──
        private static void Scramble(Mulberry32 rng, HexaLevel level)
        {
            foreach (var cell in AllCellsInOrder(level))
                cell.Rot = rng.RangeInclusive(0, 5);

            foreach (var arrow in level.Arrows)
            {
                int guard = 0;
                while (ConnectionTracer.Trace(level, arrow.ArrowId).Connected && guard++ < 20)
                {
                    var head = level.GetCell(arrow.HeadPos);
                    head.Rot = (head.Rot + rng.RangeInclusive(1, 5)) % 6; // head'in geçerli rot'u tekildir
                }
            }
        }

        // ── 8. buz ataması: tam simülasyonla doğrulanır; olmazsa garantili fallback ──
        private static void AssignIce(Mulberry32 rng, HexaLevel level, int count)
        {
            int n = level.Arrows.Count;

            for (int t = 0; t < 80; t++)
            {
                var order = Enumerable.Range(0, n).ToList();
                rng.Shuffle(order);
                var chosen = order.Take(count).ToList();
                var freeze = new int[n];
                for (int i = 0; i < chosen.Count; i++) freeze[chosen[i]] = i + 1;

                if (ExitSimulator.CanExitAll(level.BlockedBy, freeze))
                {
                    ApplyFreeze(level, freeze);
                    return;
                }
            }

            // garantili fallback: topolojik sıranın SON `count` oku 1..count eşikleriyle donar
            var topo = TopologicalOrder(level);
            var fallback = new int[n];
            int idx = 1;
            foreach (int c in topo.Skip(Math.Max(0, topo.Count - count)))
                fallback[c] = idx++;
            ApplyFreeze(level, fallback);
        }

        private static void ApplyFreeze(HexaLevel level, int[] freeze)
        {
            foreach (var arrow in level.Arrows)
                arrow.FreezeAt = freeze[arrow.ArrowId];
        }

        private static List<int> TopologicalOrder(HexaLevel level)
        {
            int n = level.Arrows.Count;
            var indeg = new int[n];
            var rev = new List<int>[n];
            for (int i = 0; i < n; i++) { indeg[i] = level.BlockedBy[i].Count; rev[i] = new List<int>(); }
            for (int i = 0; i < n; i++) foreach (int b in level.BlockedBy[i]) rev[b].Add(i);

            var queue = new Queue<int>();
            for (int i = 0; i < n; i++) if (indeg[i] == 0) queue.Enqueue(i);
            var order = new List<int>();
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                order.Add(cur);
                foreach (int m in rev[cur]) if (--indeg[m] == 0) queue.Enqueue(m);
            }
            return order;
        }

        /// <summary>Deterministik hücre sırası: ok sırası × kuyruk→head (JS Map insertion-order eşleniği).</summary>
        private static IEnumerable<Cell> AllCellsInOrder(HexaLevel level)
        {
            foreach (var arrow in level.Arrows)
                foreach (var pos in arrow.Cells)
                    yield return level.GetCell(pos);
        }
    }
}

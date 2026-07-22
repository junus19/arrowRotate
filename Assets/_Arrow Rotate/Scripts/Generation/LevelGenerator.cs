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
            if (cfg.LayerCount > 1) return GenerateLayered(seed, cfg);

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

        // ════════════════════════════════════════════════════════════════════
        //  KATMANLI ÜRETİM (LayerCount > 1) — height-stacking ile kapsama garantili
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Katmanlı level üretimi. Kilit fikir: hücreler kolona (q,r) yığılır — bir okun hücresi
        /// yerleştirilirken katman = o kolonun mevcut yüksekliği (0 = yüzey). Böylece KAPSAMA
        /// (bir alt katman hücresinin üstü daima dolu) yapı gereği garantidir. "Flat" oklar yalnızca
        /// boş kolonlarda yürür (hep katman 0); "spanning" oklar dolu kolonlara basıp gömülü hücre
        /// üretir → parçaları farklı katmanlarda. Çözülebilirlik ExitSimulator.CanExitAllLayered ile
        /// DOĞRULANIR (statik DAG yetmez: terfi eden hücre yeni engel olabilir), tutmazsa seed atılır.
        /// </summary>
        private static HexaLevel GenerateLayered(int seed, LevelConfig cfg)
        {
            int curSeed = seed;
            for (int regen = 0; regen < MaxRegens * 2; regen++)
            {
                var rng = new Mulberry32(curSeed);
                var layers = new List<List<int>>();
                var paths = PlaceLayeredPaths(rng, cfg, layers);
                if (paths == null) { curSeed += 7919; continue; }

                var level = BuildCellsLayered(rng, paths, layers);
                if (!IsContiguous(level)) { curSeed += 31337; continue; }
                if (!AssignPalettesLayered(rng, level, cfg.PaletteCount)) { curSeed += 104729; continue; }

                ScrambleLayered(rng, level);
                if (cfg.IceCount > 0) AssignIceLayered(rng, level, cfg.IceCount);

                // pazarlıksız: katmanlı dinamik çözülebilirlik
                if (!ExitSimulator.CanExitAllLayered(level)) { curSeed += 15485863; continue; }

                level.Seed = curSeed;
                return level;
            }
            throw new InvalidOperationException(
                $"Katmanlı üretim başarısız: seed={seed}, R={cfg.Radius}, oklar={cfg.ArrowCount}, katman={cfg.LayerCount}");
        }

        /// <summary>Kolon-yığma yol yerleştirme. layersOut: her yola paralel, hücre başına katman.</summary>
        private static List<List<(int q, int r)>> PlaceLayeredPaths(
            Mulberry32 rng, LevelConfig cfg, List<List<int>> layersOut)
        {
            int radius = cfg.Radius, L = cfg.LayerCount, N = cfg.ArrowCount, S = cfg.SpanningArrows;
            var height = new Dictionary<(int, int), int>();
            var paths = new List<List<(int q, int r)>>();
            var usedOrder = new List<(int q, int r)>(); // kompakt küme için (herhangi katmanda dolu kolonlar)

            var lens = cfg.Lengths.ToList();
            rng.Shuffle(lens);

            for (int c = 0; c < N; c++)
            {
                bool spanning = c >= N - S && L >= 2; // son S ok yayılır (flatlerden sonra → dolu kolona basar)
                int targetLen = lens[c];
                // yayılan ok başına GÖMÜLÜ parça hedefi (layer≥1): [min,max]'tan rastgele, [1, len-1]'e kırpılır
                int buriedTarget = spanning
                    ? System.Math.Max(1, System.Math.Min(rng.RangeInclusive(cfg.BuriedMin, cfg.BuriedMax), targetLen - 1))
                    : 0;
                List<(int q, int r)> path = null;
                List<int> lay = null;

                for (int tryN = 0; tryN < MaxPlaceTries; tryN++)
                {
                    int buried;
                    var p = WalkLayered(rng, radius, L, height, usedOrder, targetLen, spanning, buriedTarget, out var l, out buried);
                    if (p == null) continue;
                    // gömülü sayısı hedefe yeterince yakınsa kabul; değilse dene (son turlarda gevşe)
                    if (spanning && buried < 1 && tryN < MaxPlaceTries - 12) continue;
                    if (spanning && buried != buriedTarget && tryN < MaxPlaceTries - 25) continue;
                    path = p; lay = l; break;
                }
                if (path == null) return null;

                for (int i = 0; i < path.Count; i++)
                {
                    if (!height.TryGetValue(path[i], out int h) || h == 0)
                        if (!height.ContainsKey(path[i])) usedOrder.Add(path[i]); // ilk kez dolan kolon
                    height[path[i]] = lay[i] + 1;
                }
                paths.Add(path);
                layersOut.Add(lay);
            }
            return paths;
        }

        /// <summary>Tek okun kolon-yığmalı self-avoiding yürüyüşü. Flat: yalnız boş kolon (hep katman 0).
        /// Spanning: buriedTarget kadar hücreyi DOLU kolona (gömülü, layer≥1), kalanını boş kolona (yüzey)
        /// yerleştirmeyi hedefler; henüz hedefe ulaşmadıysa dolu komşuya, ulaştıysa boş komşuya yönelir.
        /// buriedOut = gerçekleşen gömülü (layer≥1) hücre sayısı.</summary>
        private static List<(int q, int r)> WalkLayered(
            Mulberry32 rng, int radius, int L, Dictionary<(int, int), int> height,
            List<(int q, int r)> usedOrder, int targetLen, bool spanning, int buriedTarget,
            out List<int> layersOut, out int buriedOut)
        {
            layersOut = null; buriedOut = 0;

            // başlangıç kolonu: boş (yüzey hücresi garanti) + kompakt (kümeye komşu)
            (int q, int r) start;
            if (usedOrder.Count == 0)
            {
                start = (rng.RangeInclusive(-1, 1), rng.RangeInclusive(-1, 1));
            }
            else
            {
                var cand = new List<(int q, int r)>();
                foreach (var u in usedOrder)
                    for (int d = 0; d < 6; d++)
                    {
                        var nb = HexCoord.Neighbor(u, d);
                        if (HexCoord.InRegion(nb.q, nb.r, radius) && HeightOf(height, nb) == 0)
                            cand.Add(nb);
                    }
                if (cand.Count == 0) return null;
                start = rng.Pick(cand);
            }
            if (HeightOf(height, start) != 0) return null;

            var path = new List<(int q, int r)> { start };
            var vis = new HashSet<(int, int)> { start };

            while (path.Count < targetLen)
            {
                var last = path[path.Count - 1];
                var dirOrder = new List<int> { 0, 1, 2, 3, 4, 5 };
                rng.Shuffle(dirOrder);

                var fresh = new List<(int q, int r)>();
                var occupied = new List<(int q, int r)>();
                foreach (int d in dirOrder)
                {
                    var nb = HexCoord.Neighbor(last, d);
                    if (!HexCoord.InRegion(nb.q, nb.r, radius) || vis.Contains(nb)) continue;
                    int h = HeightOf(height, nb);
                    if (h >= L) continue;                 // katman sınırı dolu
                    if (h == 0) fresh.Add(nb);
                    else occupied.Add(nb);
                }

                (int q, int r) nx;
                if (spanning)
                {
                    // şu ana kadarki gömülü (dolu kolona basan) hücre sayısı
                    int buriedSoFar = 0;
                    for (int i = 0; i < path.Count; i++) if (HeightOf(height, path[i]) > 0) buriedSoFar++;
                    int remaining = targetLen - path.Count;                 // eklenecek hücre sayısı
                    int needBuried = buriedTarget - buriedSoFar;            // daha kaç gömülü lazım

                    // hedefe göre yönlendir: gömülü açığı büyükse dolu komşuyu, değilse boş komşuyu tercih et.
                    // (needBuried >= remaining → kalan her adım gömülü olmalı → dolu zorunlu)
                    List<(int q, int r)> pref;
                    if (needBuried > 0 && (needBuried >= remaining || occupied.Count > 0) && occupied.Count > 0)
                        pref = Concat(occupied, fresh);   // gömülü lazım → dolu öncelikli
                    else if (fresh.Count > 0)
                        pref = Concat(fresh, occupied);   // yüzey lazım → boş öncelikli
                    else if (occupied.Count > 0)
                        pref = occupied;
                    else break;
                    nx = pref[0];
                }
                else
                {
                    if (fresh.Count == 0) break;           // flat: yalnız boş kolon
                    nx = fresh[0];
                }
                path.Add(nx);
                vis.Add(nx);
            }

            if (path.Count < targetLen) return null;

            layersOut = new List<int>(path.Count);
            for (int i = 0; i < path.Count; i++)
            {
                int h = HeightOf(height, path[i]);
                layersOut.Add(h);
                if (h >= 1) buriedOut++;
            }
            return path;
        }

        private static int HeightOf(Dictionary<(int, int), int> height, (int q, int r) p)
            => height.TryGetValue(p, out int h) ? h : 0;

        private static List<(int q, int r)> Concat(List<(int q, int r)> a, List<(int q, int r)> b)
        {
            var r = new List<(int q, int r)>(a.Count + b.Count);
            r.AddRange(a); r.AddRange(b);
            return r;
        }

        private static HexaLevel BuildCellsLayered(Mulberry32 rng, List<List<(int q, int r)>> paths, List<List<int>> layers)
        {
            var level = new HexaLevel();
            for (int c = 0; c < paths.Count; c++)
            {
                var p = paths[c];
                var lay = layers[c];
                var arrow = new Arrow { ArrowId = c };
                for (int i = 0; i < p.Count; i++)
                {
                    var (q, r) = p[i];
                    Cell cell;
                    if (i == 0)
                        cell = new Cell { Q = q, R = r, ArrowId = c, Type = CellType.Tail, A = -1, B = HexCoord.DirBetween(p[0], p[1]), Rot = 0, Layer = lay[i] };
                    else if (i == p.Count - 1)
                    {
                        int a = HexCoord.DirBetween(p[i], p[i - 1]);
                        int b = rng.NextDouble() < 0.55 ? HexCoord.Opp(a) : -1;
                        if (b < 0)
                        {
                            var os = new List<int>();
                            for (int d = 0; d < 6; d++) if (d != a) os.Add(d);
                            rng.Shuffle(os);
                            b = os[0];
                        }
                        cell = new Cell { Q = q, R = r, ArrowId = c, Type = CellType.Head, A = a, B = b, Rot = 0, Layer = lay[i] };
                        arrow.ExitDir = b;
                    }
                    else
                        cell = new Cell
                        {
                            Q = q, R = r, ArrowId = c, Type = CellType.Mid,
                            A = HexCoord.DirBetween(p[i], p[i - 1]),
                            B = HexCoord.DirBetween(p[i], p[i + 1]),
                            Rot = 0, Layer = lay[i]
                        };
                    level.AddCell(cell);
                    arrow.Cells.Add((q, r));
                }
                level.Arrows.Add(arrow);
            }
            return level;
        }

        /// <summary>Palet boyaması — KATMANLAR-ARASI komşuluk (gömülü ok yüzeye çıkınca yan yana gelebilir).</summary>
        private static bool AssignPalettesLayered(Mulberry32 rng, HexaLevel level, int paletteCount)
        {
            int n = level.Arrows.Count;
            var adj = new HashSet<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new HashSet<int>();

            foreach (var arrow in level.Arrows)
                foreach (var pos in arrow.Cells)
                    for (int d = 0; d < 6; d++)
                    {
                        var np = HexCoord.Neighbor(pos, d);
                        foreach (var nb in CellsAtAnyLayer(level, np))
                            if (nb.ArrowId != arrow.ArrowId)
                            {
                                adj[arrow.ArrowId].Add(nb.ArrowId);
                                adj[nb.ArrowId].Add(arrow.ArrowId);
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
                var pick = opts.Where(p => !banned.Contains(p)).OrderBy(p => useCnt[p]).ToList();
                if (pick.Count == 0) return false;
                pal[i] = pick[0];
                useCnt[pick[0]]++;
            }
            for (int i = 0; i < n; i++) level.Arrows[i].Palette = pal[i];
            return true;
        }

        private static IEnumerable<Cell> CellsAtAnyLayer(HexaLevel level, (int q, int r) pos)
        {
            if (level.Cells.TryGetValue(pos, out var s)) yield return s;
            if (level.Buried.TryGetValue(pos, out var list))
                for (int i = 0; i < list.Count; i++) yield return list[i];
        }

        /// <summary>Karıştırma — okun KENDİ hücresini GetArrowCell ile bulur (buried cell Cells'te değil).
        /// Gömülü ok zaten bağlanamaz; yalnız tam-yüzey oklar için "baştan bağlı olmasın" garantisi.</summary>
        private static void ScrambleLayered(Mulberry32 rng, HexaLevel level)
        {
            foreach (var arrow in level.Arrows)
                foreach (var pos in arrow.Cells)
                    level.GetArrowCell(arrow.ArrowId, pos).Rot = rng.RangeInclusive(0, 5);

            foreach (var arrow in level.Arrows)
            {
                int guard = 0;
                while (ConnectionTracer.Trace(level, arrow.ArrowId).Connected && guard++ < 20)
                {
                    var head = level.GetArrowCell(arrow.ArrowId, arrow.HeadPos);
                    head.Rot = (head.Rot + rng.RangeInclusive(1, 5)) % 6;
                }
            }
        }

        /// <summary>Buz ataması (katmanlı) — rastgele eşikler, CanExitAllLayered ile doğrula, tutmazsa buzsuz.</summary>
        private static void AssignIceLayered(Mulberry32 rng, HexaLevel level, int count)
        {
            int n = level.Arrows.Count;
            count = System.Math.Min(count, n);
            var backup = new int[n];
            for (int i = 0; i < n; i++) backup[i] = level.Arrows[i].FreezeAt;

            for (int t = 0; t < 80; t++)
            {
                var order = Enumerable.Range(0, n).ToList();
                rng.Shuffle(order);
                for (int i = 0; i < n; i++) level.Arrows[i].FreezeAt = 0;
                for (int i = 0; i < count; i++) level.Arrows[order[i]].FreezeAt = i + 1;

                if (ExitSimulator.CanExitAllLayered(level)) return;
            }
            for (int i = 0; i < n; i++) level.Arrows[i].FreezeAt = backup[i]; // tutmadı → buzsuz
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

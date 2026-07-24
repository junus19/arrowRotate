using System.Collections.Generic;
using System.Linq;
using ArrowRotate.Core;
using ArrowRotate.Generation;
using ArrowRotate.Integration;
using ArrowRotate.Logic;
using UnityEditor;
using UnityEngine;

namespace ArrowRotate.EditorTools
{
    // ════════════════════════════════════════════════════════════════════════
    //  SAĞ — araç paneli + doğrulama
    // ════════════════════════════════════════════════════════════════════════
    public partial class HexaLevelEditorWindow
    {
        private static readonly string[] ToolNames = { "Draw", "Erase", "Move", "Rotate", "Recolor", "Ice", "Edit" };

        private void DrawToolsPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            _toolsScroll = EditorGUILayout.BeginScrollView(_toolsScroll, GUILayout.ExpandHeight(true));

            GUILayout.Label("Tool", EditorStyles.boldLabel);
            int newTool = GUILayout.SelectionGrid((int)_tool, ToolNames, 4);
            if (newTool != (int)_tool)
            {
                _tool = (Tool)newTool;
                CancelDrag();
                Repaint();
                GUIUtility.ExitGUI();
            }
            GUILayout.Label(ToolHint(), EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(6);
            GUILayout.Label("Katman", EditorStyles.boldLabel);
            int newLayer = GUILayout.SelectionGrid(_activeLayer,
                new[] { "0 · Yüzey", "1 · Alt", "2 · Dip" }, 3);
            if (newLayer != _activeLayer)
            {
                _activeLayer = newLayer;
                CancelDrag();
                _editCell = (int.MinValue, 0);
                Repaint();
                GUIUtility.ExitGUI();
            }
            if (_selected != null)
            {
                int c0 = 0, c1 = 0, c2 = 0;
                foreach (var c in _selected.Cells)
                {
                    if (c.Layer == 0) c0++;
                    else if (c.Layer == 1) c1++;
                    else c2++;
                }
                GUILayout.Label($"Hücre: yüzey {c0} · alt {c1} · dip {c2} — araçlar seçili katmanda çalışır",
                    EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.Space(6);
            GUILayout.Label("Palet", EditorStyles.boldLabel);
            DrawPaletteSwatches();

            if (_tool == Tool.Edit)
            {
                EditorGUILayout.Space(6);
                GUILayout.Label("Hücre Denetçisi", EditorStyles.boldLabel);
                DrawCellInspector();
            }

            EditorGUILayout.Space(10);
            DrawLevelSettings();

            EditorGUILayout.Space(8);
            using (new EditorGUI.DisabledScope(_selected == null || _selected.Arrows.Length == 0))
            {
                if (GUILayout.Button("Paletleri Ata (çizge boyama)")) AutoAssignPalettes();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Scramble")) ScrambleSelected();
                if (GUILayout.Button("Çöz (rot=0)")) SolveSelected();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);
            _fillFoldout = EditorGUILayout.Foldout(_fillFoldout, "Random Fill", true);
            if (_fillFoldout) DrawRandomFill();

            EditorGUILayout.EndScrollView();

            // panelin altına sabitlenmiş doğrulama
            GUILayout.Label("Doğrulama", EditorStyles.boldLabel);
            DrawValidation();
            EditorGUILayout.EndVertical();
        }

        private string ToolHint() => _tool switch
        {
            Tool.Draw => "Boş hücrede sürükle: kuyruk → head (en az 2 hücre)",
            Tool.Erase => "Tıkla: hücrenin OKUNU tümüyle siler (sağ tık her araçta)",
            Tool.Move => "Oku sürükle — tüm hücreleriyle taşınır",
            Tool.Rotate => "Hücreye tıkla: rot +1 (oyundaki tap)",
            Tool.Recolor => "Oka tıkla: seçili paletle boyar",
            Tool.Ice => "Oka tıkla: buz eşiği 0→1→2→3→0",
            _ => "Hücre seç; seçili HEAD'e tekrar tıkla → uçuş yönü döner",
        };

        private void DrawPaletteSwatches()
        {
            var palettes = ArrowRotate.View.HexaPalette.Palettes; // Color Database'ten (dinamik uzunluk)
            const int perRow = 5;
            for (int i = 0; i < palettes.Length; i++)
            {
                if (i % perRow == 0) EditorGUILayout.BeginHorizontal();
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = palettes[i];
                string label = _paintPalette == i ? "✓" : " ";
                if (GUILayout.Button(label, GUILayout.Width(42), GUILayout.Height(24)))
                    _paintPalette = i;
                GUI.backgroundColor = prev;
                if (i % perRow == perRow - 1 || i == palettes.Length - 1) EditorGUILayout.EndHorizontal();
            }
            GUILayout.Label($"Palet {_paintPalette}", EditorStyles.centeredGreyMiniLabel);
        }

        // ── Hücre denetçisi (Edit aracı) ──────────────────────────────────────

        private void DrawCellInspector()
        {
            if (_selected == null || _editCell.q == int.MinValue)
            {
                EditorGUILayout.HelpBox("Canvas'ta bir hücre seç.", MessageType.None);
                return;
            }
            var cell = CellAt(_editCell.q, _editCell.r);
            if (cell == null)
            {
                EditorGUILayout.HelpBox($"({_editCell.q}, {_editCell.r}) — boş hücre.", MessageType.None);
                return;
            }

            GUILayout.Label($"Hücre ({cell.Q}, {cell.R}) · Ok {cell.ArrowId} · {cell.Type} · Katman {cell.Layer}", EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();
            int newA = cell.Type == CellType.Tail
                ? -1
                : EditorGUILayout.IntSlider("A (giriş)", cell.A, 0, 5);
            int newB = EditorGUILayout.IntSlider(cell.Type == CellType.Head ? "B (uçuş)" : "B (çıkış)", cell.B, 0, 5);
            int newRot = EditorGUILayout.IntSlider("Rot", cell.Rot, 0, 5);
            if (EditorGUI.EndChangeCheck())
            {
                Record("Edit Cell");
                cell.A = newA;
                cell.B = newB;
                cell.Rot = newRot;
                Dirty();
            }

            // hücreyi katmanlar arasında taşı (ok katmanlara yayılabilir); hedef katman doluysa reddedilir
            EditorGUI.BeginChangeCheck();
            int newCellLayer = EditorGUILayout.IntSlider("Katman", cell.Layer, 0, HexaLevel.MaxBuriedLayers);
            if (EditorGUI.EndChangeCheck() && newCellLayer != cell.Layer)
            {
                if (CellAt(cell.Q, cell.R, newCellLayer) == null)
                {
                    Record("Change Cell Layer");
                    cell.Layer = newCellLayer;
                    Dirty();
                }
                else Debug.LogWarning($"[LevelEditor] ({cell.Q},{cell.R}) katman {newCellLayer} dolu — taşınamadı.");
            }

            var arrow = _selected.Arrows[cell.ArrowId];
            EditorGUI.BeginChangeCheck();
            int newPalette = EditorGUILayout.IntSlider("Palet", arrow.Palette, 0, 9);
            int newFreeze = EditorGUILayout.IntSlider("Buz Eşiği", arrow.FreezeAt, 0, 3);
            GUILayout.Space(2);
            int newLock = EditorGUILayout.IntSlider("Kilit Grubu (-1=yok)", arrow.LockGroup, -1, 3); // >=0 → kilitli ok (grubun anahtar hexagonu açar)
            if (EditorGUI.EndChangeCheck())
            {
                Record("Edit Arrow");
                arrow.Palette = newPalette;
                arrow.FreezeAt = newFreeze;
                arrow.LockGroup = newLock;
                Dirty();
            }
            if (arrow.LockGroup >= 0)
                EditorGUILayout.HelpBox("Kilitli ok. Anahtar artık AYRI bir hexagon (Random Fill 'Kilitli Ok' otomatik yerleştirir).", MessageType.None);

            if (cell.Type == CellType.Head)
                EditorGUILayout.HelpBox("Seçili HEAD'e tekrar tıkla → uçuş yönü döner.", MessageType.None);
        }

        // ── Level ayarları ────────────────────────────────────────────────────

        private void DrawLevelSettings()
        {
            if (_selected == null) return;
            GUILayout.Label("Level", EditorStyles.boldLabel);
            GUILayout.Label($"{_selected.Arrows.Length} ok · {_selected.Cells.Length} hücre", EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            int newRadius = EditorGUILayout.IntSlider("Bölge Yarıçapı", _selected.Radius, 2, 8);
            if (EditorGUI.EndChangeCheck())
            {
                Record("Change Radius");
                _selected.Radius = newRadius;
                Dirty();
            }

            var so = new SerializedObject(_selected);
            var diffProp = so.FindProperty("_difficulty");
            if (diffProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(diffProp, new GUIContent("Zorluk Etiketi"));
                if (EditorGUI.EndChangeCheck()) so.ApplyModifiedProperties(); // Hard → main menüde tag
            }
        }

        // ── Paletleri Ata / Scramble / Çöz ────────────────────────────────────

        /// <summary>Çizge boyaması: aynı paletteki iki ok hiçbir hücrede komşu olamaz.</summary>
        private void AutoAssignPalettes()
        {
            int n = _selected.Arrows.Length;
            var adj = new HashSet<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new HashSet<int>();

            foreach (var cell in _selected.Cells)
            {
                for (int d = 0; d < 6; d++)
                {
                    // katmanlar-arası: gömülü ok yüzeye çıkınca komşu olabileceği herkesle ayrışmalı
                    foreach (var nb in CellsAt(cell.Q + HexCoord.Dirs[d].dq, cell.R + HexCoord.Dirs[d].dr))
                    {
                        if (nb.ArrowId != cell.ArrowId)
                        {
                            adj[cell.ArrowId].Add(nb.ArrowId);
                            adj[nb.ArrowId].Add(cell.ArrowId);
                        }
                    }
                }
            }

            Record("Auto Palette");
            const int paletteCount = 10;
            var useCnt = new int[paletteCount];
            var assigned = Enumerable.Repeat(-1, n).ToArray();
            bool ok = true;

            for (int i = 0; i < n; i++)
            {
                var banned = new HashSet<int>();
                foreach (int j in adj[i]) if (assigned[j] >= 0) banned.Add(assigned[j]);
                int pick = Enumerable.Range(0, paletteCount)
                    .Where(p => !banned.Contains(p))
                    .OrderBy(p => useCnt[p])
                    .DefaultIfEmpty(-1)
                    .First();
                if (pick < 0) { ok = false; pick = 0; }
                assigned[i] = pick;
                useCnt[pick]++;
            }

            for (int i = 0; i < n; i++) _selected.Arrows[i].Palette = assigned[i];
            Dirty();
            if (!ok) Debug.LogWarning("[LevelEditor] Palet boyaması tam çözülemedi — komşuluk ihlali kaldı, doğrulamaya bak.");
        }

        /// <summary>Rot'ları karıştırır; hiçbir ok baştan bağlı kalmaz (üreticiyle aynı garanti).</summary>
        private void ScrambleSelected()
        {
            Record("Scramble");
            var rng = new Mulberry32(System.Environment.TickCount);
            foreach (var cell in _selected.Cells)
                cell.Rot = rng.RangeInclusive(0, 5);

            var level = _selected.ToHexaLevel();
            foreach (var arrow in level.Arrows)
            {
                int guard = 0;
                while (ConnectionTracer.Trace(level, arrow.ArrowId).Connected && guard++ < 20)
                {
                    var head = level.GetCell(arrow.HeadPos);
                    head.Rot = (head.Rot + rng.RangeInclusive(1, 5)) % 6;
                    var save = CellAt(head.Q, head.R, 0); // bağlı ok = tümü yüzeyde → head katman 0
                    save.Rot = head.Rot;
                }
            }
            Dirty();
        }

        private void SolveSelected()
        {
            Record("Solve");
            foreach (var cell in _selected.Cells) cell.Rot = 0;
            Dirty();
        }

        // ── Random Fill ───────────────────────────────────────────────────────

        private static readonly int[] FillDifficulties = { 1, 2, 3, 5 };
        private static readonly string[] FillDifficultyNames =
            { "1 — Giriş (3 ok)", "2 — Kolay (5 ok)", "3-4 — Orta (6 ok, buz)", "5+ — Zor (14 ok, buz)" };

        private void DrawRandomFill()
        {
            _fillCustom = EditorGUILayout.ToggleLeft("Özel (ok / katman / nested)", _fillCustom);

            if (_fillCustom)
            {
                _fillArrows = EditorGUILayout.IntSlider("Ok Sayısı", _fillArrows, 1, 20);
                _fillLayers = EditorGUILayout.IntSlider("Katman", _fillLayers, 1, HexaLevel.MaxBuriedLayers + 1);

                // Gömülü STİL: İç içe (Nested — küçük iç hexagon) vs Alt alta (Stacked — taşlar üst üste, yükselerek çıkar)
                using (new EditorGUI.DisabledScope(_fillLayers <= 1))
                    _fillBuriedStyle = EditorGUILayout.Popup("Gömülü Stil", _fillBuriedStyle, new[] { "İç içe (Nested)", "Alt alta (Stacked)" });

                // Gömülü ok sayısı (Nested: kaç iç hexagon / Stacked: kaç yayılan ok). Max = ok-1 (kapatıcı yüzey oku).
                int nestedMax = Mathf.Max(0, _fillArrows - 1);
                using (new EditorGUI.DisabledScope(_fillLayers <= 1))
                    _fillNested = EditorGUILayout.IntSlider(_fillBuriedStyle == 1 ? "Yayılan Ok" : "Nested Sayısı",
                        Mathf.Clamp(_fillNested, 0, nestedMax), 0, nestedMax);
                if (_fillLayers <= 1) _fillNested = 0; // düz levelda gömülü yok

                _fillIce = EditorGUILayout.IntSlider("Buzlu Ok", Mathf.Min(_fillIce, _fillArrows), 0, Mathf.Min(_fillArrows, 6));

                // KİLİTLİ ok: tek grup, anahtar = ilk çıkabilen ok, kilitliler = en geç çıkanlar (çözülebilirlik korunur).
                // Yalnız düz levelda (Katman 1) — katmanlı çıkış sırası dinamik, statik grafikle güvenli değil.
                int lockedMax = Mathf.Max(0, _fillArrows - 1); // en az anahtar için 1 ok açıkta kalmalı
                using (new EditorGUI.DisabledScope(_fillLayers > 1))
                    _fillLocked = EditorGUILayout.IntSlider("Kilitli Ok", Mathf.Clamp(_fillLocked, 0, lockedMax), 0, lockedMax);
                if (_fillLayers > 1) _fillLocked = 0; // katmanlı + kilit henüz desteklenmiyor
            }
            else
            {
                int idx = System.Array.IndexOf(FillDifficulties, _fillDifficulty);
                if (idx < 0) idx = 0;
                idx = EditorGUILayout.Popup("Zorluk", idx, FillDifficultyNames);
                _fillDifficulty = FillDifficulties[idx];
            }

            EditorGUILayout.BeginHorizontal();
            _fillSeed = EditorGUILayout.IntField("Seed", _fillSeed);
            if (GUILayout.Button("🎲", GUILayout.Width(30)))
                _fillSeed = Random.Range(1, 999999);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(_fillCustom
                ? "Nested Sayısı = kaç küçük iç hexagon (gömülü hücre). Kilitli Ok = kaç ok kilitli olacak (tek grup; " +
                  "anahtar = ilk çıkabilen ok, kilitliler = en geç çıkanlar → çözülebilir kalır; düz level). " +
                  "Katman≥2 nested için; hedeflenen sayılar üretim tutmazsa yaklaşır."
                : "Seçili level'ın İÇERİĞİNİ prosedürel üretimle DEĞİŞTİRİR (scramble + buz dahil). Seed tekrarı aynı level'ı verir.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(_selected == null))
            {
                if (GUILayout.Button("Random Fill"))
                {
                    // Nested: yayılan ok başına TAM 1 gömülü hücre (küçük iç hexagon). Stacked: 1..Katman-1 gömülü (üst üste yığın).
                    int bmax = (_fillCustom && _fillBuriedStyle == 1) ? Mathf.Max(1, _fillLayers - 1) : 1;
                    var cfg = _fillCustom
                        ? LevelConfig.ForCustom(_fillArrows, _fillLayers, _fillNested, _fillIce, 1, bmax)
                        : LevelConfig.ForLevel(_fillDifficulty);
                    try
                    {
                        var level = LevelGenerator.Generate(_fillSeed, cfg);
                        if (_fillCustom && _fillLocked > 0 && _fillLayers <= 1)
                            AssignLockKey(level, _fillLocked, cfg.Radius);
                        Record("Random Fill");
                        _selected.FromHexaLevel(level, cfg.Radius);
                        _selected.StackedLayers = _fillCustom && _fillLayers > 1 && _fillBuriedStyle == 1; // gömülü stil levelde saklanır
                        Dirty();
                        if (_fillCustom && _fillLayers > 1) ReportSpanning(level);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[LevelEditor] Random Fill başarısız (seed {_fillSeed}) — farklı seed / daha az yayılan ok dene. {ex.Message}");
                    }
                }
            }
        }

        /// <summary>Düz levelda kilit/anahtar atar: çıkış sırasını BlockedBy'dan üretir (Kahn peel);
        /// İLK çıkan okun çıkış yolunun önüne ANAHTAR HEXAGONU (grup 0) konur, kilitliler = EN GEÇ çıkan `count` ok.
        /// İlk ok çıkarken anahtara çarpar → kilit açılır; kalan sıra aynen işler → çözülebilirlik korunur.</summary>
        private static void AssignLockKey(HexaLevel level, int count, int radius)
        {
            int n = level.Arrows.Count;
            if (n < 2 || level.BlockedBy == null || level.BlockedBy.Count != n) return;

            var exited = new bool[n];
            var order = new System.Collections.Generic.List<int>(n);
            bool progress = true;
            while (order.Count < n && progress)
            {
                progress = false;
                for (int c = 0; c < n; c++)
                {
                    if (exited[c]) continue;
                    bool ready = true;
                    foreach (int b in level.BlockedBy[c]) if (!exited[b]) { ready = false; break; }
                    if (ready) { exited[c] = true; order.Add(c); progress = true; }
                }
            }
            if (order.Count < n) return; // çözülemez (beklenmez) → kilit ekleme

            // İLK çıkan okun çıkış yolunda bölge-içi boş bir hücreye anahtar hexagonu koy
            int keyArrow = order[0];
            var ka = level.Arrows[keyArrow];
            var head = level.GetArrowCell(keyArrow, ka.HeadPos);
            var (dq, dr) = HexCoord.Dirs[ka.ExitDir];
            int kq = head.Q, kr = head.R; bool placed = false;
            for (int step = 1; step <= 4; step++)
            {
                kq += dq; kr += dr;
                if (HexCoord.InRegion(kq, kr, radius) && level.GetCell(kq, kr) == null)
                { level.Keys.Add(new KeyCell { Q = kq, R = kr, Group = 0 }); placed = true; break; }
            }
            if (!placed) { Debug.LogWarning("[LevelEditor] Anahtar için uygun boş hücre yok — kilit eklenmedi."); return; }

            count = Mathf.Clamp(count, 0, n - 1);
            int locked = 0;
            for (int i = n - 1; i >= 1 && locked < count; i--) // en geç çıkanlardan kilitle (anahtar okunu kilitleme)
            {
                int id = order[i];
                if (id == keyArrow) continue;
                level.Arrows[id].LockGroup = 0;
                locked++;
            }
            Debug.Log($"[LevelEditor] Kilit: anahtar hexagonu ({kq},{kr}) grup 0, {locked} kilitli ok. Çıkış sırası: {string.Join(",", order)}");
        }

        /// <summary>Üretilen levelda yayılan ok sayısı + katman başına toplam parça dağılımını konsola yazar.</summary>
        private static void ReportSpanning(HexaLevel level)
        {
            int spanning = 0, maxLayer = 0;
            var perLayer = new Dictionary<int, int>();
            var spanDetail = new System.Text.StringBuilder();
            foreach (var arrow in level.Arrows)
            {
                var seen = new Dictionary<int, int>(); // katman → bu okun o katmandaki parça sayısı
                foreach (var pos in arrow.Cells)
                {
                    var c = level.GetArrowCell(arrow.ArrowId, pos);
                    seen.TryGetValue(c.Layer, out int n); seen[c.Layer] = n + 1;
                    perLayer.TryGetValue(c.Layer, out int t); perLayer[c.Layer] = t + 1;
                    if (c.Layer > maxLayer) maxLayer = c.Layer;
                }
                if (seen.Count >= 2)
                {
                    spanning++;
                    var parts = seen.OrderBy(kv => kv.Key).Select(kv => $"L{kv.Key}:{kv.Value}");
                    spanDetail.Append($" [ok{arrow.ArrowId} {string.Join("/", parts)}]");
                }
            }
            var dist = string.Join(", ", perLayer.OrderBy(kv => kv.Key).Select(kv => $"L{kv.Key}={kv.Value}"));
            Debug.Log($"[LevelEditor] Katmanlı üretim: {level.Arrows.Count} ok, en derin katman {maxLayer}, " +
                      $"yayılan ok: {spanning}. Parça dağılımı: {dist}. Yayılanlar:{spanDetail} (seed {level.Seed})");
        }

        // ── Doğrulama ─────────────────────────────────────────────────────────

        private void DrawValidation()
        {
            if (_selected == null) return;
            var problems = Validate();
            if (problems.Count == 0)
            {
                var okStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.4f, 0.9f, 0.5f) } };
                GUILayout.Label("✔ Level geçerli — çözülebilir, karışık, palet ve buz kuralları tamam.", okStyle);
                return;
            }
            var bad = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 0.55f, 0.5f) }, wordWrap = true };
            foreach (var p in problems.Take(6))
                GUILayout.Label("• " + p, bad);
            if (problems.Count > 6)
                GUILayout.Label($"… ve {problems.Count - 6} sorun daha", bad);
        }

        private List<string> Validate()
        {
            var problems = new List<string>();
            var data = _selected;
            if (data.Arrows.Length == 0)
            {
                problems.Add("Level boş — Draw ile ok çiz veya Random Fill kullan.");
                return problems;
            }

            // çakışma (katman başına) + bölge + katman kuralları
            var seen = new HashSet<(int, int, int)>();
            foreach (var c in data.Cells)
            {
                if (!seen.Add((c.Q, c.R, c.Layer))) problems.Add($"Çakışma: ({c.Q},{c.R}) katman {c.Layer} iki kez dolu.");
                if (!HexCoord.InRegion(c.Q, c.R, data.Radius)) problems.Add($"({c.Q},{c.R}) bölge dışında (R={data.Radius}).");
                if (c.ArrowId < 0 || c.ArrowId >= data.Arrows.Length) problems.Add($"({c.Q},{c.R}) geçersiz arrowId {c.ArrowId}.");
                if (c.Layer < 0 || c.Layer > HexaLevel.MaxBuriedLayers) problems.Add($"({c.Q},{c.R}) geçersiz katman {c.Layer} (0..{HexaLevel.MaxBuriedLayers}).");
            }

            // gömülü hücrenin üstü dolu olmalı: katman L>0 → aynı (q,r)'de L-1 şart
            // (üstü boş gömülü hücre ASLA yüzeye çıkamaz; katman boşluğu da yasak)
            foreach (var c in data.Cells)
            {
                if (c.Layer <= 0) continue;
                if (CellAt(c.Q, c.R, c.Layer - 1) == null)
                    problems.Add($"Ok {c.ArrowId}: ({c.Q},{c.R}) katman {c.Layer} hücresinin üstünde katman {c.Layer - 1} yok — asla yüzeye çıkamaz.");
            }

            // yapı + bitişiklik (dizide ok sırası + kuyruk→head varsayılır)
            for (int a = 0; a < data.Arrows.Length; a++)
            {
                var cells = data.Cells.Where(c => c.ArrowId == a).ToList();
                if (cells.Count < 2) { problems.Add($"Ok {a}: {cells.Count} hücre (en az 2)."); continue; }
                if (cells[0].Type != CellType.Tail) problems.Add($"Ok {a}: ilk hücre Tail değil.");
                if (cells[cells.Count - 1].Type != CellType.Head) problems.Add($"Ok {a}: son hücre Head değil.");
                for (int i = 1; i < cells.Count - 1; i++)
                    if (cells[i].Type != CellType.Mid) { problems.Add($"Ok {a}: ara hücre Mid değil."); break; }
                for (int i = 0; i + 1 < cells.Count; i++)
                    if (HexCoord.DirBetween((cells[i].Q, cells[i].R), (cells[i + 1].Q, cells[i + 1].R)) < 0)
                    { problems.Add($"Ok {a}: {i}→{i + 1} hücreleri komşu değil."); break; }
            }
            if (problems.Count > 0) return problems; // yapı bozuksa mantık kontrolleri anlamsız

            bool layered = data.Cells.Any(c => c.Layer > 0);

            // çözülebilirlik: her okun a/b zinciri rot=0'da bağlanmalı. Ok gömülü olabileceği için
            // OK BAŞINA mini level kurulur (yalnızca o okun hücreleri, hepsi yüzeyde) — katmandan bağımsız kontrol.
            var solved = data.ToHexaLevel();
            foreach (var cell in solved.Cells.Values) cell.Rot = 0;
            for (int a = 0; a < data.Arrows.Length; a++)
            {
                var mini = new HexaLevel();
                for (int i = 0; i < data.Arrows.Length; i++)
                    mini.Arrows.Add(new Arrow { ArrowId = i, Palette = data.Arrows[i].Palette });
                foreach (var c in data.Cells.Where(c => c.ArrowId == a))
                {
                    mini.AddCell(new Cell { Q = c.Q, R = c.R, ArrowId = a, Type = c.Type, A = c.A, B = c.B, Rot = 0, Layer = 0 });
                    mini.Arrows[a].Cells.Add((c.Q, c.R));
                }
                if (!ConnectionTracer.Trace(mini, a).Connected)
                    problems.Add($"Ok {a}: rot=0'da bağlanmıyor (a/b değerleri tutarsız).");
            }

            // başlangıç karışıklığı: mevcut rot'larla bağlı ok olmamalı (gömülü ok zaten bağlanamaz)
            var current = data.ToHexaLevel();
            foreach (var arrow in current.Arrows)
                if (ConnectionTracer.Trace(current, arrow.ArrowId).Connected)
                    problems.Add($"Ok {arrow.ArrowId}: level başında BAĞLI — Scramble kullan.");

            // palet komşuluğu — KATMANLAR-ARASI: gömülü ok yüzeye çıkınca aynı paletli komşuyla
            // yan yana gelebilir; bu yüzden komşuluk (q,r) uzayında tüm katman çiftlerine bakar
            foreach (var c in data.Cells)
            {
                bool flagged = false;
                for (int d = 0; d < 6 && !flagged; d++)
                {
                    foreach (var nb in CellsAt(c.Q + HexCoord.Dirs[d].dq, c.R + HexCoord.Dirs[d].dr))
                    {
                        if (nb.ArrowId != c.ArrowId &&
                            data.Arrows[c.ArrowId].Palette == data.Arrows[nb.ArrowId].Palette)
                        {
                            problems.Add($"Palet ihlali: ok {c.ArrowId} ve {nb.ArrowId} aynı palette komşu (katman {c.Layer}/{nb.Layer}) — 'kopuk ok' bug'ı!");
                            flagged = true; // hücre başına bir uyarı yeter
                            break;
                        }
                    }
                }
            }

            // buz + engelleme: katmanlıysa DİNAMİK simülasyon (terfi eden hücre yeni engel olabilir),
            // düz levelda klasik statik grafik + simülasyon
            if (layered)
            {
                if (!ExitSimulator.CanExitAllLayered(data.ToHexaLevel()))
                    problems.Add("DEADLOCK: bu diziliş + katmanlar + buz eşikleriyle tüm oklar çıkamaz.");
            }
            else
            {
                var blockedBy = BuildBlockedBy(solved, data.Radius);
                var freeze = data.Arrows.Select(x => x.FreezeAt).ToArray();
                if (!ExitSimulator.CanExitAll(blockedBy, freeze))
                    problems.Add("DEADLOCK: bu diziliş + buz eşikleriyle tüm oklar çıkamaz.");
            }

            // anahtar mekaniği tutarlılığı: her kilit grubunun bir anahtar hexagonu olmalı
            var lockGroups = new HashSet<int>();
            var keyGroups = new HashSet<int>();
            for (int a = 0; a < data.Arrows.Length; a++)
                if (data.Arrows[a].LockGroup >= 0) lockGroups.Add(data.Arrows[a].LockGroup);
            if (data.Keys != null) foreach (var k in data.Keys) keyGroups.Add(k.Group);
            foreach (var g in lockGroups)
                if (!keyGroups.Contains(g))
                    problems.Add($"Kilit grubu {g}: anahtar hexagonu yok (aynı gruptan bir anahtar gerekli) — kilitli oklar asla açılmaz.");

            return problems.Distinct().ToList();
        }

        private static List<HashSet<int>> BuildBlockedBy(HexaLevel solved, int radius)
        {
            int n = solved.Arrows.Count;
            var blockedBy = new List<HashSet<int>>(n);
            for (int i = 0; i < n; i++) blockedBy.Add(new HashSet<int>());

            foreach (var arrow in solved.Arrows)
            {
                var head = solved.GetCell(arrow.HeadPos);
                int exitDir = head.WorldB; // rot=0 → lokal B
                var (dq, dr) = HexCoord.Dirs[exitDir];
                int q = head.Q, r = head.R;
                for (int k = 0; k < 2 * radius + 4; k++)
                {
                    q += dq; r += dr;
                    var cc = solved.GetCell(q, r);
                    if (cc != null && cc.ArrowId != arrow.ArrowId)
                        blockedBy[arrow.ArrowId].Add(cc.ArrowId);
                }
            }
            return blockedBy;
        }
    }
}

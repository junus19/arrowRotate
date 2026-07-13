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
            var palettes = ArrowRotate.View.HexaPalette.Palettes;
            for (int row = 0; row < 2; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = row * 5; i < row * 5 + 5; i++)
                {
                    var prev = GUI.backgroundColor;
                    GUI.backgroundColor = palettes[i];
                    string label = _paintPalette == i ? "✓" : " ";
                    if (GUILayout.Button(label, GUILayout.Width(42), GUILayout.Height(24)))
                        _paintPalette = i;
                    GUI.backgroundColor = prev;
                }
                EditorGUILayout.EndHorizontal();
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

            GUILayout.Label($"Hücre ({cell.Q}, {cell.R}) · Ok {cell.ArrowId} · {cell.Type}", EditorStyles.miniBoldLabel);

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

            var arrow = _selected.Arrows[cell.ArrowId];
            EditorGUI.BeginChangeCheck();
            int newPalette = EditorGUILayout.IntSlider("Palet", arrow.Palette, 0, 9);
            int newFreeze = EditorGUILayout.IntSlider("Buz Eşiği", arrow.FreezeAt, 0, 3);
            if (EditorGUI.EndChangeCheck())
            {
                Record("Edit Arrow");
                arrow.Palette = newPalette;
                arrow.FreezeAt = newFreeze;
                Dirty();
            }

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
                    var nb = CellAt(cell.Q + HexCoord.Dirs[d].dq, cell.R + HexCoord.Dirs[d].dr);
                    if (nb != null && nb.ArrowId != cell.ArrowId)
                    {
                        adj[cell.ArrowId].Add(nb.ArrowId);
                        adj[nb.ArrowId].Add(cell.ArrowId);
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
                    var save = CellAt(head.Q, head.R);
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
            int idx = System.Array.IndexOf(FillDifficulties, _fillDifficulty);
            if (idx < 0) idx = 0;
            idx = EditorGUILayout.Popup("Zorluk", idx, FillDifficultyNames);
            _fillDifficulty = FillDifficulties[idx];

            EditorGUILayout.BeginHorizontal();
            _fillSeed = EditorGUILayout.IntField("Seed", _fillSeed);
            if (GUILayout.Button("🎲", GUILayout.Width(30)))
                _fillSeed = Random.Range(1, 999999);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Seçili level'ın İÇERİĞİNİ prosedürel üretimle DEĞİŞTİRİR " +
                                    "(scramble + buz dahil). Seed tekrarı aynı level'ı verir.", MessageType.None);

            using (new EditorGUI.DisabledScope(_selected == null))
            {
                if (GUILayout.Button("Random Fill"))
                {
                    var cfg = LevelConfig.ForLevel(_fillDifficulty);
                    var level = LevelGenerator.Generate(_fillSeed, cfg);
                    Record("Random Fill");
                    _selected.FromHexaLevel(level, cfg.Radius);
                    Dirty();
                }
            }
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

            // çakışma + bölge
            var seen = new HashSet<(int, int)>();
            foreach (var c in data.Cells)
            {
                if (!seen.Add((c.Q, c.R))) problems.Add($"Çakışma: ({c.Q},{c.R}) iki kez dolu.");
                if (!HexCoord.InRegion(c.Q, c.R, data.Radius)) problems.Add($"({c.Q},{c.R}) bölge dışında (R={data.Radius}).");
                if (c.ArrowId < 0 || c.ArrowId >= data.Arrows.Length) problems.Add($"({c.Q},{c.R}) geçersiz arrowId {c.ArrowId}.");
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

            // çözülebilirlik: rot=0 kopyada her ok bağlı olmalı
            var solved = data.ToHexaLevel();
            foreach (var cell in solved.Cells.Values) cell.Rot = 0;
            foreach (var arrow in solved.Arrows)
                if (!ConnectionTracer.Trace(solved, arrow.ArrowId).Connected)
                    problems.Add($"Ok {arrow.ArrowId}: rot=0'da bağlanmıyor (a/b değerleri tutarsız).");

            // başlangıç karışıklığı: mevcut rot'larla bağlı ok olmamalı
            var current = data.ToHexaLevel();
            foreach (var arrow in current.Arrows)
                if (ConnectionTracer.Trace(current, arrow.ArrowId).Connected)
                    problems.Add($"Ok {arrow.ArrowId}: level başında BAĞLI — Scramble kullan.");

            // palet komşuluğu
            foreach (var c in data.Cells)
            {
                for (int d = 0; d < 6; d++)
                {
                    var nb = CellAt(c.Q + HexCoord.Dirs[d].dq, c.R + HexCoord.Dirs[d].dr);
                    if (nb != null && nb.ArrowId != c.ArrowId &&
                        data.Arrows[c.ArrowId].Palette == data.Arrows[nb.ArrowId].Palette)
                    {
                        problems.Add($"Palet ihlali: ok {c.ArrowId} ve {nb.ArrowId} aynı palette komşu — 'kopuk ok' bug'ı!");
                        d = 6; // hücre başına bir uyarı yeter
                    }
                }
            }

            // buz + DAG: çözülmüş halden engelleme grafiği → simülasyon
            var blockedBy = BuildBlockedBy(solved, data.Radius);
            var freeze = data.Arrows.Select(x => x.FreezeAt).ToArray();
            if (!ExitSimulator.CanExitAll(blockedBy, freeze))
                problems.Add("DEADLOCK: bu diziliş + buz eşikleriyle tüm oklar çıkamaz.");

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

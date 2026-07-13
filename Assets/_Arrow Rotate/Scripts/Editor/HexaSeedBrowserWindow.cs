using System.Collections.Generic;
using System.Linq;
using ArrowRotate.Core;
using ArrowRotate.Generation;
using ArrowRotate.Integration;
using ArrowRotate.Logic;
using GameBrain.Casual;
using UnityEditor;
using UnityEngine;

namespace ArrowRotate.EditorTools
{
    /// <summary>
    /// Seed gezgini: config + seed aralığı tara → zorluk istatistikleri + mini önizleme;
    /// beğenilen seed'leri HexaLevelData asset'i olarak bake et; klasörü GameConfig'e ata.
    /// Menü: Arrow Rotate ▸ Seed Browser
    /// </summary>
    public class HexaSeedBrowserWindow : EditorWindow
    {
        private const string LevelsFolder = "Assets/_Arrow Rotate/Data/Levels";
        private const string ConfigPath = "Assets/_Arrow Rotate/Data/Hexa Game Config.asset";

        private int _difficulty = 1;
        private int _startSeed = 1000;
        private int _count = 20;
        private Vector2 _scroll;
        private readonly List<Row> _rows = new List<Row>();
        private int _previewIndex = -1;

        private class Row
        {
            public int Seed;
            public bool Selected;
            public int Arrows;
            public int Cells;
            public int Edges;
            public int Frozen;
            public bool Valid;
            public HexaLevel Level;
        }

        [MenuItem("Arrow Rotate/Seed Browser")]
        public static void Open() => GetWindow<HexaSeedBrowserWindow>("Hexa Seed Browser");

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Üretim Taraması", EditorStyles.boldLabel);
            _difficulty = EditorGUILayout.IntSlider("Zorluk (tablo satırı)", _difficulty, 1, 5);
            _startSeed = EditorGUILayout.IntField("Başlangıç Seed", _startSeed);
            _count = EditorGUILayout.IntSlider("Adet", _count, 1, 100);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Tara", GUILayout.Height(26))) Scan();
                if (GUILayout.Button("Seçilenleri Bake Et", GUILayout.Height(26))) BakeSelected();
                if (GUILayout.Button("Klasörü Config'e Ata", GUILayout.Height(26))) AssignFolderToConfig();
            }

            EditorGUILayout.Space(4);
            if (_rows.Count > 0)
            {
                EditorGUILayout.LabelField($"{_rows.Count} sonuç — ort. engel: {_rows.Average(r => r.Edges):F1}", EditorStyles.miniLabel);
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(180));
                for (int i = 0; i < _rows.Count; i++)
                {
                    var row = _rows[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        row.Selected = EditorGUILayout.Toggle(row.Selected, GUILayout.Width(18));
                        GUI.color = row.Valid ? Color.white : new Color(1f, 0.5f, 0.5f);
                        EditorGUILayout.LabelField(
                            $"seed {row.Seed}  ·  {row.Arrows} ok / {row.Cells} hücre  ·  engel {row.Edges}  ·  buz {row.Frozen}" + (row.Valid ? "" : "  ⚠"),
                            GUILayout.MinWidth(280));
                        GUI.color = Color.white;
                        if (GUILayout.Button("Önizle", GUILayout.Width(60))) _previewIndex = i;
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            if (_previewIndex >= 0 && _previewIndex < _rows.Count)
                DrawPreview(_rows[_previewIndex]);
        }

        private void Scan()
        {
            _rows.Clear();
            _previewIndex = -1;
            var cfg = LevelConfig.ForLevel(_difficulty);
            for (int i = 0; i < _count; i++)
            {
                int seed = _startSeed + i;
                var row = new Row { Seed = seed };
                try
                {
                    var level = LevelGenerator.Generate(seed, cfg);
                    row.Level = level;
                    row.Arrows = level.Arrows.Count;
                    row.Cells = level.Cells.Count;
                    row.Edges = level.EdgeCount;
                    row.Frozen = level.Arrows.Count(a => a.FreezeAt > 0);
                    row.Valid = ExitSimulator.CanExitAll(level.BlockedBy, level.Arrows.Select(a => a.FreezeAt).ToArray())
                                && level.Arrows.All(a => !ConnectionTracer.Trace(level, a.ArrowId).Connected);
                }
                catch (System.Exception ex)
                {
                    row.Valid = false;
                    Debug.LogWarning($"seed {seed}: üretim hatası — {ex.Message}");
                }
                _rows.Add(row);
            }
        }

        private void BakeSelected()
        {
            var selected = _rows.Where(r => r.Selected && r.Valid).ToList();
            if (selected.Count == 0)
            {
                EditorUtility.DisplayDialog("Seed Browser", "Geçerli + seçili satır yok.", "Tamam");
                return;
            }

            System.IO.Directory.CreateDirectory(LevelsFolder);
            int nextIndex = AssetDatabase.FindAssets("t:HexaLevelData", new[] { LevelsFolder }).Length + 1;

            foreach (var row in selected)
            {
                var asset = CreateInstance<HexaLevelData>();
                asset.EditorInit(row.Seed, _difficulty);
                string path = AssetDatabase.GenerateUniqueAssetPath($"{LevelsFolder}/HexaLevel_{nextIndex:000}.asset");
                AssetDatabase.CreateAsset(asset, path);
                nextIndex++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[SeedBrowser] {selected.Count} level bake edildi (zorluk {_difficulty}).");
        }

        private void AssignFolderToConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (cfg == null)
            {
                EditorUtility.DisplayDialog("Seed Browser", $"Config bulunamadı: {ConfigPath}", "Tamam");
                return;
            }

            var levels = AssetDatabase.FindAssets("t:HexaLevelData", new[] { LevelsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<HexaLevelData>)
                .Where(a => a != null)
                .ToArray();

            var so = new SerializedObject(cfg);
            var prop = so.FindProperty("_levels");
            prop.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[SeedBrowser] {levels.Length} level Config'e atandı.");
        }

        // ── mini önizleme: hücreler palet renginde disk, buzlular açık mavi çerçeveli ──
        private void DrawPreview(Row row)
        {
            if (row.Level == null) return;
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Önizleme — seed {row.Seed}", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(position.width - 20, 240);
            EditorGUI.DrawRect(rect, new Color(0.357f, 0.384f, 0.522f));

            var level = row.Level;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var cell in level.Cells.Values)
            {
                var (x, y) = HexMetrics.Center(cell.Q, cell.R, 1f);
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
            float spanX = Mathf.Max(1f, maxX - minX), spanY = Mathf.Max(1f, maxY - minY);
            float scale = Mathf.Min((rect.width - 40f) / spanX, (rect.height - 40f) / spanY);
            float cx = rect.x + rect.width * 0.5f, cy = rect.y + rect.height * 0.5f;
            float midX = (minX + maxX) * 0.5f, midY = (minY + maxY) * 0.5f;
            float r = Mathf.Clamp(scale * 0.42f, 3f, 14f);

            foreach (var arrow in level.Arrows)
            {
                var palette = ArrowRotate.View.HexaPalette.ForPalette(arrow.Palette);
                foreach (var pos in arrow.Cells)
                {
                    var cell = level.GetCell(pos);
                    var (x, y) = HexMetrics.Center(cell.Q, cell.R, 1f);
                    // editör GUI'sinde y aşağı — HexMetrics y-yukarı verdiği için aynala
                    var p = new Vector2(cx + (x - midX) * scale, cy - (y - midY) * scale);
                    if (arrow.FreezeAt > 0)
                        EditorGUI.DrawRect(new Rect(p.x - r - 2, p.y - r - 2, 2 * r + 4, 2 * r + 4), new Color(0.78f, 0.9f, 1f, 0.9f));
                    EditorGUI.DrawRect(new Rect(p.x - r, p.y - r, 2 * r, 2 * r), palette);
                }
            }
            EditorGUILayout.LabelField("(kare = hücre, açık mavi çerçeve = buzlu ok)", EditorStyles.miniLabel);
        }
    }
}

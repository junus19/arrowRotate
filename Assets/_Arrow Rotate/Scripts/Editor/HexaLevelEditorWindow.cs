using System.Collections.Generic;
using System.Linq;
using ArrowRotate.Core;
using ArrowRotate.Integration;
using GameBrain.Casual;
using UnityEditor;
using UnityEngine;

namespace ArrowRotate.EditorTools
{
    /// <summary>
    /// Görsel level editörü — Arrow Rotate ▸ Level Editor (arrowJam editör deseninin hex uyarlaması).
    ///
    /// Yerleşim:
    ///   Sol   — level listesi (seç / yeniden adlandır / oluştur / kopyala / sil / GameConfig checkbox).
    ///   Orta  — etkileşimli hex canvas (tail nokta, mid kırık çizgi, head ok; rot uygulanmış halde).
    ///   Sağ   — araçlar, palet, hücre denetçisi, bölge yarıçapı, Paletleri Ata / Scramble / Çöz,
    ///           Random Fill ve doğrulama paneli.
    ///
    /// Araçlar:
    ///   Draw    — boş hücrelerde sürükle: KUYRUK sürükleme başında, HEAD bırakılan hücrede;
    ///             a/b bağlantıları otomatik, head uçuş yönü "düz devam" (Edit ile değiştirilir).
    ///   Erase   — tıkla/sürükle: hücrenin OKUNU tümüyle siler (tek hücre silmek zinciri bozar).
    ///             Sağ tık her araçta siler.
    ///   Move    — oku (tüm hücreleriyle) yeni yere taşır.
    ///   Rotate  — hücreye tıkla → rot +1 (oyundaki tap'in editör karşılığı).
    ///   Recolor — okun paletini seçili renge boyar.
    ///   Ice     — okun buz eşiğini döndürür (0 → 1 → 2 → 3 → 0).
    ///   Edit    — hücre denetçisi; seçili HEAD'e tekrar tıkla → uçuş yönü döner.
    ///
    /// Tüm düzenlemeler Undo destekli, doğrudan HexaLevelData asset'ine yazılır.
    /// </summary>
    public partial class HexaLevelEditorWindow : EditorWindow
    {
        private enum Tool { Draw, Erase, Move, Rotate, Recolor, Ice, Edit }

        private const string LevelsFolder = "Assets/_Arrow Rotate/Data/Levels";
        private const string ConfigPath = "Assets/_Arrow Rotate/Data/Hexa Game Config.asset";

        // ── Level list ────────────────────────────────────────────────────────
        private readonly List<HexaLevelData> _levels = new List<HexaLevelData>();
        private HexaLevelData _selected;
        private Vector2 _listScroll;
        private HexaLevelData _renamingLevel;
        private string _renameBuffer = "";

        // ── Tool state ────────────────────────────────────────────────────────
        private Tool _tool = Tool.Draw;
        private int _paintPalette;
        private Vector2 _toolsScroll;

        private readonly List<(int q, int r)> _drawPath = new List<(int, int)>();
        private bool _drawing;

        private readonly List<int> _moveCellIdx = new List<int>(); // _selected.Cells indeksleri
        private int _moveArrowId = -1;
        private (int q, int r) _moveAnchor;
        private (int q, int r) _moveOffset;
        private bool _moving;

        private (int q, int r) _editCell = (int.MinValue, 0);
        private bool _erasing;

        // ── Random Fill ───────────────────────────────────────────────────────
        private bool _fillFoldout;
        private int _fillDifficulty = 1;
        private int _fillSeed = 1000;

        // ── Stil sabitleri ────────────────────────────────────────────────────
        private static readonly Color CanvasBg = new Color(0.10f, 0.10f, 0.14f);
        private static readonly Color EmptyHex = new Color(0.16f, 0.16f, 0.24f);
        private static readonly Color EmptyDot = new Color(0.40f, 0.40f, 0.60f);
        private static readonly Color IceTint = new Color(0.78f, 0.90f, 1f, 0.55f);

        [MenuItem("Arrow Rotate/Level Editor")]
        public static void Open() => GetWindow<HexaLevelEditorWindow>("Hexa Level Editor");

        private void OnEnable()
        {
            RefreshLevelList();
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable() => Undo.undoRedoPerformed -= Repaint;
        private void OnFocus() => RefreshLevelList();

        private void OnGUI()
        {
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            DrawLevelListPanel();
            GUILayout.Space(10);
            DrawCanvasPanel();
            GUILayout.Space(10);
            DrawToolsPanel();
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);
        }

        // ════════════════════════════════════════════════════════════════════
        //  SOL — level listesi
        // ════════════════════════════════════════════════════════════════════

        private void DrawLevelListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            GUILayout.Label("Levels", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New")) CreateNewLevel();
            using (new EditorGUI.DisabledScope(_selected == null))
            {
                if (GUILayout.Button("Duplicate")) DuplicateSelected();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));
            foreach (var lv in _levels)
            {
                if (lv == null) continue;
                EditorGUILayout.BeginHorizontal();
                if (lv == _renamingLevel)
                {
                    DrawRenameRow(lv);
                }
                else
                {
                    bool inConfig = IsInGameConfig(lv);
                    bool wantedIn = EditorGUILayout.Toggle(inConfig, GUILayout.Width(18));
                    if (wantedIn != inConfig) SetInGameConfig(lv, wantedIn);

                    var style = lv == _selected ? SelectedStyle() : EditorStyles.miniButton;
                    if (GUILayout.Button(lv.name, style)) SelectLevel(lv);

                    if (GUILayout.Button("✎", EditorStyles.miniButton, GUILayout.Width(24)))
                    {
                        _renamingLevel = lv;
                        _renameBuffer = lv.name;
                        GUI.FocusControl(null);
                    }
                    if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(24)))
                    {
                        DeleteLevel(lv);
                        EditorGUILayout.EndHorizontal();
                        GUIUtility.ExitGUI();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRenameRow(HexaLevelData lv)
        {
            GUI.SetNextControlName("RenameField");
            _renameBuffer = EditorGUILayout.TextField(_renameBuffer);
            var e = Event.current;
            bool commit = GUILayout.Button("✓", EditorStyles.miniButton, GUILayout.Width(24)) ||
                          (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return);
            bool cancel = GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(24)) ||
                          (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape);
            if (commit) CommitRename(lv);
            else if (cancel) _renamingLevel = null;
        }

        private void CommitRename(HexaLevelData lv)
        {
            string name = _renameBuffer.Trim();
            if (!string.IsNullOrEmpty(name) && name != lv.name)
            {
                string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(lv), name);
                if (!string.IsNullOrEmpty(error)) Debug.LogWarning("Rename: " + error);
            }
            _renamingLevel = null;
            RefreshLevelList();
        }

        private void SelectLevel(HexaLevelData lv)
        {
            _selected = lv;
            _renamingLevel = null;
            _editCell = (int.MinValue, 0);
            CancelDrag();
            Repaint();
            GUIUtility.ExitGUI();
        }

        private void RefreshLevelList()
        {
            _levels.Clear();
            _levels.AddRange(AssetDatabase.FindAssets("t:HexaLevelData", new[] { LevelsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p, System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<HexaLevelData>)
                .Where(a => a != null));
            if (_selected == null && _levels.Count > 0) _selected = _levels[0];
        }

        private void CreateNewLevel()
        {
            System.IO.Directory.CreateDirectory(LevelsFolder);
            var asset = CreateInstance<HexaLevelData>();
            asset.Radius = 5;
            string path = AssetDatabase.GenerateUniqueAssetPath($"{LevelsFolder}/HexaLevel_{NextLevelNumber():000}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            RefreshLevelList();
            SelectLevel(asset);
        }

        private void DuplicateSelected()
        {
            string src = AssetDatabase.GetAssetPath(_selected);
            string dst = AssetDatabase.GenerateUniqueAssetPath(src);
            AssetDatabase.CopyAsset(src, dst);
            AssetDatabase.SaveAssets();
            RefreshLevelList();
            SelectLevel(AssetDatabase.LoadAssetAtPath<HexaLevelData>(dst));
        }

        private void DeleteLevel(HexaLevelData lv)
        {
            if (!EditorUtility.DisplayDialog("Level Sil", $"'{lv.name}' silinsin mi?", "Sil", "Vazgeç")) return;
            SetInGameConfig(lv, false);
            AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(lv));
            if (_selected == lv) _selected = null;
            RefreshLevelList();
        }

        private int NextLevelNumber()
        {
            int max = 0;
            foreach (var lv in _levels)
            {
                var m = System.Text.RegularExpressions.Regex.Match(lv.name, @"\d+$");
                if (m.Success && int.TryParse(m.Value, out int n)) max = Mathf.Max(max, n);
            }
            return max + 1;
        }

        // ════════════════════════════════════════════════════════════════════
        //  GameConfig üyeliği (level başına checkbox)
        // ════════════════════════════════════════════════════════════════════

        private GameConfig _gameConfig;

        private GameConfig FindGameConfig()
        {
            if (_gameConfig == null)
                _gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            return _gameConfig;
        }

        private bool IsInGameConfig(HexaLevelData lv)
        {
            var cfg = FindGameConfig();
            return cfg != null && cfg.Levels != null && cfg.Levels.Contains(lv);
        }

        private void SetInGameConfig(HexaLevelData lv, bool include)
        {
            var cfg = FindGameConfig();
            if (cfg == null) return;
            var so = new SerializedObject(cfg);
            var prop = so.FindProperty("_levels");

            var list = new List<Object>();
            for (int i = 0; i < prop.arraySize; i++)
            {
                var v = prop.GetArrayElementAtIndex(i).objectReferenceValue;
                if (v != null && v != lv) list.Add(v);
            }
            if (include) list.Add(lv);

            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(cfg);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Veri yardımcıları — _selected.Cells / _selected.Arrows üzerinde
        // ════════════════════════════════════════════════════════════════════

        private HexaCellSave CellAt(int q, int r)
        {
            var cells = _selected.Cells;
            for (int i = 0; i < cells.Length; i++)
                if (cells[i].Q == q && cells[i].R == r) return cells[i];
            return null;
        }

        private bool IsCellEmpty((int q, int r) p) => CellAt(p.q, p.r) == null;

        private bool InRegion((int q, int r) p) => HexCoord.InRegion(p.q, p.r, _selected.Radius);

        private List<int> CellIndicesOfArrow(int arrowId)
        {
            var result = new List<int>();
            for (int i = 0; i < _selected.Cells.Length; i++)
                if (_selected.Cells[i].ArrowId == arrowId) result.Add(i);
            return result;
        }

        /// <summary>Oku tümüyle siler; arrowId'ler yoğun (0..n-1) kalacak şekilde yeniden numaralanır.</summary>
        private void RemoveArrow(int arrowId)
        {
            Record("Erase Arrow");
            _selected.Cells = _selected.Cells.Where(c => c.ArrowId != arrowId).ToArray();
            foreach (var c in _selected.Cells)
                if (c.ArrowId > arrowId) c.ArrowId--;

            var arrows = _selected.Arrows.ToList();
            arrows.RemoveAt(arrowId);
            _selected.Arrows = arrows.ToArray();
            Dirty();
        }

        private void Record(string op) => Undo.RecordObject(_selected, op);
        private void Dirty() => EditorUtility.SetDirty(_selected);

        private static GUIStyle _selectedStyle;
        private static GUIStyle SelectedStyle()
        {
            if (_selectedStyle == null)
            {
                _selectedStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.cyan },
                };
            }
            return _selectedStyle;
        }

        private static GUIStyle _centeredGray;
        private static GUIStyle CenteredGray()
        {
            if (_centeredGray == null)
            {
                _centeredGray = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.gray },
                };
            }
            return _centeredGray;
        }
    }
}

using System.Collections.Generic;
using ArrowRotate.Core;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Tahtanın görsel temsili: her segment taşıyan hücre için taş + segment.
    /// Koordinat dönüşümü tek yerde (HexMetrics üzerinden) — dokunma seçimi raycast'siz.
    /// </summary>
    public enum HexaViewMode
    {
        Flat2D,   // prosedürel düz hex mesh (prototip görünümü)
        Depth3D,  // hexagon.fbx puck taşlar + ışık; oklar düz sprite kalır
        Shapes2D  // Shapes kütüphanesi: yuvarlatılmış RegularPolygon taşlar + Polyline oklar
    }

    public class BoardView : MonoBehaviour
    {
        public const float TileZ = 0f;
        public const float SegmentZ = -0.1f;
        public const float OverlayZ = -0.2f;

        public float CellSize = 1f; // S (hex köşe yarıçapı, dünya birimi)

        [Header("Görünüm Modu")]
        public HexaViewMode ViewMode = HexaViewMode.Flat2D;
        [Tooltip("Depth3D modda taş modeli (hexagon.fbx)")]
        public GameObject TileModel3D;

        [Header("Renk & Tema")]
        [Tooltip("Aktif tema (boşsa Resources/Themes/Theme_Default). Build'de HexaThemeData.Active olur.")]
        public HexaThemeData Theme;

        [Header("3D Kamera / Işık (prefab'lardan okunur — düzenle, yansır)")]
        [Tooltip("Board Light Prefab 2 — olduğu gibi instantiate edilir (açı/şiddet/renk prefab'dan). Boşsa koddan varsayılan ışık.")]
        public GameObject LightPrefab;
        [Tooltip("Camera Prefab — tilt (X) ve arka plan rengi buradan okunur. Boşsa aşağıdaki değerler.")]
        public GameObject CameraPrefab;
        [Tooltip("CameraPrefab boşsa kullanılacak X eğimi (derece)")]
        public float CameraTiltXDeg = -27.4f;
        [Tooltip("CameraPrefab boşsa kullanılacak arka plan rengi")]
        public Color CameraBackground = new Color(0x9B / 255f, 0xA9 / 255f, 0xD1 / 255f); // #9BA9D1

        private readonly Dictionary<(int q, int r), TileView> _tiles = new Dictionary<(int, int), TileView>();
        private readonly Dictionary<(int q, int r), SegmentView> _segments = new Dictionary<(int, int), SegmentView>();
        private readonly Dictionary<int, IceView> _ices = new Dictionary<int, IceView>();
        private HexaLevel _level;

        public void Build(HexaLevel level)
        {
            Clear();
            _level = level;

            if (Theme != null) HexaThemeData.Active = Theme; // inspector'daki tema aktif olur

            bool use3D = ViewMode == HexaViewMode.Depth3D && TileModel3D != null;
            bool useShapes = ViewMode == HexaViewMode.Shapes2D;
            if (use3D) Setup3DLighting();

            foreach (var arrow in level.Arrows)
            {
                var color = HexaPalette.ForPalette(arrow.Palette);
                foreach (var pos in arrow.Cells)
                {
                    var cell = level.GetCell(pos);
                    var (x, y) = HexMetrics.Center(cell.Q, cell.R, CellSize);
                    _tiles[pos] = use3D
                        ? TileView.Create3D(transform, new Vector3(x, y, TileZ), CellSize, color, TileModel3D)
                        : useShapes
                            ? TileView.CreateShapes(transform, new Vector3(x, y, TileZ), CellSize, color)
                            : TileView.Create(transform, new Vector3(x, y, TileZ), CellSize, color);
                    _segments[pos] = SegmentView.Create(transform, new Vector3(x, y, SegmentZ), CellSize, cell, useShapes);
                }

                if (arrow.FreezeAt > 0)
                    _ices[arrow.ArrowId] = IceView.Create(transform, level, arrow, CellSize);
            }
        }

        /// <summary>
        /// Aktif temayı yeniden uygular — runtime'da (Güncelle butonu / ayarlar menüsü) çağrılır.
        /// Gölge renkleri ve kamera arka planı canlı güncellenir. NOT: taş renkleri HexaColorDatabase'ten
        /// (palet), buz renkleri ise oluşturulurken sabitlenir — onlar için rebuild (Begin) gerekir.
        /// </summary>
        public void RefreshTheme()
        {
            if (Theme != null) HexaThemeData.Active = Theme;
            var theme = HexaThemeData.Active;

            if (ViewMode != HexaViewMode.Depth3D)
            {
                var cam = Camera.main;
                if (cam != null) cam.backgroundColor = theme.CameraBackground;
            }

            foreach (var seg in _segments.Values)
                if (seg != null) seg.SetShadowColor(theme.SegmentShadow);
        }

        /// <summary>Depth3D: ışığı LightPrefab'dan instantiate eder (açı/şiddet/renk prefab'dan) + düz ambient.</summary>
        private void Setup3DLighting()
        {
            if (LightPrefab != null)
            {
                // prefab'ın kendi rotasyonu korunur (directional için tek önemli olan yön)
                Instantiate(LightPrefab, transform);
            }
            else
            {
                var go = new GameObject("Board Light");
                go.transform.SetParent(transform, false);
                var light = go.AddComponent<Light>();
                light.type = LightType.Directional;
                go.transform.rotation = Quaternion.LookRotation(new Vector3(0.3f, -0.45f, 1f));
                light.intensity = 0.85f;
                light.shadows = LightShadows.None;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.25f, 0.26f, 0.30f); // yan yüzler koyu kalır, renkler doygun
        }

        public void Clear()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            _tiles.Clear();
            _segments.Clear();
            _ices.Clear();
        }

        // ── buz (SKILL.md §5) ──────────────────────────────────────────────────
        public void ShakeIce(int arrowId)
        {
            if (_ices.TryGetValue(arrowId, out var ice) && ice != null) ice.Shake();
        }

        public void BreakIce(int arrowId)
        {
            if (_ices.TryGetValue(arrowId, out var ice) && ice != null)
            {
                ice.Break();
                _ices.Remove(arrowId);
            }
        }

        public void UpdateIceBadges(int exitedCount)
        {
            if (_level == null) return;
            foreach (var kv in _ices)
            {
                var arrow = _level.Arrows[kv.Key];
                if (kv.Value != null) kv.Value.SetRemaining(arrow.FreezeAt - exitedCount);
            }
        }

        public TileView GetTile((int q, int r) pos) => _tiles.TryGetValue(pos, out var t) ? t : null;
        public SegmentView GetSegment((int q, int r) pos) => _segments.TryGetValue(pos, out var sv) ? sv : null;

        public (int q, int r) WorldToAxial(Vector3 world)
            => HexMetrics.WorldToAxial(world.x, world.y, CellSize);

        public void RemoveCellVisuals((int q, int r) pos)
        {
            if (_tiles.TryGetValue(pos, out var t)) { if (t != null) Destroy(t.gameObject); _tiles.Remove(pos); }
            if (_segments.TryGetValue(pos, out var sv)) { if (sv != null) Destroy(sv.gameObject); _segments.Remove(pos); }
        }

        /// <summary>Kamerayı tahta bbox'ına oturtur (dikey telefon güvenli alan payıyla).</summary>
        public void FitCamera(Camera cam, float padding = 1.5f)
        {
            if (_level == null || _level.Cells.Count == 0) return;

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var cell in _level.Cells.Values)
            {
                var (x, y) = HexMetrics.Center(cell.Q, cell.R, CellSize);
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            minX -= CellSize; maxX += CellSize;
            minY -= CellSize; maxY += CellSize;

            var boardCenter = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);

            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;

            if (ViewMode == HexaViewMode.Depth3D)
            {
                // tilt + arka plan CameraPrefab'dan okunur (prefab düzenle → oyuna yansır); yoksa inline değerler
                Quaternion rot;
                if (CameraPrefab != null)
                {
                    rot = CameraPrefab.transform.rotation;
                    var pc = CameraPrefab.GetComponent<Camera>();
                    cam.backgroundColor = pc != null ? pc.backgroundColor : CameraBackground;
                }
                else
                {
                    rot = Quaternion.Euler(CameraTiltXDeg, 0f, 0f);
                    cam.backgroundColor = CameraBackground;
                }
                cam.transform.rotation = rot;
                // board merkezini bakış hattı üzerinde kadraja al (kadraj/zoom otomatik — level boyutu değişir)
                cam.transform.position = boardCenter - rot * Vector3.forward * 12f;
            }
            else
            {
                cam.transform.rotation = Quaternion.identity;
                cam.transform.position = boardCenter + Vector3.back * 10f;
                cam.backgroundColor = HexaPalette.Background;
            }

            float halfH = (maxY - minY) * 0.5f + padding;
            float halfW = (maxX - minX) * 0.5f + padding;
            cam.orthographicSize = Mathf.Max(halfH, halfW / cam.aspect);
        }
    }
}

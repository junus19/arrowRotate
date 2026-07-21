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

        [Header("Depth3D XZ (taş mesh EP'den; segmentler prosedürel tek parça)")]
        [Tooltip("Hexagon taş mesh (XZ/Y-up, köşe yarıçapı ~1)")]
        public Mesh HexMesh3D;
        [Tooltip("Taş materyali — YEDEK. Materyaller artık öncelikle HexaColorDatabase'ten okunur; DB boşsa buraya, o da boşsa Lit3DTransparent'a düşer.")]
        public Material TileMaterial3D;
        [Tooltip("Ok materyali — YEDEK. Öncelik HexaColorDatabase.SegmentMaterial; boşsa buraya, o da boşsa Lit3DTransparent.")]
        public Material ArrowMaterial3D;
        [Tooltip("Taşlar arası BOŞLUK (S oranı). BÜYÜK değer = büyük boşluk. Footprint = 1 − TileGap. Hücre aralığı sabit (segment bağlantıları etkilenmez); taşları küçültüp aralarında görsel gap açar (örtüşme/z-fight'ı önler). 0 = mesh'in doğal boşluğu, 0.1 belirgin boşluk.")]
        [Range(0f, 0.6f)] public float TileGap = 0f;
        [Tooltip("Taş kalınlık (Y) çarpanı — puck yüksekliği. Mesh merkez pivotlu, hem üste hem alta büyür. 1 = mesh'in doğal kalınlığı, 1.5 = %50 kalın.")]
        public float TileThicknessY = 1f;
        [Tooltip("Segment/ok'u hexagon üst yüzeyinin ne kadar ALTINA gömer (mesh birimi × S). 0 = yüzeyde; ~0.12 hafif gömülü. Segment yüksekliği ≈0.30.")]
        public float SegmentSink = 0.12f;
        [Tooltip("Segment/ok'u taş üstünde EKSTRA aşağı alan dünya-Y offset'i (× S). Kalınlık ince ayarı için. 0 = kapalı.")]
        public float SegmentDropY = 0f;
        [Tooltip("XZ'de gölge: ışığı Soft gölgeye zorlar + taş/segmentleri caster yapar. Zemine (altındaki plane) gölge düşmesi için AÇIK olmalı. Not: taş materyali Transparent'sa gölge atmaz — opak master material kullan.")]
        public bool CastShadows3D = true;

        [Header("Renk & Tema")]
        [Tooltip("Aktif tema (boşsa Resources/Themes/Theme_Default). Build'de HexaThemeData.Active olur.")]
        public HexaThemeData Theme;

        [Header("3D Kamera / Işık (prefab'lardan okunur — düzenle, yansır)")]
        [Tooltip("Ana yönlü ışık (Board Light Prefab 2) — olduğu gibi instantiate edilir (açı/şiddet/renk prefab'dan). Gölge kaynağı budur. Boşsa koddan varsayılan ışık.")]
        public GameObject LightPrefab;
        [Tooltip("Dolgu/helper ışık (helperLight) — olduğu gibi instantiate edilir. Gölge atmaz (dolgu), CastShadows3D ONU ETKİLEMEZ. Boşsa eklenmez.")]
        public GameObject HelperLightPrefab;
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

            bool xz = ViewMode == HexaViewMode.Depth3D && HexMesh3D != null; // EP mesh'li gerçek XZ
            bool useFbx3D = ViewMode == HexaViewMode.Depth3D && !xz && TileModel3D != null; // eski fbx (fallback)
            bool useShapes = ViewMode == HexaViewMode.Shapes2D;
            if (xz || useFbx3D) Setup3DLighting();

            var db = HexaColorDatabase.Active;
            // Ok/segment materyali: DB > BoardView yedeği > Lit3DTransparent
            var segMat = db.SegmentMaterial != null ? db.SegmentMaterial
                       : (ArrowMaterial3D != null ? ArrowMaterial3D : MeshFactory.Lit3DTransparent);

            foreach (var arrow in level.Arrows)
            {
                var color = HexaPalette.ForPalette(arrow.Palette);
                foreach (var pos in arrow.Cells)
                {
                    var cell = level.GetCell(pos);
                    var (x, y) = HexMetrics.Center(cell.Q, cell.R, CellSize);
                    if (xz)
                    {
                        // Taş materyali: DB (master/per-color) > BoardView yedeği (Create3DXZ null'da Lit3DTransparent'a düşer)
                        var tileMat = db.HexMaterialForPalette(arrow.Palette) ?? TileMaterial3D;
                        float tileFootprint = Mathf.Clamp(1f - TileGap, 0.2f, 2f); // boşluk arttıkça taş küçülür
                        var tv = TileView.Create3DXZ(transform, new Vector3(x, 0f, y), CellSize, color, HexMesh3D, tileMat, tileFootprint, TileThicknessY);
                        var sv = SegmentView.Create3DXZ(transform, new Vector3(x, SurfaceY, y), CellSize, cell, segMat);
                        if (CastShadows3D) { tv.SetCastShadows(true); sv.SetCastShadows(true); }
                        _tiles[pos] = tv;
                        _segments[pos] = sv;
                    }
                    else if (useFbx3D)
                    {
                        _tiles[pos] = TileView.Create3D(transform, new Vector3(x, y, TileZ), CellSize, color, TileModel3D);
                        _segments[pos] = SegmentView.Create(transform, new Vector3(x, y, SegmentZ), CellSize, cell, false);
                    }
                    else
                    {
                        _tiles[pos] = useShapes
                            ? TileView.CreateShapes(transform, new Vector3(x, y, TileZ), CellSize, color)
                            : TileView.Create(transform, new Vector3(x, y, TileZ), CellSize, color);
                        _segments[pos] = SegmentView.Create(transform, new Vector3(x, y, SegmentZ), CellSize, cell, useShapes);
                    }
                }

                if (arrow.FreezeAt > 0 && !xz) // buz: XZ'de sonra ele alınacak
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

            // XZ: materyalleri DB'den canlı yeniden ata (master/per-color/segment değiştir → level'ı yeniden yüklemeden dene)
            if (Is3DXZ && _level != null)
            {
                var db = HexaColorDatabase.Active;
                var segMat = db.SegmentMaterial != null ? db.SegmentMaterial
                           : (ArrowMaterial3D != null ? ArrowMaterial3D : MeshFactory.Lit3DTransparent);
                foreach (var arrow in _level.Arrows)
                {
                    var tileMat = db.HexMaterialForPalette(arrow.Palette) ?? TileMaterial3D;
                    foreach (var pos in arrow.Cells)
                    {
                        if (_tiles.TryGetValue(pos, out var t) && t != null) t.SetMaterial(tileMat);
                        if (_segments.TryGetValue(pos, out var sv) && sv != null) sv.SetMaterial(segMat);
                    }
                }
            }
        }

        /// <summary>Depth3D: ışığı LightPrefab'dan instantiate eder (açı/şiddet/renk prefab'dan) + düz ambient.</summary>
        private void Setup3DLighting()
        {
            Light light;
            if (LightPrefab != null)
            {
                // prefab'ın kendi rotasyonu korunur (directional için tek önemli olan yön)
                light = Instantiate(LightPrefab, transform).GetComponentInChildren<Light>();
            }
            else
            {
                var go = new GameObject("Board Light");
                go.transform.SetParent(transform, false);
                light = go.AddComponent<Light>();
                light.type = LightType.Directional;
                go.transform.rotation = Quaternion.LookRotation(new Vector3(0.3f, -0.45f, 1f));
                light.intensity = 0.85f;
                light.shadows = LightShadows.None;
            }

            // Zemine gölge düşmesi için ana ışık gölge atmalı — prefab'da kapalıysa da CastShadows3D açıkken zorla
            if (CastShadows3D && light != null && light.shadows == LightShadows.None)
                light.shadows = LightShadows.Soft;

            // dolgu/helper ışık — olduğu gibi eklenir, gölge atmaz (kasıtlı, ışığına dokunulmaz)
            if (HelperLightPrefab != null) Instantiate(HelperLightPrefab, transform);

            // environment lighting: kaynak Skybox, intensity multiplier 1.1 (kullanıcı kararı 2026-07-21)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.1f;
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

        public bool Is3DXZ => ViewMode == HexaViewMode.Depth3D && HexMesh3D != null;

        /// <summary>XZ modda segment/ok tabanının oturduğu yükseklik — KALINLAŞAN taş üst yüzeyini (bounds.max.y·TileThicknessY)
        /// takip eder, SegmentSink kadar gömülür, üstüne SegmentDropY kadar ekstra aşağı alınır.
        /// Uçuş animasyonu (FlightRenderer3D) da buraya oturur → tille tutarlı.</summary>
        public float SurfaceY => HexMesh3D != null
            ? (HexMesh3D.bounds.max.y * TileThicknessY - 0.005f - SegmentSink - SegmentDropY) * CellSize
            : 0f;

        public (int q, int r) WorldToAxial(Vector3 world)
            => Is3DXZ
                ? HexMetrics.WorldToAxial(world.x, world.z, CellSize)   // XZ: planarY = world Z
                : HexMetrics.WorldToAxial(world.x, world.y, CellSize);

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

            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;

            bool xz = ViewMode == HexaViewMode.Depth3D && HexMesh3D != null;
            if (xz)
            {
                // XZ düzlemi: board yatık (world Y=0, planarY→world Z), kamera yukarıdan eğik bakar
                var boardCenterXZ = new Vector3((minX + maxX) * 0.5f, 0f, (minY + maxY) * 0.5f);
                float tilt = CameraPrefab != null
                    ? Mathf.Abs(CameraPrefab.transform.eulerAngles.x > 180 ? CameraPrefab.transform.eulerAngles.x - 360 : CameraPrefab.transform.eulerAngles.x)
                    : 55f;
                if (tilt < 20f) tilt = 55f; // prefab XY-tilt değeri XZ'ye uygun değilse varsayılan
                var rot = Quaternion.Euler(tilt, 0f, 0f);
                cam.transform.rotation = rot;
                cam.transform.position = boardCenterXZ - rot * Vector3.forward * 25f;
                cam.backgroundColor = CameraBackground;

                float xzHalfW = (maxX - minX) * 0.5f + padding;
                float xzHalfDepth = (maxY - minY) * 0.5f + padding;
                // eğik bakışta derinlik ekseni cos(tilt) ile kısalır; dikey kaplama buna göre
                float xzHalfV = Mathf.Max(xzHalfDepth * Mathf.Cos(tilt * Mathf.Deg2Rad) + 0.5f, xzHalfW / cam.aspect);
                cam.orthographicSize = xzHalfV;
                return;
            }

            var boardCenter = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);

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

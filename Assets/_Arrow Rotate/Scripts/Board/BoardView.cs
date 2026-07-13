using System.Collections.Generic;
using ArrowRotate.Core;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Tahtanın görsel temsili: her segment taşıyan hücre için taş + segment.
    /// Koordinat dönüşümü tek yerde (HexMetrics üzerinden) — dokunma seçimi raycast'siz.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        public const float TileZ = 0f;
        public const float SegmentZ = -0.1f;
        public const float OverlayZ = -0.2f;

        public float CellSize = 1f; // S (hex köşe yarıçapı, dünya birimi)

        private readonly Dictionary<(int q, int r), TileView> _tiles = new Dictionary<(int, int), TileView>();
        private readonly Dictionary<(int q, int r), SegmentView> _segments = new Dictionary<(int, int), SegmentView>();
        private readonly Dictionary<int, IceView> _ices = new Dictionary<int, IceView>();
        private HexaLevel _level;

        public void Build(HexaLevel level)
        {
            Clear();
            _level = level;

            foreach (var arrow in level.Arrows)
            {
                var color = HexaPalette.ForPalette(arrow.Palette);
                foreach (var pos in arrow.Cells)
                {
                    var cell = level.GetCell(pos);
                    var (x, y) = HexMetrics.Center(cell.Q, cell.R, CellSize);
                    _tiles[pos] = TileView.Create(transform, new Vector3(x, y, TileZ), CellSize, color);
                    _segments[pos] = SegmentView.Create(transform, new Vector3(x, y, SegmentZ), CellSize, cell);
                }

                if (arrow.FreezeAt > 0)
                    _ices[arrow.ArrowId] = IceView.Create(transform, level, arrow, CellSize);
            }
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

            var center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, -10f);
            cam.transform.position = center;
            cam.orthographic = true;
            cam.backgroundColor = HexaPalette.Background;
            cam.clearFlags = CameraClearFlags.SolidColor;

            float halfH = (maxY - minY) * 0.5f + padding;
            float halfW = (maxX - minX) * 0.5f + padding;
            cam.orthographicSize = Mathf.Max(halfH, halfW / cam.aspect);
        }
    }
}

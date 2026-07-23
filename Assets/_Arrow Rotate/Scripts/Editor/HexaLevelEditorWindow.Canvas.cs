using System.Collections.Generic;
using System.Linq;
using ArrowRotate.Core;
using ArrowRotate.Integration;
using UnityEditor;
using UnityEngine;

namespace ArrowRotate.EditorTools
{
    // ════════════════════════════════════════════════════════════════════════
    //  ORTA — hex canvas: çizim + input
    //  GUI'de y AŞAĞI bakar → prototipin SVG koordinatları BİREBİR kullanılır
    //  (runtime'daki y-aynalama BURADA YOKTUR): x=1.5·S·q, y=√3·S·(r+q/2), açı=30+60d.
    // ════════════════════════════════════════════════════════════════════════
    public partial class HexaLevelEditorWindow
    {
        private const float Sqrt3 = 1.7320508f;
        // Nested önizleme ölçeği — runtime BoardView.NestScale varsayılanıyla eşleşmeli (katman başına çarpan).
        private const float NestPreviewScale = 0.5f;

        private void DrawCanvasPanel()
        {
            var canvas = GUILayoutUtility.GetRect(100, 100000, 100, 100000,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(canvas, CanvasBg);

            if (_selected == null)
            {
                GUI.Label(canvas, "Soldan bir level seç veya oluştur.", CenteredGray());
                return;
            }

            int radius = Mathf.Max(1, _selected.Radius);
            ComputeLayout(canvas, radius, out float s, out Vector2 origin);

            HandleGridEvents(radius, s, origin);

            if (Event.current.type != EventType.Repaint) return;

            // ── Bölge (boş hücre noktaları + soluk hex) ──────────────────────
            for (int q = -radius; q <= radius; q++)
            for (int r = -radius; r <= radius; r++)
            {
                if (!HexCoord.InRegion(q, r, radius)) continue;
                var c = CenterGui(q, r, s, origin);
                DrawHexOutline(c, s * 0.94f, new Color(0.22f, 0.22f, 0.33f), 1.5f);
                if (CellAt(q, r) == null)
                {
                    Handles.color = EmptyDot;
                    Handles.DrawSolidDisc(c, Vector3.forward, s * 0.07f);
                }
            }

            // ── Taşlar + segmentler (katmanlar DERİNDEN yüzeye) ──────────────
            // aktif katman: tam renk + beyaz segment · aktiften derin: İÇTE küçük hexagon
            // (runtime Nested görselinin önizlemesi — gerçek renk, segment GİZLİ) ·
            // aktifi ÖRTEN üst katmanlar: yalnızca renkli kontur (altındaki aktif hücre okunsun)
            var moveSet = _moving ? new HashSet<int>(_moveCellIdx) : null;
            for (int layer = HexaLevel.MaxBuriedLayers; layer >= 0; layer--)
            {
                for (int i = 0; i < _selected.Cells.Length; i++)
                {
                    var cell = _selected.Cells[i];
                    if (cell.Layer != layer) continue;
                    if (moveSet != null && moveSet.Contains(i)) continue; // kaynak gizlenir
                    var center = CenterGui(cell.Q, cell.R, s, origin);
                    var palette = PaletteOf(cell.ArrowId);

                    if (layer > _activeLayer) // aktiften derin — İÇTE küçük hexagon (Nested önizleme)
                    {
                        float nf = Mathf.Pow(NestPreviewScale, layer - _activeLayer);
                        DrawHexFilled(center, s * 0.91f * nf, palette);               // gerçek palet rengi
                        DrawHexOutline(center, s * 0.91f * nf, new Color(0f, 0f, 0f, 0.35f), 1.5f); // ince kenar → yüzeyden ayrışsın
                        // segment GİZLİ (nested'de ok pasif) — çizilmez
                    }
                    else if (layer == _activeLayer)
                    {
                        DrawHexFilled(center, s * 0.91f, palette);
                        DrawPiece(center, s, cell, Color.white);
                    }
                    else // aktifi örten üst katman — kontur
                    {
                        DrawHexOutline(center, s * 0.78f, new Color(palette.r, palette.g, palette.b, 0.9f), 2.5f);
                    }
                }
            }

            // ── Buz katmanı (yalnızca aktif katman hücrelerinde) ─────────────
            for (int a = 0; a < _selected.Arrows.Length; a++)
            {
                if (_selected.Arrows[a].FreezeAt <= 0) continue;
                var idx = CellIndicesOfArrow(a);
                foreach (int i in idx)
                {
                    if (moveSet != null && moveSet.Contains(i)) continue;
                    var cell = _selected.Cells[i];
                    if (cell.Layer != _activeLayer) continue;
                    DrawHexFilled(CenterGui(cell.Q, cell.R, s, origin), s * 0.91f, IceTint);
                }
                if (idx.Count > 0)
                {
                    var mid = _selected.Cells[idx[idx.Count / 2]];
                    var mc = CenterGui(mid.Q, mid.R, s, origin);
                    Handles.color = new Color(0.85f, 0.93f, 1f);
                    Handles.DrawSolidDisc(mc, Vector3.forward, s * 0.32f);
                    var style = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.13f, 0.19f, 0.43f) },
                    };
                    GUI.Label(new Rect(mc.x - 12, mc.y - 9, 24, 18), _selected.Arrows[a].FreezeAt.ToString(), style);
                }
            }

            // ── Draw yolu önizlemesi ─────────────────────────────────────────
            if (_drawing && _drawPath.Count > 0)
            {
                var tint = ArrowRotate.View.HexaPalette.ForPalette(_paintPalette);
                tint.a = 0.4f;
                foreach (var p in _drawPath)
                    DrawHexFilled(CenterGui(p.q, p.r, s, origin), s * 0.91f, tint);
                Handles.color = Color.white;
                for (int i = 0; i + 1 < _drawPath.Count; i++)
                    Handles.DrawAAPolyLine(3f,
                        CenterGui(_drawPath[i].q, _drawPath[i].r, s, origin),
                        CenterGui(_drawPath[i + 1].q, _drawPath[i + 1].r, s, origin));
            }

            // ── Move hayaleti ────────────────────────────────────────────────
            if (_moving)
            {
                bool valid = MoveTargetValid();
                var tint = valid ? new Color(0.3f, 0.9f, 0.4f, 0.5f) : new Color(0.95f, 0.25f, 0.25f, 0.5f);
                foreach (int i in _moveCellIdx)
                {
                    var cell = _selected.Cells[i];
                    int tq = cell.Q + _moveOffset.q, tr = cell.R + _moveOffset.r;
                    var center = CenterGui(tq, tr, s, origin);
                    DrawHexFilled(center, s * 0.91f, tint);
                    DrawPiece(center, s, cell, Color.white);
                }
            }

            // ── Edit seçim vurgusu ───────────────────────────────────────────
            if (_tool == Tool.Edit && _editCell.q != int.MinValue)
                DrawHexOutline(CenterGui(_editCell.q, _editCell.r, s, origin), s * 0.94f, Color.white, 2.5f);
        }

        // ── Geometri ──────────────────────────────────────────────────────────

        private static void ComputeLayout(Rect canvas, int radius, out float s, out Vector2 origin)
        {
            // bölge bbox'ı (S=1): merkez uçları
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int q = -radius; q <= radius; q++)
            for (int r = -radius; r <= radius; r++)
            {
                if (!HexCoord.InRegion(q, r, radius)) continue;
                float x = 1.5f * q, y = Sqrt3 * (r + q * 0.5f);
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
            const float pad = 20f;
            s = Mathf.Min((canvas.width - pad * 2) / (maxX - minX + 2.2f),
                          (canvas.height - pad * 2) / (maxY - minY + 2.2f));
            s = Mathf.Clamp(s, 6f, 60f);
            var bboxCenter = new Vector2((minX + maxX) * 0.5f * s, (minY + maxY) * 0.5f * s);
            origin = canvas.center - bboxCenter;
        }

        private static Vector2 CenterGui(int q, int r, float s, Vector2 origin)
            => origin + new Vector2(1.5f * s * q, Sqrt3 * s * (r + q * 0.5f));

        private static Vector2 DirVecGui(int d)
        {
            float a = (30f + 60f * d) * Mathf.Deg2Rad; // prototip açısı (y-aşağı)
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        private static (int q, int r) GuiToAxial(Vector2 gui, float s, Vector2 origin)
        {
            float qf = (gui.x - origin.x) / (1.5f * s);
            float rf = (gui.y - origin.y) / (Sqrt3 * s) - qf * 0.5f;
            return HexMetrics.AxialRound(qf, rf);
        }

        private static void DrawHexFilled(Vector2 center, float radius, Color color)
        {
            var pts = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                pts[i] = new Vector3(center.x + radius * Mathf.Cos(a), center.y + radius * Mathf.Sin(a), 0f);
            }
            Handles.color = color;
            Handles.DrawAAConvexPolygon(pts);
        }

        private static void DrawHexOutline(Vector2 center, float radius, Color color, float width)
        {
            var pts = new Vector3[7];
            for (int i = 0; i <= 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                pts[i] = new Vector3(center.x + radius * Mathf.Cos(a), center.y + radius * Mathf.Sin(a), 0f);
            }
            Handles.color = color;
            Handles.DrawAAPolyLine(width, pts);
        }

        /// <summary>Segment çizimi — DÜNYA yönleriyle ((lokal+rot)%6); rot uygulanmış hali gösterir.</summary>
        private static void DrawPiece(Vector2 center, float s, HexaCellSave cell, Color color)
        {
            float apo = Sqrt3 * 0.5f * s;
            float lw = Mathf.Max(2f, s * 0.16f);
            Handles.color = color;

            int worldB = ((cell.B + cell.Rot) % 6 + 6) % 6;
            switch (cell.Type)
            {
                case CellType.Tail:
                {
                    Handles.DrawAAPolyLine(lw, center, center + DirVecGui(worldB) * apo);
                    Handles.DrawSolidDisc(center, Vector3.forward, s * 0.17f);
                    break;
                }
                case CellType.Mid:
                {
                    int worldA = ((cell.A + cell.Rot) % 6 + 6) % 6;
                    Handles.DrawAAPolyLine(lw,
                        center + DirVecGui(worldA) * apo, center, center + DirVecGui(worldB) * apo);
                    break;
                }
                case CellType.Head:
                {
                    int worldA = ((cell.A + cell.Rot) % 6 + 6) % 6;
                    var tip = center + DirVecGui(worldB) * (apo * 0.62f);
                    Handles.DrawAAPolyLine(lw, center + DirVecGui(worldA) * apo, center, tip);
                    var v = DirVecGui(worldB);
                    var perp = new Vector2(-v.y, v.x);
                    Handles.DrawAAConvexPolygon(
                        tip + v * s * 0.38f,
                        tip + perp * s * 0.24f,
                        tip - perp * s * 0.24f);
                    break;
                }
            }
        }

        private Color PaletteOf(int arrowId)
        {
            int palette = arrowId >= 0 && arrowId < _selected.Arrows.Length ? _selected.Arrows[arrowId].Palette : 0;
            return ArrowRotate.View.HexaPalette.ForPalette(palette);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Input
        // ════════════════════════════════════════════════════════════════════

        private void HandleGridEvents(int radius, float s, Vector2 origin)
        {
            var e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            var cell = GuiToAxial(e.mousePosition, s, origin);
            bool inGrid = HexCoord.InRegion(cell.q, cell.r, radius);

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown when inGrid && e.button == 1:
                    EraseArrowAt(cell); // sağ tık her araçta siler
                    e.Use(); Repaint();
                    GUIUtility.ExitGUI();
                    break;

                case EventType.MouseDown when inGrid && e.button == 0:
                    GUIUtility.hotControl = id;
                    ToolDown(cell);
                    e.Use(); Repaint();
                    GUIUtility.ExitGUI();
                    break;

                case EventType.MouseDrag when e.button == 0 && GUIUtility.hotControl == id:
                    if (inGrid) ToolDrag(cell);
                    e.Use(); Repaint();
                    break;

                case EventType.MouseUp when e.button == 0 && GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    ToolUp();
                    e.Use(); Repaint();
                    GUIUtility.ExitGUI();
                    break;
            }
        }

        private void ToolDown((int q, int r) cell)
        {
            switch (_tool)
            {
                case Tool.Draw:
                    if (IsCellEmpty(cell))
                    {
                        _drawing = true;
                        _drawPath.Clear();
                        _drawPath.Add(cell);
                    }
                    break;
                case Tool.Erase:
                    _erasing = true;
                    EraseArrowAt(cell);
                    break;
                case Tool.Move:
                    if (!IsCellEmpty(cell)) BeginMove(cell);
                    break;
                case Tool.Rotate:
                    RotateCellAt(cell);
                    break;
                case Tool.Recolor:
                    RecolorArrowAt(cell);
                    break;
                case Tool.Ice:
                    CycleIceAt(cell);
                    break;
                case Tool.Edit:
                    if (_editCell == cell) RotateHeadFlightDir(cell);
                    _editCell = cell;
                    break;
            }
        }

        private void ToolDrag((int q, int r) cell)
        {
            switch (_tool)
            {
                case Tool.Draw when _drawing:
                    ExtendDrawPath(cell);
                    break;
                case Tool.Move when _moving:
                    _moveOffset = (cell.q - _moveAnchor.q, cell.r - _moveAnchor.r);
                    break;
                case Tool.Erase when _erasing:
                    EraseArrowAt(cell);
                    break;
                case Tool.Recolor:
                    RecolorArrowAt(cell);
                    break;
            }
        }

        private void ToolUp()
        {
            if (_drawing)
            {
                CommitDrawPath();
                _drawing = false;
                _drawPath.Clear();
            }
            if (_moving)
            {
                CommitMove();
                _moving = false;
            }
            _erasing = false;
        }

        private void CancelDrag()
        {
            _drawing = _moving = _erasing = false;
            _drawPath.Clear();
            _moveCellIdx.Clear();
            _moveArrowId = -1;
        }

        // ── Draw aracı: kuyruk → head ─────────────────────────────────────────

        private static int CubeDist((int q, int r) a, (int q, int r) b)
        {
            int dq = a.q - b.q, dr = a.r - b.r;
            return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(dq + dr)) / 2;
        }

        private void ExtendDrawPath((int q, int r) target)
        {
            // Hedefe hex-komşu adımlarla yürü: hızlı sürüklemede hücre atlanmaz.
            int guard = 128;
            while (_drawPath[_drawPath.Count - 1] != target && guard-- > 0)
            {
                var last = _drawPath[_drawPath.Count - 1];
                (int q, int r) best = last;
                int bestDist = int.MaxValue;
                for (int d = 0; d < 6; d++)
                {
                    var nb = HexCoord.Neighbor(last, d);
                    int dist = CubeDist(nb, target);
                    if (dist < bestDist) { bestDist = dist; best = nb; }
                }
                if (best == last) break;

                // geri adım: önceki yol hücresine dönmek son hücreyi siler
                if (_drawPath.Count >= 2 && best == _drawPath[_drawPath.Count - 2])
                {
                    _drawPath.RemoveAt(_drawPath.Count - 1);
                    continue;
                }
                if (!InRegion(best) || !IsCellEmpty(best) || _drawPath.Contains(best)) break;
                _drawPath.Add(best);
            }
        }

        private void CommitDrawPath()
        {
            if (_selected == null || _drawPath.Count < 2) return; // ok en az 2 hücre (tail+head)

            Record("Draw Arrow");
            int arrowId = _selected.Arrows.Length;
            int n = _drawPath.Count;
            var newCells = new List<HexaCellSave>(n);

            for (int i = 0; i < n; i++)
            {
                var p = _drawPath[i];
                var save = new HexaCellSave { Q = p.q, R = p.r, ArrowId = arrowId, Rot = 0, Layer = _activeLayer };
                if (i == 0) // kuyruk — sürükleme başı
                {
                    save.Type = CellType.Tail;
                    save.A = -1;
                    save.B = HexCoord.DirBetween(p, _drawPath[1]);
                }
                else if (i == n - 1) // head — bırakılan hücre; uçuş yönü düz devam
                {
                    save.Type = CellType.Head;
                    save.A = HexCoord.DirBetween(p, _drawPath[i - 1]);
                    save.B = HexCoord.Opp(save.A);
                }
                else
                {
                    save.Type = CellType.Mid;
                    save.A = HexCoord.DirBetween(p, _drawPath[i - 1]);
                    save.B = HexCoord.DirBetween(p, _drawPath[i + 1]);
                }
                newCells.Add(save);
            }

            _selected.Cells = _selected.Cells.Concat(newCells).ToArray();
            _selected.Arrows = _selected.Arrows
                .Concat(new[] { new HexaArrowSave { Palette = _paintPalette, FreezeAt = 0 } }).ToArray();
            Dirty();
        }

        // ── Move aracı: tüm ok taşınır ────────────────────────────────────────

        private void BeginMove((int q, int r) cell)
        {
            var src = CellAt(cell.q, cell.r);
            if (src == null) return;
            _moveArrowId = src.ArrowId;
            _moveCellIdx.Clear();
            _moveCellIdx.AddRange(CellIndicesOfArrow(src.ArrowId));
            _moveAnchor = cell;
            _moveOffset = (0, 0);
            _moving = true;
        }

        private bool MoveTargetValid()
        {
            foreach (int i in _moveCellIdx)
            {
                var c = _selected.Cells[i];
                var t = (c.Q + _moveOffset.q, c.R + _moveOffset.r);
                if (!InRegion(t)) return false;
                var occupied = CellAt(t.Item1, t.Item2, c.Layer); // hücrenin KENDİ katmanında çakışma
                if (occupied != null && occupied.ArrowId != _moveArrowId) return false;
            }
            return true;
        }

        private void CommitMove()
        {
            if ((_moveOffset.q != 0 || _moveOffset.r != 0) && MoveTargetValid())
            {
                Record("Move Arrow");
                foreach (int i in _moveCellIdx)
                {
                    _selected.Cells[i].Q += _moveOffset.q;
                    _selected.Cells[i].R += _moveOffset.r;
                }
                Dirty();
            }
            _moveCellIdx.Clear();
            _moveArrowId = -1;
        }

        // ── Diğer araçlar ─────────────────────────────────────────────────────

        private void EraseArrowAt((int q, int r) cell)
        {
            var src = CellAt(cell.q, cell.r);
            if (src != null) RemoveArrow(src.ArrowId);
        }

        private void RotateCellAt((int q, int r) cell)
        {
            var src = CellAt(cell.q, cell.r);
            if (src == null) return;
            Record("Rotate Cell");
            src.Rot = (src.Rot + 1) % 6;
            Dirty();
        }

        private void RecolorArrowAt((int q, int r) cell)
        {
            var src = CellAt(cell.q, cell.r);
            if (src == null || _selected.Arrows[src.ArrowId].Palette == _paintPalette) return;
            Record("Recolor Arrow");
            _selected.Arrows[src.ArrowId].Palette = _paintPalette;
            Dirty();
        }

        private void CycleIceAt((int q, int r) cell)
        {
            var src = CellAt(cell.q, cell.r);
            if (src == null) return;
            Record("Cycle Ice");
            var arrow = _selected.Arrows[src.ArrowId];
            arrow.FreezeAt = (arrow.FreezeAt + 1) % 4; // 0 → 1 → 2 → 3 → 0
            Dirty();
        }

        /// <summary>Edit aracında seçili HEAD'e ikinci tıklama: uçuş yönü (lokal B) bir sonraki yöne döner.</summary>
        private void RotateHeadFlightDir((int q, int r) cell)
        {
            var src = CellAt(cell.q, cell.r);
            if (src == null || src.Type != CellType.Head) return;
            Record("Rotate Flight Dir");
            src.B = (src.B + 1) % 6;
            if (src.B == src.A) src.B = (src.B + 1) % 6; // uçuş yönü girişle aynı olamaz
            Dirty();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using ArrowRotate.Core;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Hücre üstündeki BEYAZ segment görseli. Lokal a/b yönlerinde çizilir; rot, RotRoot'un
    /// z-dönüşüyle uygulanır (tap başına ekranda saat yönü 60° = Unity z'de -60°).
    /// Mid daima MERKEZDEN kırık çizgi — kenar ortalarını düz bağlamak yasak (SKILL.md tuzağı).
    /// Dönüş: ~160ms hafif overshoot; ardışık taplar hedef açıda birikir (debounce yok).
    /// </summary>
    public class SegmentView : MonoBehaviour
    {
        public const float RotateDuration = 0.16f;

        private Transform _rotRoot;
        private float _targetAngle;
        private float _currentAngle; // birikimli sürekli açı — localEulerAngles'ın 0..360 sarmasından bağımsız
        private Coroutine _anim;
        // Dönüş ekseni/adımı: 2D = Z ekseni, -60°/tap · Depth3D XZ = Y ekseni, +60°/tap
        private Vector3 _rotAxis = Vector3.forward;
        private float _stepDeg = -60f;
        private MeshRenderer _meshRenderer3D; // XZ segment mesh renderer (canlı materyal değişimi için)

        public static SegmentView Create(Transform parent, Vector3 pos, float s, Cell cell, bool useShapes = false)
        {
            var go = new GameObject("Segment");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var view = go.AddComponent<SegmentView>();

            var rot = new GameObject("Rot");
            rot.transform.SetParent(go.transform, false);
            view._rotRoot = rot.transform;

            if (useShapes) view.BuildShapeShapes(s, cell);
            else view.BuildShape(s, cell);
            view.SetInstantRot(cell.Rot);
            return view;
        }

        // Gölge (SKILL dışı görsel tercih): beyaz segmentin altına biraz büyük, gri kopya.
        // Transform büyütmek yerine her eleman YERİNDE büyütülür — çizgi uçları kenardan taşmaz.
        // Renk temadan gelir (HexaThemeData.SegmentShadow); runtime'da SetShadowColor ile değişir.
        public static bool DrawShadow = true;          // gölge açık (head düzeltmesi sonrası geri açıldı)
        private const float ShadowThicknessMul = 1.6f; // çizgi kalınlığı (uzamaz, sadece kalınlaşır)
        private const float ShadowSizeMul = 1.28f;     // nokta/üçgen yerinde büyüme
        private const float ShadowZBehind = 0.03f;     // main'in biraz arkasında (taşın önünde kalır)

        private GameObject _shadow; // runtime renk güncellemesi için

        private Transform _shapesParent; // Shapes elemanları buraya eklenir (Main kabı)

        // ── Shapes kütüphanesi ile çizim (Polyline + Triangle + Disc) ─────────────
        private void BuildShapeShapes(float s, Cell cell)
        {
            float apo = HexMetrics.Apothem(s);
            float lw = 0.153f * s;
            Vector3 EdgePoint(int d)
            {
                float a = HexMetrics.DirAngleDeg(d) * Mathf.Deg2Rad;
                return new Vector3(apo * Mathf.Cos(a), apo * Mathf.Sin(a), 0f);
            }

            // Beyaz segmenti "Main" kabına çiz
            var main = new GameObject("Main");
            main.transform.SetParent(_rotRoot, false);
            _shapesParent = main.transform;

            switch (cell.Type)
            {
                case CellType.Tail:
                    AddPolyline(lw, Vector3.zero, EdgePoint(cell.B));
                    AddDot(0.176f * s, Vector3.zero);
                    break;
                case CellType.Mid:
                    AddPolyline(lw, EdgePoint(cell.A), Vector3.zero, EdgePoint(cell.B));
                    break;
                case CellType.Head:
                {
                    // Prototip yapısı (reference §segLocalDrawing 'head'): giriş kenarı → merkez → B yönünde uç,
                    // ok ucu o uçta B'ye bakar. (Prototip 0.62·APO + uzun uç kullanır; köşeye taşmasın diye biraz kısaldı.)
                    float angB = HexMetrics.DirAngleDeg(cell.B) * Mathf.Deg2Rad;
                    var dirB = new Vector3(Mathf.Cos(angB), Mathf.Sin(angB), 0f);
                    float stubDist = 0.30f * apo;  // merkezden B yönündeki uç (ok ucu tabanı)
                    float headLen = 0.42f * s;     // ok ucu boyu
                    float headHalfW = 0.30f * s;
                    var stubTip = dirB * stubDist;
                    var point = stubTip + dirB * headLen;
                    AddPolyline(lw, EdgePoint(cell.A), Vector3.zero, stubTip); // şaft: giriş → merkez → B ucu
                    AddArrowHead(point, stubTip, dirB, headHalfW);             // ok ucu uçta, B'ye bakar
                    break;
                }
            }

            if (DrawShadow) BuildShadow(main);
        }

        /// <summary>Main'i klonlayıp yerinde büyütülmüş + gri + arkada bir gölge kopyası oluşturur.</summary>
        private void BuildShadow(GameObject main)
        {
            var shadow = Instantiate(main, _rotRoot);
            _shadow = shadow;
            shadow.name = "Shadow";
            shadow.transform.SetAsFirstSibling(); // hiyerarşide main'den önce
            shadow.transform.localPosition = new Vector3(0f, 0f, ShadowZBehind);

            var color = HexaThemeData.Active.SegmentShadow;
            foreach (var pl in shadow.GetComponentsInChildren<Shapes.Polyline>())
            {
                pl.Thickness *= ShadowThicknessMul; // kalınlaşır, uzamaz (uç kenardan taşmaz)
                pl.Color = color;
                pl.SortingOrder = 8;
            }
            foreach (var d in shadow.GetComponentsInChildren<Shapes.Disc>())
            {
                d.Radius *= ShadowSizeMul;
                d.Color = color;
                d.SortingOrder = 8;
            }
            foreach (var tri in shadow.GetComponentsInChildren<Shapes.Triangle>())
            {
                var c = (tri.A + tri.B + tri.C) / 3f; // kendi ağırlık merkezinden yerinde büyüt
                tri.A = c + (tri.A - c) * ShadowSizeMul;
                tri.B = c + (tri.B - c) * ShadowSizeMul;
                tri.C = c + (tri.C - c) * ShadowSizeMul;
                tri.Color = color;
                tri.SortingOrder = 8;
            }
        }

        /// <summary>Runtime gölge rengi güncellemesi (tema değişince BoardView çağırır).</summary>
        public void SetShadowColor(Color color)
        {
            if (_shadow == null) return;
            foreach (var sr in _shadow.GetComponentsInChildren<Shapes.ShapeRenderer>())
                sr.Color = color;
        }

        private void AddPolyline(float width, params Vector3[] points)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(_shapesParent, false);
            var pl = go.AddComponent<Shapes.Polyline>();
            pl.Geometry = Shapes.PolylineGeometry.Flat2D;
            pl.Closed = false; // varsayılan true → 3 nokta üçgen kapatırdı; açık kol olmalı
            pl.Joins = Shapes.PolylineJoins.Round;
            pl.Thickness = width;
            pl.Color = HexaPalette.Segment;
            pl.SortingOrder = 10;
            var pts = new System.Collections.Generic.List<Vector3>(points);
            pl.SetPoints(pts);
        }

        private void AddDot(float radius, Vector3 local)
        {
            var go = new GameObject("Dot");
            go.transform.SetParent(_shapesParent, false);
            go.transform.localPosition = local;
            var d = go.AddComponent<Shapes.Disc>();
            d.Geometry = Shapes.DiscGeometry.Flat2D;
            d.Radius = radius;
            d.Color = HexaPalette.Segment;
            d.SortingOrder = 10;
        }

        private void AddArrowHead(Vector3 point, Vector3 basePt, Vector3 dir, float halfWidth)
        {
            var go = new GameObject("Head");
            go.transform.SetParent(_shapesParent, false);
            var tri = go.AddComponent<Shapes.Triangle>();
            var perp = new Vector3(-dir.y, dir.x, 0f);
            tri.A = point;                        // tepe (ileri yön)
            tri.B = basePt + perp * halfWidth;    // taban köşeleri
            tri.C = basePt - perp * halfWidth;
            tri.Roundness = 0.5f;
            tri.Color = HexaPalette.Segment;
            tri.SortingOrder = 10;
        }

        private void BuildShape(float s, Cell cell)
        {
            float apo = HexMetrics.Apothem(s);
            float lw = 0.153f * s;
            Vector3 EdgePoint(int d)
            {
                float a = HexMetrics.DirAngleDeg(d) * Mathf.Deg2Rad;
                return new Vector3(apo * Mathf.Cos(a), apo * Mathf.Sin(a), 0f);
            }

            switch (cell.Type)
            {
                case CellType.Tail:
                {
                    AddLine(lw, Vector3.zero, EdgePoint(cell.B));
                    MeshFactory.NewMeshObject("Dot", MeshFactory.Circle(0.176f * s), HexaPalette.Segment, _rotRoot, Vector3.zero);
                    break;
                }
                case CellType.Mid:
                {
                    AddLine(lw, EdgePoint(cell.A), Vector3.zero, EdgePoint(cell.B));
                    break;
                }
                case CellType.Head:
                {
                    float tipDist = 0.62f * apo;
                    float angB = HexMetrics.DirAngleDeg(cell.B) * Mathf.Deg2Rad;
                    var tip = new Vector3(tipDist * Mathf.Cos(angB), tipDist * Mathf.Sin(angB), 0f);
                    AddLine(lw, EdgePoint(cell.A), Vector3.zero, tip);
                    var tri = MeshFactory.NewMeshObject("Head", MeshFactory.Triangle(0.41f * s, 0.25f * s), HexaPalette.Segment, _rotRoot, tip);
                    tri.transform.localRotation = Quaternion.Euler(0f, 0f, HexMetrics.DirAngleDeg(cell.B));
                    break;
                }
            }
        }

        private void AddLine(float width, params Vector3[] points)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(_rotRoot, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.material = MeshFactory.SharedMaterial;
            lr.startColor = HexaPalette.Segment;
            lr.endColor = HexaPalette.Segment;
            lr.widthMultiplier = width;
            lr.numCapVertices = 6;
            lr.numCornerVertices = 6;
            lr.positionCount = points.Length;
            lr.SetPositions(points);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }

        /// <summary>Bir tap'lik dönüşü başlatır/birleştirir. Veri rot'u çağıran taraf günceller.</summary>
        public void RotateOneStep()
        {
            _targetAngle += _stepDeg;
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(RotateRoutine());
        }

        public void SetInstantRot(int rot)
        {
            if (_anim != null) { StopCoroutine(_anim); _anim = null; }
            _targetAngle = _stepDeg * rot;
            _currentAngle = _targetAngle;
            _rotRoot.localRotation = Quaternion.AngleAxis(_targetAngle, _rotAxis);
        }

        public void SetVisible(bool visible) => _rotRoot.gameObject.SetActive(visible);

        private IEnumerator RotateRoutine()
        {
            float from = _currentAngle;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / RotateDuration);
                _currentAngle = Mathf.LerpUnclamped(from, _targetAngle, Easing.OutBack(t));
                _rotRoot.localRotation = Quaternion.AngleAxis(_currentAngle, _rotAxis);
                yield return null;
            }
            _anim = null;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Depth3D XZ — TEK PARÇA prosedürel segment mesh (SegmentMesh3D üreticisi)
        //  Açılı birleşimler kavisli dirsek; head'de stub kolu + tabanı stub'a oturan
        //  ok başı aynı mesh'te birleşir (CombineMeshes) — bindirme/çakışma yok.
        // ══════════════════════════════════════════════════════════════════════

        public static SegmentView Create3DXZ(Transform parent, Vector3 worldPos, float s, Cell cell, Material mat)
        {
            var go = new GameObject("Segment");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = worldPos;
            var view = go.AddComponent<SegmentView>();
            view._rotAxis = Vector3.up;   // XZ'de dönüş Y ekseni
            view._stepDeg = 60f;          // +60°/tap (2D -60 Z'nin XZ karşılığı)

            var rot = new GameObject("Rot");
            rot.transform.SetParent(go.transform, false);
            view._rotRoot = rot.transform;

            var meshGo = new GameObject("Mesh");
            meshGo.transform.SetParent(rot.transform, false);
            var mf = meshGo.AddComponent<MeshFilter>();
            mf.sharedMesh = GetOrBuildMesh3D(s, cell);
            var mr = meshGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat != null ? mat : MeshFactory.Lit3DTransparent;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            MeshFactory.SetColor(mr, HexaPalette.Segment);
            view._meshRenderer3D = mr;

            view.SetInstantRot(cell.Rot);
            return view;
        }

        private float _dim = 1f; // katman koyulaştırması (XZ; gömülü segment soluk görünür)

        /// <summary>Katman koyulaştırma çarpanı (gömülü hücre segmenti; terfi animasyonunda 1'e döner).</summary>
        public float Dim => _dim;

        public void SetDim(float dim)
        {
            _dim = Mathf.Clamp01(dim);
            if (_meshRenderer3D == null) return; // yalnızca XZ yolu (2D modlar gömülü çizmez)
            var c = HexaPalette.Segment;
            MeshFactory.SetColor(_meshRenderer3D, new Color(c.r * _dim, c.g * _dim, c.b * _dim, c.a));
        }

        /// <summary>XZ gölge: segment/ok'u caster yapar. ⚠ Materyal Transparent'sa URP gölge haritasına yazmaz.</summary>
        public void SetCastShadows(bool on)
        {
            if (_meshRenderer3D == null) return;
            _meshRenderer3D.shadowCastingMode = on
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Canlı materyal değişimi (RefreshTheme, XZ). Segment rengi (beyaz) MPB ile korunur.</summary>
        public void SetMaterial(Material mat)
        {
            if (_meshRenderer3D == null || mat == null) return;
            _meshRenderer3D.sharedMaterial = mat;
            MeshFactory.SetColor(_meshRenderer3D, HexaPalette.Segment);
        }

        // Şekil rot'tan bağımsız (rot transform ile uygulanır) → (Tip, A, B, s) önbelleği
        private static readonly Dictionary<(CellType, int, int, float), Mesh> _meshCache3D
            = new Dictionary<(CellType, int, int, float), Mesh>();

        /// <summary>Head'in dönüş keskinliğine göre ok başını dirB boyunca ne kadar ÖNE alacağı (dünya birimi).
        /// A ve B kolları arasındaki açı: 60° (en keskin) → tam HeadBendForwardMax; 120°+ (geniş/düz) → 0.
        /// Açı iki kol arasında olduğundan rotasyondan BAĞIMSIZ → local (A,B) ile world (WorldA,WorldB) aynı sonucu verir,
        /// böylece tile ok başı ile uçuş yolu ucu birebir eşleşir (seamless). HexaGameplayManager da bunu çağırır.</summary>
        public static float HeadForwardBump(int a, int b, float s)
        {
            float angA = HexMetrics.DirAngleDeg(a) * Mathf.Deg2Rad;
            float angB = HexMetrics.DirAngleDeg(b) * Mathf.Deg2Rad;
            var dirA = new Vector2(Mathf.Cos(angA), Mathf.Sin(angA));
            var dirB = new Vector2(Mathf.Cos(angB), Mathf.Sin(angB));
            float armAngle = Vector2.Angle(dirA, dirB);            // 60 (keskin) .. 180 (düz)
            float t = Mathf.Clamp01((180f - armAngle) / 120f);     // 180°→0, 120°→0.5, 60°→1
            return t * SegmentMesh3D.HeadBendForwardMax * s;
        }

        private static Mesh GetOrBuildMesh3D(float s, Cell cell)
        {
            var key = (cell.Type, cell.A, cell.B, s);
            if (_meshCache3D.TryGetValue(key, out var cached) && cached != null) return cached;
            var mesh = BuildSegmentMesh3D(s, cell);
            _meshCache3D[key] = mesh;
            return mesh;
        }

        private static Mesh BuildSegmentMesh3D(float s, Cell cell)
        {
            float apo = HexMetrics.Apothem(s);
            Vector2 Dir(int d)
            {
                float t = HexMetrics.DirAngleDeg(d) * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(t), Mathf.Sin(t));
            }
            Vector2 Edge(int d) => Dir(d) * apo;

            float w = SegmentMesh3D.Width * s, h = SegmentMesh3D.Height * s;
            float rc = SegmentMesh3D.Fillet * s, joinR = SegmentMesh3D.JoinRadius * s;

            switch (cell.Type)
            {
                case CellType.Tail:
                    // merkez ucu yuvarlak, kenar ucu DÜZ (komşu hücreyle yüz yüze birleşim)
                    return SegmentMesh3D.BuildStrip(
                        new List<Vector2> { Vector2.zero, Edge(cell.B) },
                        w, h, rc, joinR, capStart: true, capEnd: false);

                case CellType.Mid:
                    // tek sürekli şerit; açılıysa merkezde kavisli dirsek
                    return SegmentMesh3D.BuildStrip(
                        new List<Vector2> { Edge(cell.A), Vector2.zero, Edge(cell.B) },
                        w, h, rc, joinR, capStart: false, capEnd: false);

                default: // Head: giriş → merkez → B stub kolu + tabanı stub ucuna oturan ok başı
                {
                    var dirB = Dir(cell.B);
                    // dar açılı head'de ok başı elbow'a/A koluna yapışmasın diye ekstra öne itilir
                    float tipDist = SegmentMesh3D.HeadTipDist * s + HeadForwardBump(cell.A, cell.B, s);
                    float headLen = SegmentMesh3D.HeadLength * s;
                    float baseDist = tipDist - headLen;

                    // stub ok başının 0.04·s İÇİNE uzar — düz kapak ile ok başı arka duvarı
                    // eş düzlemli kalırsa z-fight titremesi (mesh defekti görünümü) oluşuyordu
                    var shaft = SegmentMesh3D.BuildStrip(
                        new List<Vector2> { Edge(cell.A), Vector2.zero, dirB * (baseDist + 0.04f * s) },
                        w, h, rc, joinR, capStart: false, capEnd: false);
                    var head = SegmentMesh3D.BuildArrowhead(
                        headLen, SegmentMesh3D.HeadHalfWidth * s, h, rc, SegmentMesh3D.HeadCornerRadius * s);

                    float yaw = -HexMetrics.DirAngleDeg(cell.B); // +X → dirB (Y ekseni dönüşü)
                    var combine = new CombineInstance[]
                    {
                        new CombineInstance { mesh = shaft, transform = Matrix4x4.identity },
                        new CombineInstance
                        {
                            mesh = head,
                            transform = Matrix4x4.TRS(
                                new Vector3(dirB.x, 0f, dirB.y) * baseDist,
                                Quaternion.Euler(0f, yaw, 0f), Vector3.one)
                        }
                    };
                    var m = new Mesh { name = "HeadSegment3D" };
                    m.CombineMeshes(combine, true, true);
                    m.RecalculateBounds();
                    return m;
                }
            }
        }
    }
}

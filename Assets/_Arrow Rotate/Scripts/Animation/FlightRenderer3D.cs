using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// XZ uçuş/bounce animasyonu — LineRenderer YOK. Her frame, yol üzerindeki [kuyruk..uç]
    /// penceresi için SegmentMesh3D şeridi üretilir; uçta tile'lardakiyle AYNI ok başı mesh'i
    /// yol tanjantına dönük ilerler. Kesit/yükseklik/fillet tile segmentleriyle birebir aynı
    /// olduğundan kalkış anı SEAMLESS'tır (tile segmenti gizlenir, aynı görünümlü şerit akar).
    /// </summary>
    public class FlightRenderer3D : MonoBehaviour
    {
        // ── gökkuşağı juice (yalnız TEMİZ çıkışta; bounce'ta KAPALI) ──
        // UV.x OK'A GÖRELİ (uçan kuyruğa uzaklık) yazılır — dünya pozisyonuna DEĞİL. Yoksa ok hızlı
        // uçarken sabit uzaysal bantların içinden geçip strobe/flash olurdu. Kaydırmayı shader _Time ile yapar.
        private const float RainbowUVScale = 0.33f; // dünya birimi → uv (tam gradient her ~3 birimde)

        private List<Vector2> _pts;   // planar yol (+uzatma); planar (x,y) → dünya (X,Z)
        private float[] _cum;
        private float _total, _bodyLen;
        private float _s;
        private MeshFilter _stripMF;
        private MeshFilter _headMF;
        private MeshRenderer _stripMR, _headMR;
        private Transform _head;
        private float _headLen;
        private bool _rainbow;        // yalnız Fly() açar (bounce'ta kapalı → engel varsa efekt yok)

        private static Material _rainbowMat;
        private static Material RainbowMat
        {
            get
            {
                if (_rainbowMat != null) return _rainbowMat;
                var sh = Shader.Find("ArrowRotate/RainbowVertex");
                if (sh == null) return null; // shader yoksa gökkuşağı devre dışı
                _rainbowMat = new Material(sh) { name = "ArrowRainbow (runtime)" };
                _rainbowMat.SetFloat("_Glow", 1f);
                _rainbowMat.SetFloat("_ScrollSpeed", 0.5f);
                _rainbowMat.SetTexture("_GradientTex", RainbowGradient);
                return _rainbowMat;
            }
        }

        /// <summary>Değiştirilebilir gradient texture — varsayılan HSV gökkuşağı (256×1, wrap Repeat).
        /// İleride farklı gradient denemek için bu texture'ı değiştir (veya RainbowMat._GradientTex ata).</summary>
        private static Texture2D _rainbowGradient;
        private static Texture2D RainbowGradient
        {
            get
            {
                if (_rainbowGradient != null) return _rainbowGradient;
                const int w = 256;
                var tex = new Texture2D(w, 1, TextureFormat.RGBA32, false)
                {
                    name = "RainbowGradient",
                    wrapMode = TextureWrapMode.Repeat,   // kaydırma kusursuz döner (hue 1==0)
                    filterMode = FilterMode.Bilinear,
                };
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, 0, Color.HSVToRGB(x / (float)w, 0.95f, 1f));
                tex.Apply();
                _rainbowGradient = tex;
                return _rainbowGradient;
            }
        }

        public static FlightRenderer3D Create(List<(float x, float y)> pathPts, float s, float extension, float surfaceY, Material mat)
        {
            var go = new GameObject("Flight3D");
            go.transform.position = new Vector3(0f, surfaceY, 0f);
            var fr = go.AddComponent<FlightRenderer3D>();
            fr._s = s;

            var pts = new List<Vector2>(pathPts.Count + 1);
            foreach (var (x, y) in pathPts) pts.Add(new Vector2(x, y));
            var last = pts[pts.Count - 1];
            var dirExt = (last - pts[pts.Count - 2]).normalized;
            pts.Add(last + dirExt * extension);
            fr._pts = pts;

            fr._cum = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++)
                fr._cum[i] = fr._cum[i - 1] + Vector2.Distance(pts[i - 1], pts[i]);
            fr._total = fr._cum[fr._cum.Length - 1];
            fr._bodyLen = fr._total - extension;

            // BEYAZ başla (bounce beyaz kalır). Temiz çıkışta Fly() gökkuşağına çevirir.
            var material = mat != null ? mat : MeshFactory.Lit3DTransparent;

            var stripGo = new GameObject("Body");
            stripGo.transform.SetParent(go.transform, false);
            fr._stripMF = stripGo.AddComponent<MeshFilter>();
            fr._stripMR = stripGo.AddComponent<MeshRenderer>();
            fr._stripMR.sharedMaterial = material;
            fr._stripMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fr._stripMR.receiveShadows = false;
            MeshFactory.SetColor(fr._stripMR, HexaPalette.Segment);

            fr._headLen = SegmentMesh3D.HeadLength * s;
            var headGo = new GameObject("Head");
            headGo.transform.SetParent(go.transform, false);
            fr._headMF = headGo.AddComponent<MeshFilter>();
            fr._headMF.sharedMesh = SegmentMesh3D.BuildArrowhead(
                fr._headLen, SegmentMesh3D.HeadHalfWidth * s,
                SegmentMesh3D.Height * s, SegmentMesh3D.Fillet * s, SegmentMesh3D.HeadCornerRadius * s);
            fr._headMR = headGo.AddComponent<MeshRenderer>();
            fr._headMR.sharedMaterial = material;
            fr._headMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fr._headMR.receiveShadows = false;
            MeshFactory.SetColor(fr._headMR, HexaPalette.Segment);
            fr._head = headGo.transform;

            fr.SetOffset(0f);
            return fr;
        }

        public void Fly(float speed, Action onDone)
        {
            EnableRainbow(); // temiz çıkış → gökkuşağı (bounce bunu çağırmaz, beyaz kalır)
            StartCoroutine(FlyRoutine(speed, onDone));
        }

        /// <summary>Strip+ok başını gökkuşağı materyaline çevirir (shader varsa). Yalnız Fly'da çağrılır.</summary>
        private void EnableRainbow()
        {
            var rb = RainbowMat;
            if (rb == null) return; // shader yok → beyaz kalır
            _rainbow = true;
            _stripMR.sharedMaterial = rb;
            _headMR.sharedMaterial = rb;
            SetOffset(0f); // UV'leri hemen yaz
        }

        private IEnumerator FlyRoutine(float speed, Action onDone)
        {
            float d = 0f;
            while (d < _total)
            {
                d = Mathf.Min(_total, d + speed * Time.deltaTime);
                SetOffset(d);
                yield return null;
            }
            Destroy(gameObject);
            onDone?.Invoke();
        }

        public void Bounce(float hitDist, float duration, Action onDone) => StartCoroutine(BounceRoutine(hitDist, duration, onDone));

        private IEnumerator BounceRoutine(float hitDist, float duration, Action onDone)
        {
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / duration);
                SetOffset(Mathf.Max(0f, hitDist * Mathf.Sin(Mathf.PI * t)));
                yield return null;
            }
            Destroy(gameObject);
            onDone?.Invoke();
        }

        /// <summary>Pencere: kuyruk=d, uç=min(bodyLen+d,total); şerit ok başının 0.04·s İÇİNE uzar
        /// (tile head'iyle aynı çözüm — eş düzlemli düz kapak ok başı arka duvarıyla z-fight yapıyordu).</summary>
        private void SetOffset(float d)
        {
            float tailL = Mathf.Clamp(d, 0f, _total);
            float tipL = Mathf.Min(_bodyLen + d, _total);
            float baseL = Mathf.Max(tailL, tipL - _headLen);
            float stripEnd = Mathf.Min(baseL + 0.04f * _s, tipL);

            if (stripEnd - tailL > 0.01f * _s)
            {
                var win = new List<Vector2> { PointAt(tailL) };
                for (int i = 0; i < _pts.Count; i++)
                    if (_cum[i] > tailL && _cum[i] < stripEnd) win.Add(_pts[i]);
                win.Add(PointAt(stripEnd));

                var mesh = SegmentMesh3D.BuildStrip(win,
                    SegmentMesh3D.Width * _s, SegmentMesh3D.Height * _s, SegmentMesh3D.Fillet * _s,
                    SegmentMesh3D.JoinRadius * _s, capStart: true, capEnd: false);
                var old = _stripMF.sharedMesh;
                _stripMF.sharedMesh = mesh;
                if (old != null) Destroy(old);
                _stripMF.gameObject.SetActive(true);
            }
            else
            {
                _stripMF.gameObject.SetActive(false);
            }

            var basePos = PointAt(baseL);
            var dir = DirAt(baseL);
            _head.localPosition = new Vector3(basePos.x, 0f, basePos.y);
            _head.localRotation = Quaternion.Euler(0f, -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 0f);

            if (_rainbow)
            {
                // referans: uçan kuyruğun dünya konumu — UV.x buna GÖRE (okla taşınır, strobe olmaz).
                // Kaydırmayı shader _Time ile yapar; burada yalnız ok'a göreli UV yazılır.
                var tp = PointAt(tailL);
                var refW = transform.TransformPoint(new Vector3(tp.x, 0f, tp.y));
                if (_stripMF.gameObject.activeSelf && _stripMF.sharedMesh != null)
                    WriteRainbowUV(_stripMF.sharedMesh, _stripMF.transform, refW);
                if (_headMF.sharedMesh != null)
                    WriteRainbowUV(_headMF.sharedMesh, _head, refW);
            }
        }

        /// <summary>Mesh UV.x'ini OK'A GÖRELİ (refW=kuyruğa uzaklık × ölçek) yazar; UV.y=0.5 (tek satır gradient).
        /// Strip ve ok başı AYNI refW → gradient kesintisiz, okla taşınır. Shader UV.x'i _Time ile kaydırır.</summary>
        private static void WriteRainbowUV(Mesh m, Transform tf, Vector3 refW)
        {
            var verts = m.vertices;
            var uvs = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                var w = tf.TransformPoint(verts[i]);
                float d = Vector3.Distance(w, refW); // kuyruktan ok boyunca uzaklık
                uvs[i] = new Vector2(d * RainbowUVScale, 0.5f);
            }
            m.uv = uvs;
        }

        private Vector2 PointAt(float dist)
        {
            dist = Mathf.Clamp(dist, 0f, _total);
            for (int i = 1; i < _cum.Length; i++)
            {
                if (dist <= _cum[i])
                {
                    float segLen = _cum[i] - _cum[i - 1];
                    float t = segLen < 1e-6f ? 0f : (dist - _cum[i - 1]) / segLen;
                    return Vector2.Lerp(_pts[i - 1], _pts[i], t);
                }
            }
            return _pts[_pts.Count - 1];
        }

        private Vector2 DirAt(float dist)
        {
            dist = Mathf.Clamp(dist, 0f, _total);
            for (int i = 1; i < _cum.Length; i++)
            {
                if (dist <= _cum[i])
                {
                    var v = _pts[i] - _pts[i - 1];
                    return v.sqrMagnitude < 1e-10f ? Vector2.right : v.normalized;
                }
            }
            return Vector2.right;
        }

        private void OnDestroy()
        {
            if (_stripMF != null && _stripMF.sharedMesh != null) Destroy(_stripMF.sharedMesh);
        }
    }
}

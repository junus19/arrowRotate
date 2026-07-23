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
        // UV = DÜNYA POZİSYONUNUN UÇUŞ YÖNÜNE İZDÜŞÜMÜ × freq. ZAMAN KAYDIRMASI YOK — renk değişimi
        // tamamen yılanın HAREKETİNDEN gelir (tek hareket → temiz his). Bantlar harekete diktir; yılan
        // ilerledikçe dünyada sabit duran bantların içinden geçer, renkler gövde boyunca akar.
        // ⚠ freq düşük tutulmalı: renk değişim hızı = flightSpeed(≈25.9 u/s) × freq. 0.09 → ~2.3 tur/sn (iyi).
        // Yüksek freq → hızlı uçuşta strobe (yaşandı). x ve z ikisi de exitDir üzerinden doğru katkı verir.
        private const float RainbowWorldFreq = 0.09f;

        private List<Vector2> _pts;   // planar yol (+uzatma); planar (x,y) → dünya (X,Z)
        private float[] _cum;
        private float _total, _bodyLen;
        private float _s;
        private Vector2 _exitDir;     // uçuş yönü (planar, normalize) — dünya→UV izdüşüm ekseni
        private MeshFilter _stripMF;
        private MeshFilter _headMF;
        private MeshRenderer _stripMR, _headMR;
        private Transform _head;
        private float _headLen;
        private bool _rainbow;        // yalnız Fly() açar (bounce'ta kapalı → engel varsa efekt yok)
        private Color _paletteColor = Color.white; // çıkan okun palet rengi → gradient buradan türer
        private Material _fxMat;      // uçuş başına materyal (kendi gradient texture'ı)
        private Texture2D _fxTex;     // uçuş başına gradient texture (palet renk ailesi)

        /// <summary>Ok gövdesi palet-renk gradient'i olsun mu? false = beyaz kalır. Particle iz denemesinde
        /// beyaz istendi; true yapılınca palet gradient geri gelir (ikisi birlikte de çalışır).</summary>
        public static bool UseColorGradient = false;

        // ── particle izi (ok çıkarken arkadan random şekilli parçacıklar) ──
        private bool _trail;          // yalnız Fly() açar (bounce'ta iz yok)
        private readonly List<ParticleSystem> _psList = new List<ParticleSystem>(); // sprite başına bir PS; emit'te random seçilir
        private Sprite[] _trailSprites;
        private float _emitTimer, _nextEmitGap;
        private const float TrailGapMin = 0.012f, TrailGapMax = 0.035f; // random emit aralığı (sn) — daha sık
        private const float TrailLateralSpread = 0.3f; // uçuş yönüne DİK random savrulma (±, × S) — hafif serpilme

        private static Shader _trailShader;
        private static Shader TrailShader => _trailShader != null ? _trailShader
            : (_trailShader = Shader.Find("ArrowRotate/Trail"));

        // sprite → materyal önbelleği (sprite seti sabit; materyaller session boyunca paylaşılır, leak yok)
        private static readonly Dictionary<Texture, Material> _trailMats = new Dictionary<Texture, Material>();
        private static Material TrailMatFor(Texture tex)
        {
            if (TrailShader == null || tex == null) return null;
            if (_trailMats.TryGetValue(tex, out var m) && m != null) return m;
            m = new Material(TrailShader) { name = "ArrowTrail (runtime)" };
            m.SetTexture("_MainTex", tex);
            _trailMats[tex] = m;
            return m;
        }

        private static Shader _fxShader;
        private static Shader FxShader => _fxShader != null ? _fxShader
            : (_fxShader = Shader.Find("ArrowRotate/RainbowVertex")); // yoksa null → beyaz fallback

        // ── palet renginden 2-3 tonlu geçiş üretici ──
        private const float HueShift = 0.08f; // renk ailesi genişliği (kırmızı→turuncu ~+0.08 hue)

        /// <summary>Palet renginden DİKİŞSİZ (palindrom) 256×1 gradient: base ↔ (hue+HueShift, biraz parlak).
        /// Palindrom sayesinde wrap Repeat'te kusursuz döner → dünya-pozisyonu kaydırmasıyla renk ailesi
        /// içinde salınır (ör. kırmızı↔turuncu), tam gökkuşağı DEĞİL.</summary>
        private static Texture2D BuildPaletteGradient(Color baseCol)
        {
            Color.RGBToHSV(baseCol, out float h, out float s, out float v);
            var low = Color.HSVToRGB(h, s, v);
            var high = Color.HSVToRGB(Mathf.Repeat(h + HueShift, 1f), Mathf.Clamp01(s * 0.9f), Mathf.Clamp01(v * 1.12f));
            const int w = 256;
            var tex = new Texture2D(w, 1, TextureFormat.RGBA32, false)
            {
                name = "PaletteGradient", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear,
            };
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);
                float p = t < 0.5f ? t * 2f : (1f - t) * 2f; // palindrom → uçlar eşit (dikişsiz)
                tex.SetPixel(x, 0, Color.Lerp(low, high, p));
            }
            tex.Apply();
            return tex;
        }

        public static FlightRenderer3D Create(List<(float x, float y)> pathPts, float s, float extension, float surfaceY, Material mat, Color paletteColor, Sprite[] trailSprites = null)
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
            fr._exitDir = dirExt; // dünya→UV izdüşüm ekseni (bantlar bu yöne dik)
            fr._paletteColor = paletteColor;
            fr._trailSprites = trailSprites;

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

            fr.BuildTrail(go);
            fr.SetOffset(0f);
            return fr;
        }

        /// <summary>Sprite başına bir ParticleSystem kurar (emit KAPALI; Fly'da random aralıkla random PS'e elle
        /// emit → şekiller karışık gelir). Farklı texture'lı sprite'lar tek materyalde karışamadığı için ayrı PS.
        /// TrailSprites boşsa tek PS + kod-içi daire. Her PS: random rotation + random tint/alfa + söner.</summary>
        private void BuildTrail(GameObject parent)
        {
            _psList.Clear();
            if (_trailSprites != null && _trailSprites.Length > 0)
            {
                foreach (var sp in _trailSprites)
                {
                    if (sp == null) continue;
                    var m = TrailMatFor(sp.texture);
                    if (m != null) MakePS(parent, m);
                }
            }
            if (_psList.Count == 0) // fallback: kod-içi daire
            {
                var m = TrailMatFor(ArrowRotate.UI.UiSprites.Circle.texture);
                if (m != null) MakePS(parent, m);
            }
        }

        private void MakePS(GameObject parent, Material mat)
        {
            var psGo = new GameObject("Trail");
            psGo.transform.SetParent(parent.transform, false);
            var ps = psGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // bırakılan parçacık yerinde kalır → iz
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            main.startSpeed = 0f;                                        // yerinde durur, sadece söner
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f * _s, 0.4f * _s);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f); // random dönüş (radyan)
            // random tint (hafif) + random alfa: iki renk arası → parçacık başına ton/alfa çeşitlenir
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.7f), new Color(0.86f, 0.92f, 1f, 1f));
            main.maxParticles = 400;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;   // otomatik değil — Fly'da random aralıkla Emit
            emission.rateOverDistance = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f * _s;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.35f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad); // startColor'ı ÇARPAR → random alfa peak korunur, sonra söner

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var sizeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.4f));
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var psr = psGo.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sortingOrder = 3;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;

            ps.Play();
            _psList.Add(ps);
        }

        public void Fly(float speed, Action onDone)
        {
            if (UseColorGradient) EnableRainbow(); // temiz çıkış → palet gradient'i (kapalıysa ok beyaz kalır)
            _trail = _psList.Count > 0;            // particle izi (bounce bunu açmaz)
            _nextEmitGap = UnityEngine.Random.Range(TrailGapMin, TrailGapMax);
            StartCoroutine(FlyRoutine(speed, onDone));
        }

        /// <summary>Strip+ok başını palet-renk gradient materyaline çevirir (shader varsa). Yalnız Fly'da.
        /// Uçuş başına kendi materyali + texture'ı (palet renginden) — OnDestroy'da temizlenir.</summary>
        private void EnableRainbow()
        {
            if (FxShader == null) return; // shader yok → beyaz kalır
            _fxTex = BuildPaletteGradient(_paletteColor);
            _fxMat = new Material(FxShader) { name = "ArrowFx (runtime)" };
            _fxMat.SetFloat("_Glow", 1f);
            _fxMat.SetTexture("_GradientTex", _fxTex);
            _rainbow = true;
            _stripMR.sharedMaterial = _fxMat;
            _headMR.sharedMaterial = _fxMat;
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
            DetachTrail(); // kalan parçacıklar sönene dek yaşasın (Flight3D yok edilince silinmesin)
            Destroy(gameObject);
            onDone?.Invoke();
        }

        /// <summary>İz sistemlerini Flight3D'den ayırır, emisyonu durdurur, kalan parçacıklar sönünce yok eder.</summary>
        private void DetachTrail()
        {
            foreach (var ps in _psList)
            {
                if (ps == null) continue;
                ps.transform.SetParent(null, true);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(ps.gameObject, ps.main.startLifetime.constantMax + 0.2f);
            }
            _psList.Clear();
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
                // UV = dünya pozisyonunun uçuş yönüne izdüşümü (ZAMAN YOK) — renk değişimi hareketten gelir.
                if (_stripMF.gameObject.activeSelf && _stripMF.sharedMesh != null)
                    WriteRainbowUV(_stripMF.sharedMesh, _stripMF.transform);
                if (_headMF.sharedMesh != null)
                    WriteRainbowUV(_headMF.sharedMesh, _head);
            }

            // particle izi: random aralıkta okun KUYRUĞUNDA (arka uç) RANDOM bir şekil bırak → gövdenin ARKASINDA
            // kalır (baş=ön uçta emit edilirse gövdenin altında kalıyordu). Sim World → yerinde kalıp söner.
            if (_trail && _psList.Count > 0)
            {
                _emitTimer += Time.deltaTime;
                if (_emitTimer >= _nextEmitGap)
                {
                    _emitTimer = 0f;
                    _nextEmitGap = UnityEngine.Random.Range(TrailGapMin, TrailGapMax);
                    var tp = PointAt(tailL); // kuyruğun (arka ucun) planar konumu
                    // uçuş yönüne DİK yanal savrulma (ok yukarı giderse ±x): perp = (-exitDir.y, exitDir.x)
                    var perp = new Vector2(-_exitDir.y, _exitDir.x);
                    float lat = UnityEngine.Random.Range(-TrailLateralSpread, TrailLateralSpread) * _s;
                    var tailWorld = transform.TransformPoint(new Vector3(tp.x + perp.x * lat, 0f, tp.y + perp.y * lat));
                    var ep = new ParticleSystem.EmitParams { position = tailWorld };
                    _psList[UnityEngine.Random.Range(0, _psList.Count)].Emit(ep, 1); // random şekil (Circle/Star/Square)
                }
            }
        }

        /// <summary>Mesh UV.x'ini dünya pozisyonunun UÇUŞ YÖNÜNE (_exitDir) izdüşümü × freq yazar; UV.y=0.5.
        /// Zaman yok — bantlar dünyada sabit ve harekete dik; yılan ilerledikçe içlerinden geçer → renkler
        /// gövde boyunca AKAR (tek hareket kaynağı). Strip+ok başı AYNI dünya eşlemesi → kesintisiz.</summary>
        private void WriteRainbowUV(Mesh m, Transform tf)
        {
            var verts = m.vertices;
            var uvs = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                var w = tf.TransformPoint(verts[i]);
                float proj = w.x * _exitDir.x + w.z * _exitDir.y; // dünya(x,z) · uçuş yönü
                uvs[i] = new Vector2(proj * RainbowWorldFreq, 0.5f);
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
            if (_fxMat != null) Destroy(_fxMat);   // uçuş başına materyal + texture (leak olmasın)
            if (_fxTex != null) Destroy(_fxTex);
        }
    }
}

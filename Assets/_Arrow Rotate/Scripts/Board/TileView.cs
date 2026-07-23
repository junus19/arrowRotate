using System.Collections;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Tek hex taşı. Palet renginde, gölgesiz; taşlar arası ince doğal boşluk (SKILL.md §9).
    /// Vanish: 450ms scale(0.25) + ekranda saat yönü 30° + fade.
    /// FadeOut/FadeIn: ok bağlanınca taşlar saydamlaşır, çarpıp geri dönerse geri açılır.
    /// </summary>
    public class TileView : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private Shapes.RegularPolygon _poly; // Shapes2D modda dolu; renk/alfa buradan uygulanır
        private Color _color;
        private float _alpha = 1f;
        private float _dim = 1f; // katman koyulaştırması (1 = yüzey, <1 = gömülü)
        private Coroutine _fade;
        private Vector3 _spinAxis = Vector3.forward; // vanish dönüş ekseni (XZ modda Y)
        private bool _isXZ;           // XZ modu → disintegration efekti uygulanır
        private float _cellSize = 1f; // parçacık ölçekleri için S

        /// <summary>XZ'de taş yok olurken parçalanma (disintegration) parçacık patlaması. false = eski scale+spin+fade.</summary>
        public static bool UseDisintegrate = true;

        /// <summary>Katman koyulaştırma çarpanı (gömülü hücreler için; terfi animasyonunda 1'e döner).</summary>
        public float Dim => _dim;

        public void SetDim(float dim)
        {
            _dim = Mathf.Clamp01(dim);
            SetAlpha(_alpha); // rengi _dim ile yeniden uygula
        }

        /// <summary>Depth3D XZ: EP hexagon mesh'i XZ düzleminde (yatık), palet renginde. Fade/vanish material alfası ile.
        /// xzScale: yatay footprint çarpanı (hücre aralığı sabit → segment bağlantıları etkilenmez). thickness: Y kalınlık çarpanı.</summary>
        public static TileView Create3DXZ(Transform parent, Vector3 worldPos, float s, Color color, Mesh hexMesh, Material mat,
                                          float xzScale = 1f, float thickness = 1f)
        {
            var go = new GameObject("Tile");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = worldPos;
            // mesh köşe yarıçapı ~1 → S ile ölçek; yatay xzScale, kalınlık (Y) thickness ile ayrıca çarpılır
            go.transform.localScale = new Vector3(s * xzScale, s * thickness, s * xzScale);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = hexMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat != null ? mat : MeshFactory.Lit3DTransparent;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var view = go.AddComponent<TileView>();
            view._renderer = mr;
            view._color = color;
            view._spinAxis = Vector3.up; // XZ'de vanish dönüşü Y ekseni
            view._isXZ = true;
            view._cellSize = s;
            MeshFactory.SetColor(mr, color);
            return view;
        }

        /// <summary>Shapes2D: yuvarlatılmış RegularPolygon hexagon (flat-top, köşeler 0°/60°…).</summary>
        public static TileView CreateShapes(Transform parent, Vector3 pos, float s, Color color)
        {
            var go = new GameObject("Tile");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var poly = go.AddComponent<Shapes.RegularPolygon>();
            poly.Geometry = Shapes.RegularPolygonGeometry.Flat2D;
            poly.Sides = 6;
            poly.Angle = 0f;              // MeshFactory.Hex ile aynı: köşe 0°'de → flat-top
            poly.Radius = s * 0.985f;     // roundness içeri yediği için 2D inset'e denk gelir
            poly.Roundness = 0.14f;       // referanstaki hafif yuvarlak köşeler
            poly.Color = color;
            poly.SortingOrder = 0;

            var view = go.AddComponent<TileView>();
            view._poly = poly;
            view._color = color;
            return view;
        }

        /// <summary>
        /// 3D mod: hexagon.fbx taş olarak (model XZ düzleminde yatar, köşeler local Z'de,
        /// pivot üst yüzeyde). X'te -90° → üst yüz kameraya (-Z), gövde +Z'ye (derinlik);
        /// ekran düzleminde -30° → flat-top hizası (köşeler 0°,60°,...). Ölçek bounds'tan otomatik.
        /// </summary>
        public static TileView Create3D(Transform parent, Vector3 pos, float s, Color color, GameObject model)
        {
            var go = new GameObject("Tile");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var inst = Instantiate(model, go.transform, false);
            inst.transform.localRotation =
                Quaternion.AngleAxis(-30f, Vector3.forward) * Quaternion.AngleAxis(-90f, Vector3.right);

            var renderer = inst.GetComponentInChildren<MeshRenderer>();
            var meshFilter = inst.GetComponentInChildren<MeshFilter>();
            float modelCornerRadius = meshFilter.sharedMesh.bounds.size.z * 0.5f; // köşeler local Z'de
            float targetRadius = s * 0.91f; // 2D inset ile aynı görsel boşluk
            float fit = targetRadius / Mathf.Max(0.0001f, modelCornerRadius);
            // local Y = kalınlık ekseni; hafif kalınlaştırma alt kenar dudağını belirginleştirir
            inst.transform.localScale = new Vector3(fit, fit * 1.25f, fit);

            renderer.sharedMaterial = MeshFactory.Lit3DTransparent;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var view = go.AddComponent<TileView>();
            view._renderer = renderer;
            view._color = color;
            MeshFactory.SetColor(renderer, color);
            return view;
        }

        public static TileView Create(Transform parent, Vector3 pos, float s, Color color)
        {
            float inset = s * 0.09f; // proto: S-3 @ S=34
            var go = MeshFactory.NewMeshObject("Tile", MeshFactory.Hex(s - inset), color, parent, pos);
            var view = go.AddComponent<TileView>();
            view._renderer = go.GetComponent<MeshRenderer>();
            view._color = color;
            return view;
        }

        /// <summary>XZ gölge: taşı caster yapar (zemine gölge düşsün). ⚠ Materyal Transparent'sa URP gölge haritasına yazmaz.</summary>
        public void SetCastShadows(bool on)
        {
            if (_renderer == null) return;
            _renderer.shadowCastingMode = on
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Canlı materyal değişimi (RefreshTheme). Renk MPB ile taşındığı için değişmez.</summary>
        public void SetMaterial(Material mat)
        {
            if (_renderer == null || mat == null) return;
            _renderer.sharedMaterial = mat;
            MeshFactory.SetColor(_renderer, _color); // MPB yeni materyalde de _BaseColor'ı korusun
        }

        public void Vanish(float delay)
        {
            StartCoroutine(VanishRoutine(delay));
        }

        private IEnumerator VanishRoutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (_fade != null) { StopCoroutine(_fade); _fade = null; }

            if (_isXZ && UseDisintegrate)
            {
                // parçalanma: taş rengi küçük parçalar dışa savrulup dönerek düşer + söner; taş hızlı kaybolur
                SpawnDisintegration(transform.position, _color, _cellSize);
                float startAlphaD = _alpha; float td = 0f; const float durD = 0.14f;
                while (td < 1f)
                {
                    td = Mathf.Min(1f, td + Time.deltaTime / durD);
                    transform.localScale *= 0.9f;                 // hafif büzülüp
                    SetAlpha(startAlphaD * (1f - td));            // sönerek yerini parçalara bırakır
                    yield return null;
                }
                gameObject.SetActive(false);
                yield break;
            }

            const float dur = 0.45f;
            Vector3 startScale = transform.localScale;
            float startAlpha = _alpha; // saydamlaşmış taş geri parlamasın
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / dur);
                float e = Easing.OutCubic(t);
                transform.localScale = startScale * Mathf.Lerp(1f, 0.25f, e);
                transform.localRotation = Quaternion.AngleAxis(-30f * e, _spinAxis); // ekranda saat yönü
                SetAlpha(startAlpha * (1f - e));
                yield return null;
            }
            gameObject.SetActive(false);
        }

        // ── disintegration (parçalanma) parçacık patlaması ──
        private static Material _pieceMat;
        private static Material PieceMat
        {
            get
            {
                if (_pieceMat != null) return _pieceMat;
                var sh = Shader.Find("ArrowRotate/Trail"); // texture × parçacık rengi, alpha blend
                if (sh == null) return null;
                _pieceMat = new Material(sh) { name = "TilePiece (runtime)" };
                _pieceMat.SetTexture("_MainTex", PieceTexture);
                return _pieceMat;
            }
        }

        private static Texture2D _pieceTex;
        private static Texture2D PieceTexture
        {
            get
            {
                if (_pieceTex != null) return _pieceTex;
                const int w = 16; // dolu kare (küçük soft kenar) → "parça" hissi
                var tex = new Texture2D(w, w, TextureFormat.RGBA32, false) { name = "TilePiece", filterMode = FilterMode.Bilinear };
                for (int y = 0; y < w; y++)
                for (int x = 0; x < w; x++)
                {
                    float ex = Mathf.Min(x, w - 1 - x), ey = Mathf.Min(y, w - 1 - y);
                    float a = Mathf.Clamp01(Mathf.Min(ex, ey)); // 1px yumuşak kenar
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                _pieceTex = tex;
                return _pieceTex;
            }
        }

        /// <summary>Verilen konumda taş rengi parçacık patlaması: dışa savrulur, döner, yerçekimiyle düşer, söner.</summary>
        private static void SpawnDisintegration(Vector3 worldPos, Color color, float s)
        {
            var mat = PieceMat;
            if (mat == null) return;

            var go = new GameObject("Disintegrate");
            go.transform.position = worldPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * s, 6f * s);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f * s, 0.34f * s);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(color.r, color.g, color.b, 1f);
            main.gravityModifier = 2.2f;   // savrulup düşerler
            main.maxParticles = 48;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)18) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere; // taş üstünden yukarı+dışa
            shape.radius = 0.35f * s;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f); // tumbling

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f)));

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sortingOrder = 4;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;

            ps.Play();
            Destroy(go, 1.2f); // ömür + pay sonra kendini temizler
        }

        /// <summary>Ok bağlandı: taş saydamlaşır, segment zeminde asılı kalır.</summary>
        public void FadeOut(float dur = 0.45f) => FadeTo(0f, dur);

        /// <summary>Ok çarpıp geri döndü: taş geri açılır.</summary>
        public void FadeIn(float dur = 0.45f) => FadeTo(1f, dur);

        private void FadeTo(float target, float dur)
        {
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeRoutine(target, dur));
        }

        private IEnumerator FadeRoutine(float target, float dur)
        {
            float start = _alpha;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / dur);
                SetAlpha(Mathf.Lerp(start, target, Easing.OutCubic(t)));
                yield return null;
            }
            _fade = null;
        }

        private void SetAlpha(float a)
        {
            _alpha = a;
            var c = new Color(_color.r * _dim, _color.g * _dim, _color.b * _dim, a);
            if (_poly != null) _poly.Color = c;
            else MeshFactory.SetColor(_renderer, c);
        }
    }
}

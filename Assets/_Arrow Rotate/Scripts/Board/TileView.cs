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
        private Coroutine _fade;
        private Vector3 _spinAxis = Vector3.forward; // vanish dönüş ekseni (XZ modda Y)

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
            var c = _color;
            c.a = a;
            if (_poly != null) _poly.Color = c;
            else MeshFactory.SetColor(_renderer, c);
        }
    }
}

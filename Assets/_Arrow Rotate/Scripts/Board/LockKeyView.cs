using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>Sprite quad'ını sürekli kameraya karşı döndürür (ikonlar okunaklı kalsın).</summary>
    public class Billboard : MonoBehaviour
    {
        private Camera _cam;
        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam != null) transform.rotation = _cam.transform.rotation;
        }
    }

    /// <summary>Kilit/anahtar görsel yardımcıları: dünyada billboard sprite ikon + hexagon kapak (lid).</summary>
    public static class LockKeyFx
    {
        private static Mesh _quad;
        private static readonly Dictionary<Texture, Material> _iconMats = new Dictionary<Texture, Material>();

        // Grup renkleri — AÇIK tonlar; kilit & anahtar aynı grupta aynı renk (hangi anahtar hangi kilit belli olsun).
        private static readonly Color[] GroupColors =
        {
            new Color(0.60f, 0.80f, 1.00f), // açık mavi
            new Color(0.65f, 1.00f, 0.70f), // açık yeşil
            new Color(1.00f, 0.72f, 0.85f), // açık pembe
            new Color(1.00f, 0.90f, 0.55f), // açık sarı
            new Color(0.80f, 0.72f, 1.00f), // açık mor
            new Color(1.00f, 0.80f, 0.60f), // açık şeftali
        };
        public static Color GroupColor(int group) => GroupColors[((group % GroupColors.Length) + GroupColors.Length) % GroupColors.Length];

        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var m = new Mesh { name = "IconQuad" };
            m.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
            };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _quad = m;
            return m;
        }

        private static Material IconMat(Texture tex)
        {
            if (tex == null) return null;
            if (_iconMats.TryGetValue(tex, out var m) && m != null) return m;
            m = new Material(Shader.Find("Sprites/Default")) { name = "Icon (runtime)" };
            m.mainTexture = tex;
            _iconMats[tex] = m;
            return m;
        }

        /// <summary>Billboard sprite ikon (kilit/anahtar). worldPos board-local; parent board transform. tint = sprite rengi çarpanı.</summary>
        public static GameObject MakeIcon(Transform parent, Sprite sprite, Vector3 worldPos, float size, string name, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = worldPos;
            go.transform.localScale = new Vector3(size, size, size);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = Quad();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = sprite != null ? IconMat(sprite.texture) : MeshFactory.SharedMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            MeshFactory.SetColor(mr, tint); // Sprites/Default _Color ile tint
            go.AddComponent<Billboard>();
            return go;
        }

        /// <summary>Belirli bir parent'a (ör. SegmentView.RotRoot) YATIK (XZ) ikon — billboard YOK; parent döndükçe döner.
        /// localPos parent-yerel. Ok'la birlikte dönmesi gereken anahtar için.</summary>
        public static GameObject MakeFlatIconOnParent(Transform parent, Sprite sprite, Vector3 localPos, float size, string name, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // XY quad → XZ düzlemine yatık (parent'ın Y dönüşüyle döner)
            go.transform.localScale = new Vector3(size, size, size);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = Quad();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = sprite != null ? IconMat(sprite.texture) : MeshFactory.SharedMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            MeshFactory.SetColor(mr, tint);
            return go;
        }

        /// <summary>Kilitli taşın üstünü kapatan yatay (XZ) hexagon lid.</summary>
        public static GameObject MakeCap(Transform parent, Vector3 worldPos, float radius, Color color)
        {
            var go = MeshFactory.NewMeshObject("LockCap", MeshFactory.Hex(radius), color, parent, worldPos);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // XY hex → XZ düzlemine yatır
            return go;
        }
    }

    /// <summary>
    /// Bir kilit grubu (SKILL: anahtar mekaniği): kilitli taşların üstünde lid'ler + grubu temsilen tek
    /// Lock ikonu (centroid'de). Anahtar çıkınca <see cref="Open"/> — ikon pop+söner, lid'ler kalkıp söner,
    /// altındaki segmentler görünür (oklar aktifleşir). Kilitliyken <see cref="Shake"/> geri bildirimi.
    /// </summary>
    public class LockGroupView : MonoBehaviour
    {
        private readonly List<GameObject> _caps = new List<GameObject>();
        private readonly List<SegmentView> _segments = new List<SegmentView>();
        private GameObject _lockIcon;
        private Vector3 _lockPos;
        private float _s;
        private Coroutine _shake;

        public Vector3 LockPos => _lockPos;

        public static LockGroupView Create(Transform parent, int group, List<(SegmentView seg, Vector3 capCenter)> cells,
                                           Sprite lockSprite, float capRadius, float iconY, float s, Color tint)
        {
            var go = new GameObject($"LockGroup_{group}");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<LockGroupView>();
            v._s = s;

            var capColor = new Color(0.24f, 0.25f, 0.30f, 1f);
            Vector3 sum = Vector3.zero;
            foreach (var (seg, center) in cells)
            {
                if (seg != null) { v._segments.Add(seg); seg.SetVisible(false); } // altındaki ok gizli
                v._caps.Add(LockKeyFx.MakeCap(go.transform, center, capRadius, capColor));
                sum += center;
            }
            // kilit ikonu: centroid'e EN YAKIN gerçek hexagonun üstünde (hücreler arası boşlukta değil)
            Vector3 centroid = cells.Count > 0 ? sum / cells.Count : Vector3.zero;
            Vector3 best = centroid; float bestD = float.MaxValue;
            foreach (var (seg, center) in cells)
            {
                float d = (center.x - centroid.x) * (center.x - centroid.x) + (center.z - centroid.z) * (center.z - centroid.z);
                if (d < bestD) { bestD = d; best = center; }
            }
            v._lockPos = new Vector3(best.x, iconY, best.z);
            v._lockIcon = LockKeyFx.MakeIcon(go.transform, lockSprite, v._lockPos, s * 0.9f, "LockIcon", tint); // grup rengi (açık ton)
            return v;
        }

        public void Shake()
        {
            if (_shake != null) StopCoroutine(_shake);
            _shake = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            const float dur = 0.32f;
            float t = 0f;
            Vector3 basePos = _lockIcon != null ? _lockIcon.transform.localPosition : Vector3.zero;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / dur);
                float amp = 0.10f * _s * (1f - t);
                float off = Mathf.Sin(t * 42f) * amp;
                if (_lockIcon != null) _lockIcon.transform.localPosition = basePos + new Vector3(off, 0f, 0f);
                yield return null;
            }
            if (_lockIcon != null) _lockIcon.transform.localPosition = basePos;
            _shake = null;
        }

        /// <summary>Kilit açılışı: ikon pop+söner, lid'ler yükselip söner, segmentler belirir. Sonra kendini yok eder.</summary>
        public void Open()
        {
            foreach (var seg in _segments) if (seg != null) seg.SetVisible(true); // oklar artık aktif/görünür
            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            float t = 0f;
            var capBase = new List<Vector3>();
            foreach (var c in _caps) capBase.Add(c != null ? c.transform.localPosition : Vector3.zero);
            Vector3 iconBase = _lockIcon != null ? _lockIcon.transform.localScale : Vector3.one;

            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / 0.35f);
                float e = t;
                // lid'ler yükselip küçülür/söner
                for (int i = 0; i < _caps.Count; i++)
                {
                    if (_caps[i] == null) continue;
                    _caps[i].transform.localPosition = capBase[i] + new Vector3(0f, e * 0.6f * _s, 0f);
                    _caps[i].transform.localScale = Vector3.one * (1f - e);
                }
                // ikon pop (büyür) sonra söner
                if (_lockIcon != null)
                    _lockIcon.transform.localScale = iconBase * (1f + 0.4f * Mathf.Sin(e * Mathf.PI));
                yield return null;
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Bağımsız ANAHTAR hexagonu: KOYU hexagon lid + üstünde grup renginde (açık ton) Key ikonu.
    /// Bir ok üstünden geçince (çarpınca) <see cref="TriggerToLock"/>: hexagon hafif yukarı çıkıp scale ile
    /// zıplar (bounce), sonra anahtar ikonu kilide yay çizerek uçar, hexagon söner. Sonra kilit açılır (onArrive).
    /// </summary>
    public class KeyCellView : MonoBehaviour
    {
        private GameObject _cap;
        private GameObject _icon;
        private float _s;
        private Vector3 _iconBaseScale;

        public static KeyCellView Create(Transform parent, int group, Vector3 worldPosY0, Sprite keySprite,
                                         float capRadius, float tileTopY, float iconY, float s, Color tint)
        {
            var go = new GameObject($"KeyCell_{group}_{worldPosY0.x:F1}_{worldPosY0.z:F1}");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<KeyCellView>();
            v._s = s;

            var capColor = new Color(0.20f, 0.21f, 0.26f, 1f); // KOYU anahtar hexagonu
            var capCenter = new Vector3(worldPosY0.x, tileTopY + 0.02f * s, worldPosY0.z);
            v._cap = LockKeyFx.MakeCap(go.transform, capCenter, capRadius, capColor);

            var iconPos = new Vector3(worldPosY0.x, iconY, worldPosY0.z);
            v._icon = LockKeyFx.MakeIcon(go.transform, keySprite, iconPos, s * 0.9f, "KeyIcon", tint);
            v._iconBaseScale = v._icon.transform.localScale;
            return v;
        }

        public void TriggerToLock(Vector3 lockPos, System.Action onArrive)
        {
            StartCoroutine(TriggerRoutine(lockPos, onArrive));
        }

        private IEnumerator TriggerRoutine(Vector3 lockPos, System.Action onArrive)
        {
            // 1) BOUNCE: hexagon + ikon hafif yukarı çıkıp scale ile zıplar (OutBack)
            Vector3 capPos0 = _cap != null ? _cap.transform.localPosition : Vector3.zero;
            Vector3 iconPos0 = _icon != null ? _icon.transform.localPosition : Vector3.zero;
            Vector3 capScale0 = _cap != null ? _cap.transform.localScale : Vector3.one;
            float rise = 0.35f * _s;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / 0.28f);
                float e = Easing.OutBack(t);
                float up = rise * e;
                float sc = 1f + 0.35f * Mathf.Sin(e * Mathf.PI); // şişip iner
                if (_cap != null) { var p = capPos0; p.y += up; _cap.transform.localPosition = p; _cap.transform.localScale = capScale0 * (1f + 0.2f * Mathf.Sin(e * Mathf.PI)); }
                if (_icon != null) { var p = iconPos0; p.y += up; _icon.transform.localPosition = p; _icon.transform.localScale = _iconBaseScale * sc; }
                yield return null;
            }

            // 2) UÇUŞ: anahtar ikonu kilide yay çizerek uçar; hexagon (cap) söner
            Vector3 from = _icon != null ? _icon.transform.localPosition : Vector3.zero;
            float t2 = 0f;
            while (t2 < 1f)
            {
                t2 = Mathf.Min(1f, t2 + Time.deltaTime / 0.5f);
                float e = Easing.InOutSine(t2);
                if (_icon != null)
                {
                    _icon.transform.localPosition = Vector3.Lerp(from, lockPos, e) + new Vector3(0f, Mathf.Sin(e * Mathf.PI) * 0.7f * _s, 0f);
                    _icon.transform.localScale = _iconBaseScale * (1f - 0.4f * e);
                }
                if (_cap != null) _cap.transform.localScale = capScale0 * (1f - e); // hexagon küçülüp kaybolur
                yield return null;
            }

            onArrive?.Invoke(); // kilidi aç
            Destroy(gameObject);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>Sprite quad'ını sürekli kameraya karşı döndürür (ikonlar okunaklı kalsın).</summary>
    public class Billboard : MonoBehaviour
    {
        private Camera _cam;
        private Vector3 _basePos;
        private float _pull;   // kameraya doğru çekme mesafesi — eğik kamerada 3D kaplamaların örtmesini engeller
        private bool _hasBase;

        /// <summary>Dünya konumunu sabitler ve her frame kameraya doğru <paramref name="pull"/> kadar çeker (occlusion önler).</summary>
        public void SetPull(Vector3 worldBasePos, float pull)
        {
            _basePos = worldBasePos;
            _pull = pull;
            _hasBase = true;
        }

        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            transform.rotation = _cam.transform.rotation;
            if (_hasBase && _pull > 0f) transform.position = _basePos - _cam.transform.forward * _pull;
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

        /// <summary>Ahşap kaplamanın dikey ince ayarı (kullanıcı değeri): taşa daha oturmuş dursun diye 0.4 birim aşağı.</summary>
        public const float CapYOffset = -0.4f;

        /// <summary>Grup renginin KOYU tonu (hue korunur, doygunluk artar, parlaklık düşer) — ikon arka planı için.</summary>
        public static Color DarkTone(Color c, float valueScale = 0.42f)
        {
            Color.RGBToHSV(c, out float h, out float sat, out float v);
            return Color.HSVToRGB(h, Mathf.Clamp01(sat * 1.35f), Mathf.Clamp01(v * valueScale));
        }

        /// <summary>İkonun ARKASINA daire arka plan (ikonun çocuğu → billboard'la birlikte döner, ölçek/animasyonu takip eder).</summary>
        public static GameObject MakeIconBackdrop(Transform icon, float radius, Color color)
        {
            // ikon quad'ı yerelde 1x1; +Z kameradan UZAĞA bakar → küçük +Z ofseti ikonu ÖNDE bırakır
            var go = MeshFactory.NewMeshObject("IconBg", MeshFactory.Circle(radius), color, icon, new Vector3(0f, 0f, 0.02f));
            // ⚠ MeshFactory.SetColor sRGB→linear çevirir; bu unlit yolda renk ikinci kez çevrilip koyulaşıyor → HAM bas
            var mr = go.GetComponent<MeshRenderer>();
            var mpb = new MaterialPropertyBlock();
            mr.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", color);
            mr.SetPropertyBlock(mpb);
            return go;
        }

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

        /// <summary>
        /// Kilitli taşın kaplaması olarak PREFAB (WoodHex_Prefab) yerleştirir: taş genişliğine ölçeklenir,
        /// ALT yüzeyi taşın üstüne oturur. Prefab'da yalnız `WoodLevel1` aktiftir; kırılma parçacıkları
        /// (`WoodBreakParticleL3`) inaktif bekler, kilit açılınca <see cref="LockGroupView.Open"/> aktifleştirir.
        /// </summary>
        public static GameObject MakeCapPrefab(Transform parent, GameObject prefab, Vector3 centerXZ, float tileTopY, float desiredWidth)
            => MakeCapPrefab(parent, prefab, centerXZ, tileTopY, desiredWidth, CapYOffset);

        /// <summary>
        /// Kaplama prefab'ını **ÖLÇEK 1** ile, hücre merkezine ve taşın ÜST YÜZEYİNE yerleştirir (otomatik
        /// bounds-fit YOK). İnce ayar (boyut/yükseklik) prefabın kendi içinden yapılır. Rastgele 60° dönüş uygulanır.
        /// </summary>
        public static GameObject MakeCapPrefabUnscaled(Transform parent, GameObject prefab, Vector3 centerXZ, float tileTopY, string name)
        {
            var go = Object.Instantiate(prefab, parent);
            go.name = name;
            go.transform.localScale = Vector3.one;                                        // ÖLÇEK 1 (kullanıcı kararı)
            go.transform.localRotation = Quaternion.Euler(0f, 60f * Random.Range(0, 6), 0f); // hexagon simetrisi → hizalama bozulmaz
            go.transform.localPosition = new Vector3(centerXZ.x, tileTopY, centerXZ.z);
            return go;
        }

        /// <summary>Kaplama prefab'ı, dikey ofset parametreli (buz/ahşap farklı pivotlara sahip olabilir).</summary>
        public static GameObject MakeCapPrefab(Transform parent, GameObject prefab, Vector3 centerXZ, float tileTopY, float desiredWidth, float yOffset)
        {
            var go = Object.Instantiate(prefab, parent);
            go.name = "LockCap";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity; // ⚠ ölçüm DÖNDÜRÜLMEMİŞ halde yapılır (aşağıda gerekçe)
            go.transform.localScale = Vector3.one;

            // ölçü: AKTİF mesh'lerin dünya bounds'u → parent-yerel
            var rends = go.GetComponentsInChildren<MeshRenderer>(false);
            if (rends.Length == 0) { go.transform.localPosition = new Vector3(centerXZ.x, tileTopY + yOffset, centerXZ.z); return go; }
            var wb = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) wb.Encapsulate(rends[i].bounds);
            Vector3 lmin = parent.InverseTransformPoint(wb.min);
            Vector3 lmax = parent.InverseTransformPoint(wb.max);
            Vector3 lc = (lmin + lmax) * 0.5f;

            // ⚠ Ölçek DÖNDÜRÜLMEMİŞ ölçüden hesaplanır → TÜM kapaklar AYNI ölçeği alır.
            // (mr.bounds dünya-AABB'si; mesh tam düzgün altıgen değil (2.45 × 2.22) → döndürülmüş halde
            //  ölçülürse AABB büyüyüp ölçek küçülüyordu: bir kapak doğru, diğerleri daha küçük çıkıyordu.)
            float w = Mathf.Max(lmax.x - lmin.x, lmax.z - lmin.z);
            float s = w > 1e-4f ? desiredWidth / w : 1f;
            go.transform.localScale = Vector3.one * s;

            // RASTGELE 60° katı dönüş — hexagon 6-kat simetrik olduğu için yine TAM oturur, deseni farklılaşır
            var rot = Quaternion.Euler(0f, 60f * Random.Range(0, 6), 0f);
            go.transform.localRotation = rot;

            // yatayda hücre merkezine hizala (merkez ofseti dönüşle birlikte döner), dikeyde alt yüzey taşın üstüne
            Vector3 rc = rot * new Vector3(lc.x, 0f, lc.z);
            go.transform.localPosition = new Vector3(
                centerXZ.x - rc.x * s,
                tileTopY - lmin.y * s - 0.04f * desiredWidth + yOffset, // lmin.y, Y-dönüşünden ETKİLENMEZ
                centerXZ.z - rc.z * s);
            return go;
        }

        /// <summary>Kilitli taşın üstünü kapatan yatay (XZ) hexagon lid.</summary>
        public static GameObject MakeCap(Transform parent, Vector3 worldPos, float radius, Color color)
        {
            var go = MeshFactory.NewMeshObject("LockCap", MeshFactory.Hex(radius), color, parent, worldPos);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // XY hex → XZ düzlemine yatır
            // ⚠ MeshFactory.SetColor sRGB→linear çevirir; bu unlit (Sprites/Default) yolda renk İKİNCİ kez
            // çevrildiği için koyu tonlar siyaha düşüyordu (0.36 gri → siyah). Rengi HAM bas.
            var mr = go.GetComponent<MeshRenderer>();
            var mpb = new MaterialPropertyBlock();
            mr.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", color);
            mr.SetPropertyBlock(mpb);
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
        private bool _prefabCaps; // true = WoodHex_Prefab kaplaması (kırılırken WoodBreakParticleL3 açılır)
        private GameObject _lockIcon;
        private Vector3 _lockPos;
        private float _s;
        private Coroutine _shake;

        public Vector3 LockPos => _lockPos;

        /// <param name="capPrefab">Kaplama prefab'ı (WoodHex_Prefab). null ise düz koyu gri hex lid'e düşer.</param>
        /// <param name="capTopY">Taşın üst yüzeyi (prefab bu yüksekliğe oturur).</param>
        /// <param name="capWidth">Kaplamanın hedef genişliği (taş genişliği).</param>
        public static LockGroupView Create(Transform parent, int group, List<(SegmentView seg, Vector3 capCenter)> cells,
                                           Sprite lockSprite, float capRadius, float iconY, float s, Color tint,
                                           GameObject capPrefab, float capTopY, float capWidth)
        {
            var go = new GameObject($"LockGroup_{group}");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<LockGroupView>();
            v._s = s;

            var capColor = new Color(0.36f, 0.36f, 0.38f, 1f); // KOYU GRİ (yedek lid; prefab yoksa)
            v._prefabCaps = capPrefab != null;
            Vector3 sum = Vector3.zero;
            foreach (var (seg, center) in cells)
            {
                if (seg != null) { v._segments.Add(seg); seg.SetVisible(false); } // altındaki ok gizli
                v._caps.Add(v._prefabCaps
                    ? LockKeyFx.MakeCapPrefab(go.transform, capPrefab, center, capTopY, capWidth)
                    : LockKeyFx.MakeCap(go.transform, center, capRadius, capColor));
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
            // Prefab kaplama düz lid'den YÜKSEK → ikon kaplamanın üstünde kalmalı (yoksa tahtanın içinde gömülür)
            float iconYFinal = iconY;
            if (v._prefabCaps)
            {
                float top = float.MinValue;
                foreach (var cap in v._caps)
                {
                    if (cap == null) continue;
                    foreach (var mr in cap.GetComponentsInChildren<MeshRenderer>(true))
                        top = Mathf.Max(top, parent.InverseTransformPoint(mr.bounds.max).y);
                }
                if (top > float.MinValue) iconYFinal = Mathf.Max(iconY, top + 0.28f * s);
            }
            v._lockPos = new Vector3(best.x, iconYFinal, best.z);
            v._lockIcon = LockKeyFx.MakeIcon(go.transform, lockSprite, v._lockPos, s * 0.9f, "LockIcon", tint); // grup rengi (açık ton)
            LockKeyFx.MakeIconBackdrop(v._lockIcon.transform, 0.62f, LockKeyFx.DarkTone(tint));                 // arkasına grup renginin KOYU tonu
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
            if (_prefabCaps) { StartCoroutine(BreakPrefabCapsRoutine()); return; }
            StartCoroutine(OpenRoutine());
        }

        /// <summary>Prefab kaplama kırılması: her kapağın mesh'i ANINDA gizlenir, içindeki
        /// `WoodBreakParticleL3` aktifleşir (taştan taşa 0.05s yayılım), ikon pop+söner.</summary>
        private IEnumerator BreakPrefabCapsRoutine()
        {
            if (_lockIcon != null) StartCoroutine(PopFadeIcon());
            foreach (var cap in _caps)
            {
                if (cap == null) continue;
                var brk = FindDeep(cap.transform, "WoodBreakParticleL3");
                foreach (var mr in cap.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = false; // tahta yok olur
                if (brk != null)
                {
                    brk.gameObject.SetActive(true);
                    foreach (var ps in brk.GetComponentsInChildren<ParticleSystem>(true)) ps.Play();
                }
                yield return new WaitForSeconds(0.05f); // taştan taşa yayılım
            }
            yield return new WaitForSeconds(1.6f); // parçacıklar sönsün
            Destroy(gameObject);
        }

        private IEnumerator PopFadeIcon()
        {
            Vector3 s0 = _lockIcon.transform.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / 0.35f);
                if (_lockIcon != null) _lockIcon.transform.localScale = s0 * (1f + 0.4f * Mathf.Sin(t * Mathf.PI)) * (1f - t);
                yield return null;
            }
            if (_lockIcon != null) _lockIcon.SetActive(false);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
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
        private Color _explodeColor = Color.white; // patlama parçacık rengi (grup tonu — koyu taş yerine okunur)

        /// <summary>tileGO = önceden kurulmuş KOYU 3D hexagon puck (board taşları gibi). iconPos = anahtar ikonunun board-yerel konumu.</summary>
        public static KeyCellView Create(Transform parent, int group, GameObject tileGO, Vector3 iconPos, Sprite keySprite, float s, Color tint)
        {
            var go = new GameObject($"KeyCell_{group}");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<KeyCellView>();
            v._s = s;

            if (tileGO != null) { tileGO.transform.SetParent(go.transform, true); v._cap = tileGO; } // gerçek 3D taş (çarpınca patlar)
            v._icon = LockKeyFx.MakeIcon(go.transform, keySprite, iconPos, s * 0.9f, "KeyIcon", tint);
            v._iconBaseScale = v._icon.transform.localScale;
            v._explodeColor = tint; // parçacıklar grup renginde (koyu taş rengi arka planda kaybolurdu)
            return v;
        }

        public void TriggerToLock(Vector3 lockPos, System.Action onArrive)
        {
            StartCoroutine(TriggerRoutine(lockPos, onArrive));
        }

        private IEnumerator TriggerRoutine(Vector3 lockPos, System.Action onArrive)
        {
            Vector3 iconPos0 = _icon != null ? _icon.transform.localPosition : Vector3.zero;

            // 1) PATLAMA (ANINDA): ok çarptığı an hexagon parçalanır — scale animasyonu YOK, taş hemen yok olur
            if (_cap != null)
            {
                TileView.Explode(_cap.transform.position, _explodeColor, _s);
                Destroy(_cap);
                _cap = null;
            }
            // 2) POP: anahtar patlamayla havaya fırlar + BELİRGİN scale taşması (OutBack → 1.7×'i aşıp oturur)
            const float PopScale = 1.7f;
            float t1 = 0f;
            Vector3 popTo = iconPos0 + new Vector3(0f, 0.55f * _s, 0f);
            while (t1 < 1f)
            {
                t1 = Mathf.Min(1f, t1 + Time.deltaTime / 0.30f);
                float e = Easing.OutBack(t1); // 1'i aşar → yaylanma hissi
                if (_icon != null)
                {
                    _icon.transform.localPosition = Vector3.LerpUnclamped(iconPos0, popTo, Mathf.Clamp01(e));
                    _icon.transform.localScale = _iconBaseScale * Mathf.LerpUnclamped(1f, PopScale, e);
                }
                yield return null;
            }

            // 3) NEFES: büyük halde kısa bir bekleme (pop okunsun)
            float t15 = 0f;
            while (t15 < 1f) { t15 = Mathf.Min(1f, t15 + Time.deltaTime / 0.10f); yield return null; }

            // 4) UÇUŞ: kilide yay çizerek gider, büyük ölçekten küçülerek
            Vector3 from = _icon != null ? _icon.transform.localPosition : Vector3.zero;
            float t2 = 0f;
            while (t2 < 1f)
            {
                t2 = Mathf.Min(1f, t2 + Time.deltaTime / 0.5f);
                float e = Easing.InOutSine(t2);
                if (_icon != null)
                {
                    _icon.transform.localPosition = Vector3.Lerp(from, lockPos, e) + new Vector3(0f, Mathf.Sin(e * Mathf.PI) * 0.7f * _s, 0f);
                    _icon.transform.localScale = _iconBaseScale * Mathf.Lerp(PopScale, 0.55f, e); // 1.7× → 0.55×
                }
                yield return null;
            }

            onArrive?.Invoke(); // kilidi aç
            Destroy(gameObject);
        }
    }
}

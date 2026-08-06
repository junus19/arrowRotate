using System.Collections;
using System.Collections.Generic;
using ArrowRotate.Core;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// XZ 3D buz katmanı (SKILL.md §5'in Depth3D karşılığı; 2D'deki <see cref="IceView"/>'ın 3D eşi).
    /// Okun her taşını saran buz bloğu (hex mesh + kullanıcı `Ice_Mat` materyali: URP Lit transparent/additive,
    /// smoothness 0.85 → camsı parlama) + okun ORTA hücresinde kalan-eşik rozeti (billboard).
    /// Buzlu taşa dokununca <see cref="Shake"/>; eşik dolunca <see cref="Break"/> — bloklar taştan taşa
    /// sırayla parçalanır (particle) ve söner.
    /// </summary>
    public class IceView3D : MonoBehaviour
    {
        private readonly List<Transform> _blocks = new List<Transform>();
        private readonly List<Vector3> _basePos = new List<Vector3>();
        private float _s;
        private bool _prefabCaps; // true = IceHex_Prefab kaplaması (kırılırken Lvl0X-Broken setleri aktifleşir)
        private Transform _badge;
        private TextMesh _badgeText;
        private Coroutine _shake;

        private static readonly Color IceTint = new Color(0.72f, 0.90f, 1f);        // kırılma parçacıkları

        // ⚠ Buz kaplama materyali (IceTile) renderQueue=3001; rozet varsayılan 3000'de kalırsa kaplama ONUN ÜSTÜNE
        // çizilip rozeti örtüyor (derinlikten bağımsız — ikisi de transparent, ZWrite yok). Rozete DAHA YÜKSEK queue.
        private static Material _badgeBgMat, _badgeTextMat;
        private static Material BadgeBgMat
        {
            get
            {
                if (_badgeBgMat == null)
                    _badgeBgMat = new Material(Shader.Find("Sprites/Default")) { name = "IceBadgeBg (runtime)", renderQueue = 3200 };
                return _badgeBgMat;
            }
        }
        private static Material BadgeTextMat(Material fontMat)
        {
            if (_badgeTextMat == null && fontMat != null)
                _badgeTextMat = new Material(fontMat) { name = "IceBadgeText (runtime)", renderQueue = 3201 };
            return _badgeTextMat;
        }
        private static readonly Color BadgeBg = new Color(0.90f, 0.95f, 1f, 1f);    // fon: maviye yakın beyaz
        private static readonly Color BadgeFg = new Color(0.07f, 0.08f, 0.11f);     // sayı: siyaha yakın

        /// <param name="cellCenters">Buzla kaplanacak taşların board-yerel merkezleri (y=0 düzleminde).</param>
        /// <param name="planeMesh">Üst buz katmanı (UV'li <see cref="MeshFactory.HexPlaneXZ"/>) · <paramref name="iceMat"/> = Ice_Mat.</param>
        /// <param name="bodyMesh">Buz GÖVDESİ (EP puck; Ice_Body texture kullanmadığı için UV gerekmez) — null ise gövde çizilmez.</param>
        /// <param name="capPrefab">Buz kaplama prefab'ı (IceHex_Prefab). Verilirse prosedürel gövde/plane YERİNE bu kullanılır.</param>
        public static IceView3D Create(Transform parent, int arrowId, List<Vector3> cellCenters, float s, int remaining,
                                       Mesh planeMesh, Material iceMat, Vector3 planeScale, float planeY,
                                       Mesh bodyMesh, Material bodyMat, Vector3 bodyScale, float bodyY,
                                       float badgeY,
                                       GameObject capPrefab = null, float capTopY = 0f, float capWidth = 1f)
        {
            var go = new GameObject($"Ice3D_{arrowId}");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<IceView3D>();
            v._s = s;
            v._prefabCaps = capPrefab != null;

            foreach (var c in cellCenters)
            {
                if (v._prefabCaps)
                {
                    // PREFAB kaplama (IceHex_Prefab): sağlam taş görünür, Lvl0X-Broken setleri kırılmada aktifleşir
                    // ⚠ ÖLÇEK 1 (kullanıcı kararı): otomatik bounds-fit YOK — boyut/yükseklik ayarı prefabın içinden
                    // yapılır. Kaplama hücre merkezine, taşın üst yüzeyine oturur; rastgele 60° döner.
                    var cap = LockKeyFx.MakeCapPrefabUnscaled(go.transform, capPrefab, c, capTopY, "IceCap");
                    v._blocks.Add(cap.transform);
                    v._basePos.Add(cap.transform.localPosition);
                    continue;
                }
                // 1) GÖVDE: taşı saran buz kütlesi (Ice_Body) — mevcut hexagonun yerinde ikinci bir hexagon
                if (bodyMesh != null && bodyMat != null)
                    v.AddPiece(go.transform, "IceBody", new Vector3(c.x, bodyY, c.z), bodyScale, bodyMesh, bodyMat);
                // 2) ÜST KATMAN: UV'li plane (Ice_Mat — buz dokusu/çatlaklar)
                v.AddPiece(go.transform, "IceBlock", new Vector3(c.x, planeY, c.z), planeScale, planeMesh, iceMat);
            }

            // prefab kaplama düz plane'den yüksek → rozet kaplamanın üstüne alınır
            if (v._prefabCaps)
            {
                float top = float.MinValue;
                foreach (var b in v._blocks)
                    foreach (var mr in b.GetComponentsInChildren<MeshRenderer>(true))
                        top = Mathf.Max(top, parent.InverseTransformPoint(mr.bounds.max).y);
                // ⚠ Kamera EĞİK bakıyor → rozet kaplamanın tepesinin biraz üstünde kalırsa buz kubbesi onu örtüyor.
                // Bol pay bırak (0.85·s) ki rozet kubbenin üstünde net dursun.
                if (top > float.MinValue) badgeY = Mathf.Max(badgeY, top + 0.85f * s);
            }

            // rozet: orta hücrenin üstünde, kameraya dönük
            if (cellCenters.Count > 0)
            {
                var mid = cellCenters[cellCenters.Count / 2];
                var badgePos = new Vector3(mid.x, badgeY, mid.z);
                var disc = MeshFactory.NewMeshObject("IceBadge", MeshFactory.Circle(0.52f * s), BadgeBg, go.transform, badgePos); // büyütüldü (0.34 → 0.52)
                disc.GetComponent<MeshRenderer>().sharedMaterial = BadgeBgMat; // kaplamanın ÜSTÜNDE çizilsin (queue 3200)
                var bb = disc.AddComponent<Billboard>();
                // ek güvence: rozeti kameraya doğru çek (derinlik tabanlı örtmelere karşı)
                if (v._prefabCaps) bb.SetPull(disc.transform.position, 1.0f * s);
                v._badge = disc.transform;

                var textGo = new GameObject("Num");
                textGo.transform.SetParent(v._badge, false);
                textGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                var tm = textGo.AddComponent<TextMesh>();
                tm.text = Mathf.Max(0, remaining).ToString();
                tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var tmr = tm.GetComponent<MeshRenderer>();
                tmr.sharedMaterial = BadgeTextMat(tm.font.material) ?? tm.font.material; // queue 3201 → en üstte
                tm.fontSize = 64;
                tm.fontStyle = FontStyle.Bold;
                tm.characterSize = 0.11f;      // kullanıcı değeri (CellSize'dan bağımsız mutlak boyut)
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = BadgeFg;
                v._badgeText = tm;
            }
            return v;
        }

        /// <summary>Buz parçası (gövde ya da üst katman) oluşturur; shake/break listesine kaydeder.
        /// ⚠ Renk MPB ile BASILMAZ — materyalin (Ice_Mat / Ice_Body) kendi görünümü korunur.</summary>
        private void AddPiece(Transform parent, string name, Vector3 localPos, Vector3 scale, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _blocks.Add(go.transform);
            _basePos.Add(localPos);
        }

        /// <summary>Kalan gereken çıkış sayısı (freezeAt − exitedCount).</summary>
        public void SetRemaining(int remaining)
        {
            if (_badgeText != null) _badgeText.text = Mathf.Max(0, remaining).ToString();
        }

        /// <summary>Buzlu taşa dokunuş geri bildirimi (~300ms titreme). Hamle sayılmaz.</summary>
        public void Shake()
        {
            if (_shake != null) StopCoroutine(_shake);
            _shake = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            const float dur = 0.3f;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / dur);
                float off = Mathf.Sin(t * 40f) * (0.06f * _s * (1f - t));
                for (int i = 0; i < _blocks.Count; i++)
                    if (_blocks[i] != null) _blocks[i].localPosition = _basePos[i] + new Vector3(off, 0f, 0f);
                yield return null;
            }
            for (int i = 0; i < _blocks.Count; i++)
                if (_blocks[i] != null) _blocks[i].localPosition = _basePos[i];
            _shake = null;
        }

        /// <summary>Eşik doldu: bloklar taştan taşa sırayla parçalanır (particle) + söner, sonra yok olur.</summary>
        public void Break() => StartCoroutine(_prefabCaps ? BreakPrefabRoutine() : BreakRoutine());

        /// <summary>Prefab kaplama kırılması: sağlam taş mesh'i gizlenir, `Lvl01/02/03-Broken` setleri
        /// (kırık parçalar + RockBreakParticles) aktifleşip oynar; taştan taşa 0.05s yayılım.</summary>
        private IEnumerator BreakPrefabRoutine()
        {
            if (_badge != null) StartCoroutine(FadeBadge());

            foreach (var cap in _blocks)
            {
                if (cap == null) continue;
                // AKTİF mesh'ler = sağlam taş (kırık setler henüz inaktif) → gizle
                foreach (var mr in cap.GetComponentsInChildren<MeshRenderer>(false)) mr.enabled = false;

                foreach (var brokenName in BrokenSets)
                {
                    var t = FindDeep(cap, brokenName);
                    if (t == null) continue;
                    t.gameObject.SetActive(true);
                    foreach (var ps in t.GetComponentsInChildren<ParticleSystem>(true)) ps.Play();
                }
                yield return new WaitForSeconds(0.05f); // taştan taşa yayılım
            }
            yield return new WaitForSeconds(1.8f); // parçalar/parçacıklar sönsün
            Destroy(gameObject);
        }

        private static readonly string[] BrokenSets = { "Lvl01-Broken", "Lvl02-Broken", "Lvl03-Broken" };

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

        private IEnumerator BreakRoutine()
        {
            if (_badge != null) StartCoroutine(FadeBadge());

            for (int i = 0; i < _blocks.Count; i++)
            {
                var b = _blocks[i];
                if (b != null)
                {
                    TileView.Explode(b.position, IceTint, _s); // buz kırığı parçacıkları
                    StartCoroutine(ShrinkBlock(b));
                }
                yield return new WaitForSeconds(0.05f); // taştan taşa yayılım
            }
            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }

        private static IEnumerator ShrinkBlock(Transform b)
        {
            Vector3 s0 = b.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / 0.18f);
                if (b == null) yield break;
                b.localScale = s0 * (1f - t);
                yield return null;
            }
            if (b != null) b.gameObject.SetActive(false);
        }

        private IEnumerator FadeBadge()
        {
            var mr = _badge.GetComponent<MeshRenderer>();
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / 0.3f);
                var c = BadgeBg; c.a = BadgeBg.a * (1f - t);
                if (mr != null) MeshFactory.SetColor(mr, c);
                if (_badgeText != null) { var tc = BadgeFg; tc.a = 1f - t; _badgeText.color = tc; }
                yield return null;
            }
            if (_badge != null) _badge.gameObject.SetActive(false);
        }
    }
}

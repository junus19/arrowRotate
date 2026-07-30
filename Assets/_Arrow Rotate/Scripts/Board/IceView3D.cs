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
        private Transform _badge;
        private TextMesh _badgeText;
        private Coroutine _shake;

        private static readonly Color IceTint = new Color(0.72f, 0.90f, 1f);        // kırılma parçacıkları
        private static readonly Color BadgeBg = new Color(0.90f, 0.95f, 1f, 1f);    // fon: maviye yakın beyaz
        private static readonly Color BadgeFg = new Color(0.07f, 0.08f, 0.11f);     // sayı: siyaha yakın

        /// <param name="cellCenters">Buzla kaplanacak taşların board-yerel merkezleri (y=0 düzleminde).</param>
        /// <param name="planeMesh">Üst buz katmanı (UV'li <see cref="MeshFactory.HexPlaneXZ"/>) · <paramref name="iceMat"/> = Ice_Mat.</param>
        /// <param name="bodyMesh">Buz GÖVDESİ (EP puck; Ice_Body texture kullanmadığı için UV gerekmez) — null ise gövde çizilmez.</param>
        public static IceView3D Create(Transform parent, int arrowId, List<Vector3> cellCenters, float s, int remaining,
                                       Mesh planeMesh, Material iceMat, Vector3 planeScale, float planeY,
                                       Mesh bodyMesh, Material bodyMat, Vector3 bodyScale, float bodyY,
                                       float badgeY)
        {
            var go = new GameObject($"Ice3D_{arrowId}");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<IceView3D>();
            v._s = s;

            foreach (var c in cellCenters)
            {
                // 1) GÖVDE: taşı saran buz kütlesi (Ice_Body) — mevcut hexagonun yerinde ikinci bir hexagon
                if (bodyMesh != null && bodyMat != null)
                    v.AddPiece(go.transform, "IceBody", new Vector3(c.x, bodyY, c.z), bodyScale, bodyMesh, bodyMat);
                // 2) ÜST KATMAN: UV'li plane (Ice_Mat — buz dokusu/çatlaklar)
                v.AddPiece(go.transform, "IceBlock", new Vector3(c.x, planeY, c.z), planeScale, planeMesh, iceMat);
            }

            // rozet: orta hücrenin üstünde, kameraya dönük
            if (cellCenters.Count > 0)
            {
                var mid = cellCenters[cellCenters.Count / 2];
                var badgePos = new Vector3(mid.x, badgeY, mid.z);
                var disc = MeshFactory.NewMeshObject("IceBadge", MeshFactory.Circle(0.52f * s), BadgeBg, go.transform, badgePos); // büyütüldü (0.34 → 0.52)
                disc.AddComponent<Billboard>();
                v._badge = disc.transform;

                var textGo = new GameObject("Num");
                textGo.transform.SetParent(v._badge, false);
                textGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                var tm = textGo.AddComponent<TextMesh>();
                tm.text = Mathf.Max(0, remaining).ToString();
                tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                tm.GetComponent<MeshRenderer>().material = tm.font.material;
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
        public void Break() => StartCoroutine(BreakRoutine());

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

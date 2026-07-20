using System.Collections;
using System.Collections.Generic;
using ArrowRotate.Core;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Bir okun buz katmanı (SKILL.md §5): taşların üstünde yarı saydam açık mavi hex + ince
    /// çatlak çizgileri; okun ORTA hücresinde kalan-eşik rozeti. Buzlu taşa dokununca shake (~300ms).
    /// Kırılma: her taşın buzu 6 üçgen parçaya ayrılır, parçalar dışa savrulur/döner/küçülür/söner
    /// (~650ms), taştan taşa ~50ms yayılır. Rozet solup kaybolur.
    /// </summary>
    public class IceView : MonoBehaviour
    {
        // Buz renkleri temadan okunur (HexaThemeData.Active), Create'te doldurulur.
        private Color _fill, _edge, _crack, _badgeBg, _badgeTextColor;

        private const float IceZ = -0.15f;
        private const float BadgeZ = -0.18f;

        private readonly List<(Transform root, Vector3 center)> _tiles = new List<(Transform, Vector3)>();
        private float _s;
        private TextMesh _badgeText;
        private Transform _badge;
        private Coroutine _shake;

        public static IceView Create(Transform parent, HexaLevel level, Arrow arrow, float s)
        {
            var go = new GameObject($"Ice_{arrow.ArrowId}");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<IceView>();
            view._s = s;

            var theme = HexaThemeData.Active;
            view._fill = theme.IceFill;
            view._edge = theme.IceEdge;
            view._crack = theme.IceCrack;
            view._badgeBg = theme.IceBadgeBg;
            view._badgeTextColor = theme.IceBadgeText;

            foreach (var pos in arrow.Cells)
            {
                var cell = level.GetCell(pos);
                var (x, y) = HexMetrics.Center(cell.Q, cell.R, s);
                var center = new Vector3(x, y, IceZ);

                var tileRoot = new GameObject("IceTile").transform;
                tileRoot.SetParent(go.transform, false);
                tileRoot.localPosition = center;

                MeshFactory.NewMeshObject("Fill", MeshFactory.Hex(s * 0.91f), view._fill, tileRoot, Vector3.zero);
                view.AddOutline(tileRoot, s);
                view.AddCracks(tileRoot, s, cell.Q * 73856093 ^ cell.R * 19349663);

                view._tiles.Add((tileRoot, center));
            }

            // rozet: orta hücre
            var midPos = arrow.Cells[arrow.Cells.Count / 2];
            var midCell = level.GetCell(midPos);
            var (bx, by) = HexMetrics.Center(midCell.Q, midCell.R, s);
            view.BuildBadge(new Vector3(bx, by, BadgeZ), s, arrow.FreezeAt);

            return view;
        }

        private void AddOutline(Transform parent, float s)
        {
            var go = new GameObject("Edge");
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.material = MeshFactory.SharedMaterial;
            lr.startColor = _edge;
            lr.endColor = _edge;
            lr.widthMultiplier = 0.035f * s;
            lr.loop = true;
            lr.positionCount = 6;
            float r = s * 0.91f;
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(r * Mathf.Cos(a), r * Mathf.Sin(a), 0f));
            }
        }

        private void AddCracks(Transform parent, float s, int hash)
        {
            // deterministik 2 kısa çatlak (hücre koordinat hash'inden)
            var rng = new Mulberry32(hash);
            for (int c = 0; c < 2; c++)
            {
                var go = new GameObject("Crack");
                go.transform.SetParent(parent, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.material = MeshFactory.SharedMaterial;
                lr.startColor = _crack;
                lr.endColor = _crack;
                lr.widthMultiplier = 0.02f * s;
                lr.positionCount = 3;
                float a0 = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float len = s * (0.35f + 0.25f * (float)rng.NextDouble());
                var p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * (s * 0.15f);
                var p1 = p0 + new Vector3(Mathf.Cos(a0 + 0.5f), Mathf.Sin(a0 + 0.5f), 0f) * (len * 0.5f);
                var p2 = p1 + new Vector3(Mathf.Cos(a0 - 0.4f), Mathf.Sin(a0 - 0.4f), 0f) * (len * 0.5f);
                lr.SetPosition(0, p0);
                lr.SetPosition(1, p1);
                lr.SetPosition(2, p2);
            }
        }

        private void BuildBadge(Vector3 pos, float s, int remaining)
        {
            var badgeGo = MeshFactory.NewMeshObject("Badge", MeshFactory.Circle(0.34f * s), _badgeBg, transform, pos);
            _badge = badgeGo.transform;

            var textGo = new GameObject("Num");
            textGo.transform.SetParent(_badge, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            _badgeText = textGo.AddComponent<TextMesh>();
            _badgeText.text = remaining.ToString();
            _badgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _badgeText.GetComponent<MeshRenderer>().material = _badgeText.font.material;
            _badgeText.fontSize = 64;
            _badgeText.fontStyle = FontStyle.Bold;
            _badgeText.characterSize = 0.011f * s;
            _badgeText.anchor = TextAnchor.MiddleCenter;
            _badgeText.alignment = TextAlignment.Center;
            _badgeText.color = _badgeTextColor;
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
                float amp = 0.06f * _s * (1f - t);
                float off = Mathf.Sin(t * 40f) * amp;
                foreach (var (root, center) in _tiles)
                    if (root != null) root.localPosition = center + new Vector3(off, 0f, 0f);
                yield return null;
            }
            foreach (var (root, center) in _tiles)
                if (root != null) root.localPosition = center;
            _shake = null;
        }

        /// <summary>Eşik doldu: kırılma FX'i, sonra kendini yok eder.</summary>
        public void Break()
        {
            StartCoroutine(BreakRoutine());
        }

        private IEnumerator BreakRoutine()
        {
            if (_badge != null) StartCoroutine(FadeBadge());

            for (int i = 0; i < _tiles.Count; i++)
            {
                var (root, center) = _tiles[i];
                if (root != null)
                {
                    SpawnShards(center, i);
                    root.gameObject.SetActive(false);
                }
                yield return new WaitForSeconds(0.05f); // taştan taşa yayılım
            }
            yield return new WaitForSeconds(0.7f);
            Destroy(gameObject);
        }

        private IEnumerator FadeBadge()
        {
            var mr = _badge.GetComponent<MeshRenderer>();
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / 0.3f);
                var c = _badgeBg; c.a = 1f - t;
                MeshFactory.SetColor(mr, c);
                if (_badgeText != null)
                {
                    var tc = _badgeTextColor; tc.a = 1f - t;
                    _badgeText.color = tc;
                }
                yield return null;
            }
            _badge.gameObject.SetActive(false);
        }

        private void SpawnShards(Vector3 center, int tileIndex)
        {
            float r = _s * 0.91f;
            for (int i = 0; i < 6; i++)
            {
                float a1 = i * 60f * Mathf.Deg2Rad;
                float a2 = (i + 1) * 60f * Mathf.Deg2Rad;
                var c1 = new Vector3(r * Mathf.Cos(a1), r * Mathf.Sin(a1), 0f);
                var c2 = new Vector3(r * Mathf.Cos(a2), r * Mathf.Sin(a2), 0f);

                var mesh = new Mesh();
                mesh.vertices = new[] { Vector3.zero, c1, c2 };
                mesh.triangles = new[] { 0, 2, 1 };
                mesh.RecalculateBounds();

                var shard = MeshFactory.NewMeshObject("Shard", mesh, _fill, transform, center);
                var dir = ((c1 + c2) * 0.5f).normalized;
                float dist = _s * (0.7f + 0.066f * ((tileIndex * 7 + i * 3) % 7)); // ~0.7-1.1·S deterministik
                float rot = (i % 2 == 0 ? 1f : -1f) * (55f + (i * 13 + tileIndex * 5) % 6);
                StartCoroutine(ShardRoutine(shard, dir * dist, rot));
            }
        }

        private IEnumerator ShardRoutine(GameObject shard, Vector3 delta, float rotDeg)
        {
            const float dur = 0.65f;
            var mr = shard.GetComponent<MeshRenderer>();
            var start = shard.transform.localPosition;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / dur);
                float e = Easing.OutCubic(t);
                shard.transform.localPosition = start + delta * e;
                shard.transform.localRotation = Quaternion.Euler(0f, 0f, rotDeg * e);
                shard.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, e);
                var c = _fill; c.a = _fill.a * (1f - t);
                MeshFactory.SetColor(mr, c);
                yield return null;
            }
            Destroy(shard);
        }
    }
}

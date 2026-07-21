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
        private List<Vector2> _pts;   // planar yol (+uzatma); planar (x,y) → dünya (X,Z)
        private float[] _cum;
        private float _total, _bodyLen;
        private float _s;
        private MeshFilter _stripMF;
        private Transform _head;
        private float _headLen;

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

            var material = mat != null ? mat : MeshFactory.Lit3DTransparent;

            var stripGo = new GameObject("Body");
            stripGo.transform.SetParent(go.transform, false);
            fr._stripMF = stripGo.AddComponent<MeshFilter>();
            var smr = stripGo.AddComponent<MeshRenderer>();
            smr.sharedMaterial = material;
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            smr.receiveShadows = false;
            MeshFactory.SetColor(smr, HexaPalette.Segment);

            fr._headLen = SegmentMesh3D.HeadLength * s;
            var headGo = new GameObject("Head");
            headGo.transform.SetParent(go.transform, false);
            var hmf = headGo.AddComponent<MeshFilter>();
            hmf.sharedMesh = SegmentMesh3D.BuildArrowhead(
                fr._headLen, SegmentMesh3D.HeadHalfWidth * s,
                SegmentMesh3D.Height * s, SegmentMesh3D.Fillet * s, SegmentMesh3D.HeadCornerRadius * s);
            var hmr = headGo.AddComponent<MeshRenderer>();
            hmr.sharedMaterial = material;
            hmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hmr.receiveShadows = false;
            MeshFactory.SetColor(hmr, HexaPalette.Segment);
            fr._head = headGo.transform;

            fr.SetOffset(0f);
            return fr;
        }

        public void Fly(float speed, Action onDone) => StartCoroutine(FlyRoutine(speed, onDone));

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

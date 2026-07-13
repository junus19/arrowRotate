using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Uçuş/bounce overlay'i — prototipteki stroke-dash "kayan pencere" tekniğinin karşılığı.
    /// Ok gövdesi, tam yol polyline'ı üzerinde [tail, tip] penceresi olarak çizilir;
    /// pencere ilerledikçe ok yol boyunca süzülür. Uç üçgeni yol yönüne bakar, kuyruk noktası
    /// pencerenin gerisinde. Uçuş: d 0→total (hız sabit). Bounce: d = hitDist·sin(π·t).
    /// </summary>
    public class FlightRenderer : MonoBehaviour
    {
        private List<Vector2> _pts;
        private float[] _cum;         // kümülatif yay uzunlukları
        private float _total;         // uzatma dahil toplam
        private float _bodyLen;       // uzatma HARİÇ gövde uzunluğu
        private LineRenderer _line;
        private Transform _tri;
        private Transform _dot;
        private float _z;

        public static FlightRenderer Create(List<(float x, float y)> pathPts, float s, float extension, float z)
        {
            var go = new GameObject("Flight");
            var fr = go.AddComponent<FlightRenderer>();
            fr._z = z;

            // uzatma: son noktadan son segment yönünde ileri
            var pts = new List<Vector2>(pathPts.Count + 1);
            foreach (var (x, y) in pathPts) pts.Add(new Vector2(x, y));
            var last = pts[pts.Count - 1];
            var prev = pts[pts.Count - 2];
            var dir = (last - prev).normalized;
            pts.Add(last + dir * extension);
            fr._pts = pts;

            fr._cum = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++)
                fr._cum[i] = fr._cum[i - 1] + Vector2.Distance(pts[i - 1], pts[i]);
            fr._total = fr._cum[fr._cum.Length - 1];
            fr._bodyLen = fr._total - extension;

            float lw = 0.153f * s;
            var lineGo = new GameObject("Body");
            lineGo.transform.SetParent(go.transform, false);
            fr._line = lineGo.AddComponent<LineRenderer>();
            fr._line.useWorldSpace = true;
            fr._line.material = MeshFactory.SharedMaterial;
            fr._line.startColor = HexaPalette.Segment;
            fr._line.endColor = HexaPalette.Segment;
            fr._line.widthMultiplier = lw;
            fr._line.numCapVertices = 6;
            fr._line.numCornerVertices = 6;

            fr._tri = MeshFactory.NewMeshObject("Tip", MeshFactory.Triangle(0.41f * s, 0.25f * s), HexaPalette.Segment, go.transform, Vector3.zero).transform;
            fr._dot = MeshFactory.NewMeshObject("TailDot", MeshFactory.Circle(0.176f * s), HexaPalette.Segment, go.transform, Vector3.zero).transform;

            fr.SetOffset(0f);
            return fr;
        }

        /// <summary>Sabit hızla tüm yol boyunca uç ve kaybol (prototip: 0.55px/ms @S=34 ⇒ ~16.2·S/sn).</summary>
        public void Fly(float speed, Action onDone)
        {
            StartCoroutine(FlyRoutine(speed, onDone));
        }

        private System.Collections.IEnumerator FlyRoutine(float speed, Action onDone)
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

        /// <summary>Çarpıp geri dönme: ileri hitDist kadar gidip sin eğrisiyle döner (~560ms).</summary>
        public void Bounce(float hitDist, float duration, Action onDone)
        {
            StartCoroutine(BounceRoutine(hitDist, duration, onDone));
        }

        private System.Collections.IEnumerator BounceRoutine(float hitDist, float duration, Action onDone)
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

        /// <summary>Pencere: kuyruk=d, uç=min(bodyLen+d, total). Prototip setOffset eşleniği.</summary>
        private void SetOffset(float d)
        {
            float tailL = Mathf.Clamp(d, 0f, _total);
            float tipL = Mathf.Min(_bodyLen + d, _total);

            var window = new List<Vector3>();
            window.Add(WithZ(PointAt(tailL)));
            for (int i = 0; i < _pts.Count; i++)
            {
                if (_cum[i] > tailL && _cum[i] < tipL)
                    window.Add(WithZ(_pts[i]));
            }
            window.Add(WithZ(PointAt(tipL)));

            _line.positionCount = window.Count;
            _line.SetPositions(window.ToArray());

            var tipPos = PointAt(tipL);
            var tipDir = DirAt(tipL);
            _tri.position = WithZ(tipPos);
            _tri.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tipDir.y, tipDir.x) * Mathf.Rad2Deg);

            _dot.position = WithZ(PointAt(tailL));
            _dot.gameObject.SetActive(tailL < _total - 0.05f);
        }

        private Vector3 WithZ(Vector2 p) => new Vector3(p.x, p.y, _z);

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
    }
}

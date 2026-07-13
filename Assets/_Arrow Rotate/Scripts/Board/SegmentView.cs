using System.Collections;
using ArrowRotate.Core;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Hücre üstündeki BEYAZ segment görseli. Lokal a/b yönlerinde çizilir; rot, RotRoot'un
    /// z-dönüşüyle uygulanır (tap başına ekranda saat yönü 60° = Unity z'de -60°).
    /// Mid daima MERKEZDEN kırık çizgi — kenar ortalarını düz bağlamak yasak (SKILL.md tuzağı).
    /// Dönüş: ~160ms hafif overshoot; ardışık taplar hedef açıda birikir (debounce yok).
    /// </summary>
    public class SegmentView : MonoBehaviour
    {
        public const float RotateDuration = 0.16f;

        private Transform _rotRoot;
        private float _targetAngle;
        private float _currentAngle; // birikimli sürekli açı — localEulerAngles'ın 0..360 sarmasından bağımsız
        private Coroutine _anim;

        public static SegmentView Create(Transform parent, Vector3 pos, float s, Cell cell)
        {
            var go = new GameObject("Segment");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var view = go.AddComponent<SegmentView>();

            var rot = new GameObject("Rot");
            rot.transform.SetParent(go.transform, false);
            view._rotRoot = rot.transform;

            view.BuildShape(s, cell);
            view.SetInstantRot(cell.Rot);
            return view;
        }

        private void BuildShape(float s, Cell cell)
        {
            float apo = HexMetrics.Apothem(s);
            float lw = 0.153f * s;
            Vector3 EdgePoint(int d)
            {
                float a = HexMetrics.DirAngleDeg(d) * Mathf.Deg2Rad;
                return new Vector3(apo * Mathf.Cos(a), apo * Mathf.Sin(a), 0f);
            }

            switch (cell.Type)
            {
                case CellType.Tail:
                {
                    AddLine(lw, Vector3.zero, EdgePoint(cell.B));
                    MeshFactory.NewMeshObject("Dot", MeshFactory.Circle(0.176f * s), HexaPalette.Segment, _rotRoot, Vector3.zero);
                    break;
                }
                case CellType.Mid:
                {
                    AddLine(lw, EdgePoint(cell.A), Vector3.zero, EdgePoint(cell.B));
                    break;
                }
                case CellType.Head:
                {
                    float tipDist = 0.62f * apo;
                    float angB = HexMetrics.DirAngleDeg(cell.B) * Mathf.Deg2Rad;
                    var tip = new Vector3(tipDist * Mathf.Cos(angB), tipDist * Mathf.Sin(angB), 0f);
                    AddLine(lw, EdgePoint(cell.A), Vector3.zero, tip);
                    var tri = MeshFactory.NewMeshObject("Head", MeshFactory.Triangle(0.41f * s, 0.25f * s), HexaPalette.Segment, _rotRoot, tip);
                    tri.transform.localRotation = Quaternion.Euler(0f, 0f, HexMetrics.DirAngleDeg(cell.B));
                    break;
                }
            }
        }

        private void AddLine(float width, params Vector3[] points)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(_rotRoot, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.material = MeshFactory.SharedMaterial;
            lr.startColor = HexaPalette.Segment;
            lr.endColor = HexaPalette.Segment;
            lr.widthMultiplier = width;
            lr.numCapVertices = 6;
            lr.numCornerVertices = 6;
            lr.positionCount = points.Length;
            lr.SetPositions(points);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }

        /// <summary>Bir tap'lik dönüşü başlatır/birleştirir. Veri rot'u çağıran taraf günceller.</summary>
        public void RotateOneStep()
        {
            _targetAngle -= 60f;
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(RotateRoutine());
        }

        public void SetInstantRot(int rot)
        {
            if (_anim != null) { StopCoroutine(_anim); _anim = null; }
            _targetAngle = HexMetrics.RotationZDeg(rot);
            _currentAngle = _targetAngle;
            _rotRoot.localRotation = Quaternion.Euler(0f, 0f, _targetAngle);
        }

        public void SetVisible(bool visible) => _rotRoot.gameObject.SetActive(visible);

        private IEnumerator RotateRoutine()
        {
            float from = _currentAngle;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / RotateDuration);
                _currentAngle = Mathf.LerpUnclamped(from, _targetAngle, Easing.OutBack(t));
                _rotRoot.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);
                yield return null;
            }
            _anim = null;
        }
    }
}

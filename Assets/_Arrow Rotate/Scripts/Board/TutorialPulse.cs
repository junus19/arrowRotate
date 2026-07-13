using System.Collections;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Tutorial işaretçisi: hedef hücrede büyüyüp sönen beyaz halka (tap daveti).
    /// Sprite asset'i gerektirmez; el görseli art pass'te eklenir.
    /// </summary>
    public class TutorialPulse : MonoBehaviour
    {
        private LineRenderer _ring;
        private float _s;

        public static TutorialPulse Create(Transform parent, Vector3 worldPos, float s)
        {
            var go = new GameObject("TutorialPulse");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(worldPos.x, worldPos.y, -0.25f);
            var pulse = go.AddComponent<TutorialPulse>();
            pulse._s = s;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.material = MeshFactory.SharedMaterial;
            lr.loop = true;
            lr.widthMultiplier = 0.07f * s;
            const int segments = 32;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
            }
            pulse._ring = lr;
            pulse.StartCoroutine(pulse.PulseRoutine());
            return pulse;
        }

        private IEnumerator PulseRoutine()
        {
            const float dur = 0.9f;
            while (true)
            {
                float t = 0f;
                while (t < 1f)
                {
                    t = Mathf.Min(1f, t + Time.deltaTime / dur);
                    float scale = Mathf.Lerp(0.55f, 1.15f, Easing.OutCubic(t)) * _s;
                    transform.localScale = new Vector3(scale, scale, 1f);
                    var c = Color.white; c.a = 1f - t * 0.9f;
                    _ring.startColor = c;
                    _ring.endColor = c;
                    yield return null;
                }
                yield return new WaitForSeconds(0.25f);
            }
        }
    }
}

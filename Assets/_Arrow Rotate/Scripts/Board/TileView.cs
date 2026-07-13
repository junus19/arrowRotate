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
        private Color _color;
        private float _alpha = 1f;
        private Coroutine _fade;

        public static TileView Create(Transform parent, Vector3 pos, float s, Color color)
        {
            float inset = s * 0.09f; // proto: S-3 @ S=34
            var go = MeshFactory.NewMeshObject("Tile", MeshFactory.Hex(s - inset), color, parent, pos);
            var view = go.AddComponent<TileView>();
            view._renderer = go.GetComponent<MeshRenderer>();
            view._color = color;
            return view;
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
                transform.localRotation = Quaternion.Euler(0f, 0f, -30f * e); // ekranda saat yönü
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
            MeshFactory.SetColor(_renderer, c);
        }
    }
}

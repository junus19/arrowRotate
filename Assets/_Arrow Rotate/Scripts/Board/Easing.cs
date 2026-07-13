using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>Prototipteki CSS easing'lerin karşılıkları.</summary>
    public static class Easing
    {
        /// <summary>Hafif overshoot (dönüş animasyonu — proto: cubic-bezier(.3,.9,.4,1.05)).</summary>
        public static float OutBack(float t, float overshoot = 1.2f)
        {
            float c3 = overshoot + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + overshoot * u * u;
        }

        public static float OutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
    }
}

using System;

namespace ArrowRotate.Core
{
    /// <summary>
    /// Hex geometrisi — Unity dünya koordinatında (y YUKARI).
    ///
    /// Y-EKSENİ KARARI (SKILL.md §2): Prototip SVG'de y aşağı bakar ve "ekranda saat yönü dönüş"
    /// açı ARTIŞIDIR. Unity'de y yukarı baktığından prototip açıları AYNALANIR:
    ///   unityAngle(d) = -(30 + 60·d) derece, hücre merkezi y'si negatiflenir,
    ///   her tap görsel olarak z ekseninde -60° uygular (ekranda saat yönü korunur).
    /// Bu sözleşme HexMetricsTests ile sabitlenmiştir — DEĞİŞTİRME.
    /// </summary>
    public static class HexMetrics
    {
        public const float Sqrt3 = 1.7320508f;

        /// <summary>Kenar orta uzaklığı (apothem) = √3/2 · S.</summary>
        public static float Apothem(float s) => Sqrt3 * 0.5f * s;

        /// <summary>Hücre merkezi (Unity, y yukarı). Prototip: x=1.5·S·q, y_svg=√3·S·(r+q/2).</summary>
        public static (float x, float y) Center(int q, int r, float s)
            => (1.5f * s * q, -Sqrt3 * s * (r + q * 0.5f));

        /// <summary>d yönünün Unity dünya açısı (derece). Prototipteki 30+60d'nin aynası.</summary>
        public static float DirAngleDeg(int d) => -(30f + 60f * d);

        /// <summary>Kenar orta noktası (Unity koordinatı).</summary>
        public static (float x, float y) EdgeMid(int q, int r, int d, float s)
        {
            var (cx, cy) = Center(q, r, s);
            float a = DirAngleDeg(d) * (float)Math.PI / 180f;
            float apo = Apothem(s);
            return (cx + apo * (float)Math.Cos(a), cy + apo * (float)Math.Sin(a));
        }

        /// <summary>Bir tap'in görsel dönüş katkısı: ekranda saat yönü 60° = Unity z'de -60°.</summary>
        public static float RotationZDeg(int rot) => -60f * rot;

        /// <summary>Unity dünya noktası → en yakın axial hücre (fizik raycast'siz tap seçimi).</summary>
        public static (int q, int r) WorldToAxial(float x, float y, float s)
        {
            float qf = x / (1.5f * s);
            float ySvg = -y;                       // Unity y-yukarı → prototip y-aşağı
            float rf = ySvg / (Sqrt3 * s) - qf * 0.5f;
            return AxialRound(qf, rf);
        }

        /// <summary>Küp koordinatta yuvarlama: en büyük sapan eksen düzeltilir.</summary>
        public static (int q, int r) AxialRound(float qf, float rf)
        {
            float xf = qf, zf = rf, yf = -xf - zf;
            int rx = (int)Math.Round(xf, MidpointRounding.AwayFromZero);
            int ry = (int)Math.Round(yf, MidpointRounding.AwayFromZero);
            int rz = (int)Math.Round(zf, MidpointRounding.AwayFromZero);
            float dx = Math.Abs(rx - xf), dy = Math.Abs(ry - yf), dz = Math.Abs(rz - zf);
            if (dx > dy && dx > dz) rx = -ry - rz;
            else if (dy > dz) { /* y düzeltilir ama axial'a girmez */ }
            else rz = -rx - ry;
            return (rx, rz);
        }
    }
}

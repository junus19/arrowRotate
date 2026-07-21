using System.Collections.Generic;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// XZ düzleminde TEK PARÇA segment mesh üretici (EP arm oranlarıyla: w=0.28·S, h=0.3·S, fillet=0.1·S).
    /// - BuildStrip: yuvarlatılmış-dikdörtgen kesiti yol boyunca süpürür; açılı köşeler kavisli döner
    ///   (smooth dirsek), uçlar istenirse yuvarlak kapak (revolve) istenirse düz kapak.
    /// - BuildArrowhead: aynı yükseklik/fillet ile yuvarlatılmış üçgen prizma (tepe +X'te).
    /// Taban y=0 (taş üst yüzeyine oturur). Planar (x,y) → dünya (X,Z).
    /// Uçuş animasyonu da aynı üreticiyi kullanır (FlightRenderer3D her frame pencere şeridi üretir).
    /// </summary>
    public static class SegmentMesh3D
    {
        // ── kesit / genel parametreler (S=1'e göre; çağıran s ile ölçekler) ─────
        public const float Width = 0.28f;
        public const float Height = 0.30f;
        public const float Fillet = 0.10f;
        public const float JoinRadius = 0.20f;   // yol köşesi kavis yarıçapı (smooth dirsek)
        public const float HeadLength = 0.48f;
        public const float HeadHalfWidth = 0.30f;
        public const float HeadCornerRadius = 0.08f;
        // Açılı head'de ok başını dirB boyunca ekstra öne iten MAX mesafe (S oranı; en keskin 60° kolda).
        // Düzlükten sapmaya orantılı: 180° (düz)→0, 120°→yarısı, 60° (en keskin)→tam. SegmentView.HeadForwardBump kullanır.
        public const float HeadBendForwardMax = 0.20f;
        // Tepe merkezden uzaklığı (0.866·apothem — kenara yakın). Taban = TipDist−HeadLength = 0.27·s
        // merkezden uzak → açılı girişte ok başı dirseğe/diğer segmente değmez.
        // ⚠ FlightPathBuilder'a 3D'de aynı değer geçilir (HexaGameplayManager) — kalkışta uç sıçramasın.
        public const float HeadTipDist = 0.75f;

        // ── şerit ────────────────────────────────────────────────────────────────
        public static Mesh BuildStrip(List<Vector2> path, float w, float h, float rc, float joinR,
                                      bool capStart, bool capEnd,
                                      int joinSteps = 6, int filletSteps = 3, int capSteps = 8)
        {
            var pts = RoundCorners(path, joinR, joinSteps);
            int n = pts.Count;
            if (n < 2) return new Mesh();

            var profile = BuildProfile(w, h, rc, filletSteps);       // kapalı döngü (CCW t-y)
            var half = BuildHalfProfile(w, h, rc, filletSteps);      // revolve kapak için
            int ringSize = profile.Count;

            var verts = new List<Vector3>(n * ringSize + 64);
            var tris = new List<int>(n * ringSize * 6);

            // istasyon tanjantları
            var tangents = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 d0 = i > 0 ? (pts[i] - pts[i - 1]).normalized : Vector2.zero;
                Vector2 d1 = i < n - 1 ? (pts[i + 1] - pts[i]).normalized : Vector2.zero;
                var t = d0 + d1;
                tangents[i] = t.sqrMagnitude > 1e-8f ? t.normalized : (d0 == Vector2.zero ? d1 : d0);
            }

            // tüp: her istasyonda profil halkası
            for (int i = 0; i < n; i++)
            {
                var T = tangents[i];
                var R = new Vector2(T.y, -T.x); // planar sağ
                foreach (var p in profile)
                    verts.Add(new Vector3(pts[i].x + R.x * p.x, p.y, pts[i].y + R.y * p.x));
            }
            for (int i = 0; i < n - 1; i++)
            for (int j = 0; j < ringSize; j++)
            {
                int j2 = (j + 1) % ringSize;
                int a = i * ringSize + j, b = (i + 1) * ringSize + j;
                int c = i * ringSize + j2, d = (i + 1) * ringSize + j2;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(c); tris.Add(d); tris.Add(b);
            }

            // uçlar
            if (capStart) AddRoundCap(verts, tris, pts[0], -tangents[0], half, capSteps);
            else AddFlatCap(verts, tris, 0, ringSize, forwardOutward: false);
            if (capEnd) AddRoundCap(verts, tris, pts[n - 1], tangents[n - 1], half, capSteps);
            else AddFlatCap(verts, tris, (n - 1) * ringSize, ringSize, forwardOutward: true);

            return Finish(verts, tris);
        }

        /// <summary>Yuvarlatılmış üçgen prizma ok ucu: taban x=0 (z'de ±halfWidth), tepe (length, 0). Taban y=0.
        /// Üst fillet loop'ları GERÇEK poligon offset'idir: köşe yay merkezleri sabit, yarıçap (cornerR−inset).
        /// (Eski normal-inset yöntemi inset>cornerR olunca outline'ı kendine kestiriyordu → görünür mesh defekti.)</summary>
        public static Mesh BuildArrowhead(float length, float halfWidth, float h, float rc, float cornerR,
                                          int cornerSteps = 7, int filletSteps = 4)
        {
            var corners = new[] { new Vector2(length, 0f), new Vector2(0f, halfWidth), new Vector2(0f, -halfWidth) }; // CCW

            // her outline noktası için sabit yay merkezi + açı — inset'te merkez aynı kalır, yarıçap küçülür
            int nc = corners.Length;
            var cens = new List<Vector2>(nc * (cornerSteps + 1));
            var angs = new List<float>(nc * (cornerSteps + 1));
            for (int i = 0; i < nc; i++)
            {
                Vector2 p = corners[i];
                Vector2 d0 = (p - corners[(i - 1 + nc) % nc]).normalized;
                Vector2 d1 = (corners[(i + 1) % nc] - p).normalized;
                float turn = Mathf.Acos(Mathf.Clamp(Vector2.Dot(d0, d1), -1f, 1f));
                float t = cornerR * Mathf.Tan(turn * 0.5f);
                Vector2 pa = p - d0 * t, pb = p + d1 * t;
                float side = Mathf.Sign(d0.x * d1.y - d0.y * d1.x);
                Vector2 n0 = side * new Vector2(-d0.y, d0.x);
                Vector2 cen0 = pa + n0 * cornerR;
                float a0 = Mathf.Atan2(pa.y - cen0.y, pa.x - cen0.x);
                float a1 = Mathf.Atan2(pb.y - cen0.y, pb.x - cen0.x);
                float sweep = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                for (int k = 0; k <= cornerSteps; k++)
                {
                    cens.Add(cen0);
                    angs.Add(a0 + sweep * k / cornerSteps);
                }
            }
            int m = cens.Count;

            rc = Mathf.Min(rc, h * 0.6f);
            var loops = new List<(float inset, float y)> { (0f, 0f), (0f, h - rc) };
            for (int k = 1; k <= filletSteps; k++)
            {
                float a = Mathf.PI * 0.5f * k / filletSteps;
                loops.Add((rc * (1f - Mathf.Cos(a)), h - rc + rc * Mathf.Sin(a)));
            }

            var verts = new List<Vector3>((loops.Count + 1) * m + 1);
            var tris = new List<int>(loops.Count * m * 6);
            foreach (var (inset, y) in loops)
            {
                float r = Mathf.Max(cornerR - inset, 0f); // inset>cornerR → köşe yayı merkeze çöker (kesişme yok)
                for (int i = 0; i < m; i++)
                {
                    var p = cens[i] + new Vector2(Mathf.Cos(angs[i]), Mathf.Sin(angs[i])) * r;
                    verts.Add(new Vector3(p.x, y, p.y));
                }
            }
            for (int l = 0; l < loops.Count - 1; l++)
            for (int j = 0; j < m; j++)
            {
                int j2 = (j + 1) % m;
                int a = l * m + j, b = (l + 1) * m + j, c = l * m + j2, d = (l + 1) * m + j2;
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(c); tris.Add(b); tris.Add(d);
            }
            // üst yüz: halkayı KOPYALA (hard edge → düz gölgeleme) + centroid fan (konveks)
            int topStart = (loops.Count - 1) * m;
            int fanStart = verts.Count;
            var cenTop = Vector3.zero;
            for (int i = 0; i < m; i++) { verts.Add(verts[topStart + i]); cenTop += verts[topStart + i]; }
            cenTop /= m;
            int ci = verts.Count;
            verts.Add(cenTop);
            for (int j = 0; j < m; j++)
            {
                int j2 = (j + 1) % m;
                tris.Add(ci); tris.Add(fanStart + j2); tris.Add(fanStart + j);
            }
            return Finish(verts, tris);
        }

        // ── yardımcılar ──────────────────────────────────────────────────────────

        /// <summary>İç köşeleri kavis yayına çevirir (smooth dirsek).</summary>
        private static List<Vector2> RoundCorners(List<Vector2> path, float radius, int steps)
        {
            if (path.Count < 3 || radius <= 0f) return new List<Vector2>(path);
            var outp = new List<Vector2> { path[0] };
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 p = path[i];
                Vector2 d0 = (p - path[i - 1]).normalized;
                Vector2 d1 = (path[i + 1] - p).normalized;
                float turn = Mathf.Acos(Mathf.Clamp(Vector2.Dot(d0, d1), -1f, 1f));
                if (turn < 0.05f) { outp.Add(p); continue; }

                float r = radius;
                float t = r * Mathf.Tan(turn * 0.5f);
                float maxT = 0.45f * Mathf.Min((p - path[i - 1]).magnitude, (path[i + 1] - p).magnitude);
                if (t > maxT) { t = maxT; r = t / Mathf.Tan(turn * 0.5f); }

                Vector2 pa = p - d0 * t, pb = p + d1 * t;
                float side = Mathf.Sign(d0.x * d1.y - d0.y * d1.x);
                Vector2 n0 = side * new Vector2(-d0.y, d0.x);
                Vector2 cen = pa + n0 * r;
                float a0 = Mathf.Atan2(pa.y - cen.y, pa.x - cen.x);
                float a1 = Mathf.Atan2(pb.y - cen.y, pb.x - cen.x);
                float sweep = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;

                outp.Add(pa);
                for (int k = 1; k < steps; k++)
                {
                    float aa = a0 + sweep * k / steps;
                    outp.Add(cen + new Vector2(Mathf.Cos(aa), Mathf.Sin(aa)) * r);
                }
                outp.Add(pb);
            }
            outp.Add(path[path.Count - 1]);
            return outp;
        }

        /// <summary>Kesit: taban düz (y=0), üst köşeler rc yuvarlak. (t,y) CCW kapalı döngü.</summary>
        private static List<Vector2> BuildProfile(float w, float h, float rc, int steps)
        {
            float hw = w * 0.5f;
            rc = Mathf.Min(rc, Mathf.Min(hw, h) * 0.95f);
            var p = new List<Vector2> { new Vector2(-hw, 0f), new Vector2(hw, 0f) };
            for (int k = 0; k <= steps; k++)
            {
                float a = Mathf.PI * 0.5f * k / steps;
                p.Add(new Vector2(hw - rc + rc * Mathf.Cos(a), h - rc + rc * Mathf.Sin(a)));
            }
            for (int k = 0; k <= steps; k++)
            {
                float a = Mathf.PI * 0.5f * (1f + (float)k / steps);
                p.Add(new Vector2(-hw + rc + rc * Mathf.Cos(a), h - rc + rc * Mathf.Sin(a)));
            }
            return p;
        }

        /// <summary>Kapak revolve profili: (yarıçap t, yükseklik y), dıştan (hw,0) tepeye (0,h).</summary>
        private static List<Vector2> BuildHalfProfile(float w, float h, float rc, int steps)
        {
            float hw = w * 0.5f;
            rc = Mathf.Min(rc, Mathf.Min(hw, h) * 0.95f);
            var p = new List<Vector2> { new Vector2(hw, 0f) };
            for (int k = 0; k <= steps; k++)
            {
                float a = Mathf.PI * 0.5f * k / steps;
                p.Add(new Vector2(hw - rc + rc * Mathf.Cos(a), h - rc + rc * Mathf.Sin(a)));
            }
            p.Add(new Vector2(0f, h));
            return p;
        }

        /// <summary>Yuvarlak uç: yarım profili uç noktası etrafında 180° döndürür.</summary>
        private static void AddRoundCap(List<Vector3> verts, List<int> tris, Vector2 end, Vector2 outward,
                                        List<Vector2> half, int steps)
        {
            var R0 = new Vector2(outward.y, -outward.x);
            int m = half.Count;
            int baseIdx = verts.Count;
            for (int s = 0; s <= steps; s++)
            {
                float phi = Mathf.PI * s / steps;
                var dir = R0 * Mathf.Cos(phi) + outward * Mathf.Sin(phi);
                foreach (var p in half)
                    verts.Add(new Vector3(end.x + dir.x * p.x, p.y, end.y + dir.y * p.x));
            }
            for (int s = 0; s < steps; s++)
            for (int j = 0; j < m - 1; j++)
            {
                int a = baseIdx + s * m + j, b = baseIdx + (s + 1) * m + j;
                int c = a + 1, d = b + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(c); tris.Add(d); tris.Add(b);
            }
        }

        /// <summary>Düz uç: profil halkasını fan ile kapatır (hücre kenarında komşuyla yüz yüze birleşim).</summary>
        private static void AddFlatCap(List<Vector3> verts, List<int> tris, int ringStart, int ringSize, bool forwardOutward)
        {
            var cen = Vector3.zero;
            for (int i = 0; i < ringSize; i++) cen += verts[ringStart + i];
            cen /= ringSize;
            int ci = verts.Count;
            verts.Add(cen);
            for (int j = 0; j < ringSize; j++)
            {
                int j2 = (j + 1) % ringSize;
                if (forwardOutward) { tris.Add(ci); tris.Add(ringStart + j); tris.Add(ringStart + j2); }
                else { tris.Add(ci); tris.Add(ringStart + j2); tris.Add(ringStart + j); }
            }
        }

        private static Mesh Finish(List<Vector3> verts, List<int> tris)
        {
            var mesh = new Mesh { name = "Segment3D" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

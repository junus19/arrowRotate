using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>Runtime düz-renk mesh üreticileri (taş, nokta, ok ucu). Sprites/Default + MPB _Color ile boyanır.</summary>
    public static class MeshFactory
    {
        private static Material _sharedMat;

        public static Material SharedMaterial
        {
            get
            {
                if (_sharedMat == null)
                    _sharedMat = new Material(Shader.Find("Sprites/Default"));
                return _sharedMat;
            }
        }

        /// <summary>Flat-top hexagon (köşeler 0°, 60°, ... 300°).</summary>
        public static Mesh Hex(float radius)
        {
            var mesh = new Mesh { name = "Hex" };
            var verts = new Vector3[7];
            verts[0] = Vector3.zero;
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                verts[i + 1] = new Vector3(radius * Mathf.Cos(a), radius * Mathf.Sin(a), 0f);
            }
            var tris = new int[18];
            for (int i = 0; i < 6; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = 1 + (i + 1) % 6;
                tris[i * 3 + 2] = 1 + i;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Circle(float radius, int segments = 20)
        {
            var mesh = new Mesh { name = "Circle" };
            var verts = new Vector3[segments + 1];
            verts[0] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                verts[i + 1] = new Vector3(radius * Mathf.Cos(a), radius * Mathf.Sin(a), 0f);
            }
            var tris = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = 1 + (i + 1) % segments;
                tris[i * 3 + 2] = 1 + i;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>+x yönüne bakan ok ucu üçgeni: uç (l,0), kanatlar (0,±w).</summary>
        public static Mesh Triangle(float length, float halfWidth)
        {
            var mesh = new Mesh { name = "Tri" };
            mesh.vertices = new[]
            {
                new Vector3(length, 0f, 0f),
                new Vector3(0f, halfWidth, 0f),
                new Vector3(0f, -halfWidth, 0f)
            };
            mesh.triangles = new[] { 0, 2, 1 };
            mesh.RecalculateBounds();
            return mesh;
        }

        public static GameObject NewMeshObject(string name, Mesh mesh, Color color, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = SharedMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            SetColor(mr, color);
            return go;
        }

        public static void SetColor(MeshRenderer mr, Color color)
        {
            var mpb = new MaterialPropertyBlock();
            mr.GetPropertyBlock(mpb);
            // MPB renkleri gamma düzeltmesinden geçmez; Linear space'te sRGB→linear çevir
            var c = QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
            mpb.SetColor("_Color", c);      // Sprites/Default (2D)
            mpb.SetColor("_BaseColor", c);  // URP Lit (3D taşlar)
            mr.SetPropertyBlock(mpb);
        }

        private static Material _lit3DTransparent;

        /// <summary>3D taşlar için paylaşılan URP Lit malzeme — alpha fade'ler için Transparent surface.</summary>
        public static Material Lit3DTransparent
        {
            get
            {
                if (_lit3DTransparent == null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.SetFloat("_Surface", 1f); // Transparent
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_ZWrite", 1f); // konveks puck — iç yüzey artefaktlarını önler
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    mat.SetFloat("_Smoothness", 0.15f);
                    mat.SetFloat("_Metallic", 0f);
                    _lit3DTransparent = mat;
                }
                return _lit3DTransparent;
            }
        }
    }
}

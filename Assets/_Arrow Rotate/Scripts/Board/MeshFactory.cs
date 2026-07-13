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
            mpb.SetColor("_Color", c);
            mr.SetPropertyBlock(mpb);
        }
    }
}

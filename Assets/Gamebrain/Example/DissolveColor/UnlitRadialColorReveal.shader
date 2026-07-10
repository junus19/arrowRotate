Shader "Custom/UnlitRadialColorReveal2D_World"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.5,0.5,0.5,1)
        _TargetColor ("Target Color", Color) = (0,1,0,1)

        _RevealCenter1 ("Reveal Center 1", Vector) = (0,0,0,0)
        _RevealCenter2 ("Reveal Center 2", Vector) = (0,0,0,0)
        _RevealCenter3 ("Reveal Center 3", Vector) = (0,0,0,0)

        _RevealRadius1 ("Reveal Radius 1", Float) = -0.05
        _RevealRadius2 ("Reveal Radius 2", Float) = -0.05
        _RevealRadius3 ("Reveal Radius 3", Float) = -0.05

        _EdgeSoftness ("Edge Softness", Float) = 0.08
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            fixed4 _BaseColor;
            fixed4 _TargetColor;

            float4 _RevealCenter1;
            float4 _RevealCenter2;
            float4 _RevealCenter3;

            float _RevealRadius1;
            float _RevealRadius2;
            float _RevealRadius3;

            float _EdgeSoftness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float RevealMask(float2 pos, float2 center, float radius)
            {
                float dist = distance(pos, center);

                return 1.0 - smoothstep(
                    radius - _EdgeSoftness,
                    radius,
                    dist
                );
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 pos = i.worldPos.xy;

                float mask1 = RevealMask(pos, _RevealCenter1.xy, _RevealRadius1);
                float mask2 = RevealMask(pos, _RevealCenter2.xy, _RevealRadius2);
                float mask3 = RevealMask(pos, _RevealCenter3.xy, _RevealRadius3);

                float mask = max(mask1, max(mask2, mask3));

                return lerp(_BaseColor, _TargetColor, mask);
            }

            ENDCG
        }
    }
}
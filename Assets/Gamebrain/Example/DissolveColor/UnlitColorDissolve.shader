Shader "Custom/UnlitImageDissolveColor"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.5,0.5,0.5,1)
        _TargetColor ("Target Color", Color) = (1,0,0,1)

        _DissolveTex ("Dissolve Image", 2D) = "white" {}

        _Dissolve ("Dissolve", Range(0,1)) = 0
        _Softness ("Softness", Range(0.001,0.5)) = 0.05

        _EdgeWidth ("Toon Edge Width", Range(0.001,0.2)) = 0.03
        _EdgeIntensity ("Toon Edge Intensity", Range(0,5)) = 1.5

        _DissolveOffset ("Dissolve Offset", Vector) = (0,0,0,0)
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _BaseColor;
            fixed4 _TargetColor;

            sampler2D _DissolveTex;
            float4 _DissolveTex_ST;

            float _Dissolve;
            float _Softness;
            float _EdgeWidth;
            float _EdgeIntensity;

            float4 _DissolveOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.uv =
                    TRANSFORM_TEX(v.uv, _DissolveTex)
                    + _DissolveOffset.xy;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dissolveMap = tex2D(_DissolveTex, i.uv).r;

                float mask = smoothstep(
                    _Dissolve - _Softness,
                    _Dissolve + _Softness,
                    dissolveMap
                );

                fixed4 baseCol = lerp(
                    _BaseColor,
                    _TargetColor,
                    1 - mask
                );

                float edge = step(
                    abs(dissolveMap - _Dissolve),
                    _EdgeWidth
                );

                float3 edgeColor =
                    saturate(_TargetColor.rgb + 0.45)
                    * _EdgeIntensity;

                baseCol.rgb = lerp(
                    baseCol.rgb,
                    edgeColor,
                    edge
                );

                return baseCol;
            }

            ENDCG
        }
    }
}
Shader "ArrowRotate/RainbowVertex"
{
    // Kaydırılan gradient-texture shader'ı (URP unlit). UV.x = ok'a göreli (kuyruğa uzaklık)
    // koordinat (FlightRenderer3D her frame yazar) → strobe yok; shader _Time ile örneklemeyi
    // kaydırır → gradient ok boyunca akar. _GradientTex değiştirilerek farklı gradientler denenir.
    Properties
    {
        [MainTexture] _GradientTex ("Gradient", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed", Float) = 0.5
        _Glow ("Glow", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Unlit"
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_GradientTex);
            SAMPLER(sampler_GradientTex);
            float _ScrollSpeed;
            float _Glow;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = float2(IN.uv.x - _Time.y * _ScrollSpeed, IN.uv.y);
                half4 c = SAMPLE_TEXTURE2D(_GradientTex, sampler_GradientTex, uv);
                return half4(c.rgb * _Glow, c.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

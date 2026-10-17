// A painted 2D flame: the mask (InkArtGenerator Fire) is torn upward by scrolling noise and banded into
// three flat painted colours (cream core, orange, deep red edge). The glow is a separate additive quad.
Shader "NASCON/InkFire"
{
    Properties
    {
        _MainTex("Flame Mask", 2D) = "white" {}
        _NoiseTex("Noise", 2D) = "gray" {}
        _Core("Core", Color) = (1, 0.95, 0.75, 1)
        _Mid("Mid", Color) = (1, 0.55, 0.15, 1)
        _Edge("Edge", Color) = (0.75, 0.15, 0.05, 1)
        _Intensity("Intensity", Range(0, 1.5)) = 1
        _Speed("Speed", Float) = 1.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Core, _Mid, _Edge;
                half _Intensity;
                float _Speed;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V Vert(A i) { V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            half4 Frag(V i) : SV_Target
            {
                float2 uv = i.uv;
                half n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * float2(1.5, 1.0) - float2(0, _Time.y * _Speed)).r;
                half n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * float2(3.0, 2.0) - float2(0.3, _Time.y * _Speed * 1.7)).r;
                uv.x += (n1 - 0.5) * 0.18 * uv.y;
                half4 m = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x, uv.y / max(_Intensity, 0.05)));
                half tear = (n2 - 0.4) * uv.y * 1.6;
                half body = m.r - tear;
                clip(body - 0.4);
                half core = m.g - tear * 1.4 - (n1 - 0.5) * 0.3;
                half3 col = core > 0.45 ? _Core.rgb : (body > 0.8 ? _Mid.rgb : _Edge.rgb);
                return half4(col * 1.25, 1);
            }
            ENDHLSL
        }
    }
}

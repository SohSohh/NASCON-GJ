// Soft additive light bloom for lanterns, fires and torches on the story scroll: a radial falloff that
// breathes with a gentle flicker. Colour and strength per material; _Flicker 0 for a steady light.
Shader "NASCON/InkGlow"
{
    Properties
    {
        _Color("Color", Color) = (1, 0.6, 0.3, 1)
        _Intensity("Intensity", Range(0, 4)) = 1
        _Flicker("Flicker", Range(0, 1)) = 0.3
        _Falloff("Falloff", Range(0.5, 6)) = 2.2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        ZTest Always
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity, _Flicker, _Falloff;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float seed : TEXCOORD1; };
            V Vert(A i)
            {
                V o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                float3 wp = TransformObjectToWorld(float3(0, 0, 0));
                o.seed = wp.x * 1.7 + wp.y * 3.1;
                return o;
            }
            half4 Frag(V i) : SV_Target
            {
                float r = length(i.uv - 0.5) * 2.0;
                half fall = pow(saturate(1.0 - r), _Falloff);
                float t = _Time.y * 9.0 + i.seed;
                half flick = 1.0 + (sin(t) * 0.5 + sin(t * 2.3 + 1.0) * 0.3 + sin(t * 5.1) * 0.2) * _Flicker * 0.35;
                return half4(_Color.rgb * fall * _Intensity * flick, 1);
            }
            ENDHLSL
        }
    }
}

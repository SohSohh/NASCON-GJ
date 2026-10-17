// Soft additive light shafts for the story scroll (sun through the eaves, firelight up the wall, moonlight).
// Procedural: a fan of streaks from the top edge of the quad that slowly shimmer and drift. No texture needed.
Shader "NASCON/InkRays"
{
    Properties
    {
        _Color("Color", Color) = (1, 0.75, 0.45, 1)
        _Intensity("Intensity", Range(0, 3)) = 0.6
        _Streaks("Streaks", Range(2, 40)) = 14
        _Spread("Spread (fan)", Range(0, 1)) = 0.45
        _Speed("Shimmer Speed", Float) = 0.15
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity, _Streaks, _Spread, _Speed;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V Vert(A i) { V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            float h(float x) { return frac(sin(x * 91.17) * 43758.55); }
            float n1(float x) { float i = floor(x), f = frac(x); f = f * f * (3 - 2 * f); return lerp(h(i), h(i + 1), f); }
            half4 Frag(V i) : SV_Target
            {
                float2 uv = i.uv;
                float depth = 1.0 - uv.y;                                   // 0 at the source (top), 1 at the far end
                float x = (uv.x - 0.5) / lerp(1.0, 1.0 + _Spread * 2.0, depth) + 0.5; // fan out away from the source
                float t = _Time.y * _Speed;
                float streak = n1(x * _Streaks + t) * 0.6 + n1(x * _Streaks * 2.3 - t * 1.7 + 4.0) * 0.4;
                streak = smoothstep(0.35, 0.95, streak);
                float edges = smoothstep(0.0, 0.25, x) * smoothstep(1.0, 0.75, x);
                float fall = smoothstep(0.0, 0.75, uv.y) * smoothstep(1.0, 0.93, uv.y); // strongest near the source, fading out
                half a = streak * edges * fall * _Intensity;
                return half4(_Color.rgb * a, 1);
            }
            ENDHLSL
        }
    }
}

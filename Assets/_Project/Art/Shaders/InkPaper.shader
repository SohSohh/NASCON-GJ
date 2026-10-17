// The scroll itself: aged rice paper with a warm wash gradient, drifting ink-wash clouds,
// a burned vignette and scattered gold leaf. Sits behind every story panel.
Shader "NASCON/InkPaper"
{
    Properties
    {
        _PaperTex("Paper", 2D) = "white" {}
        _NoiseTex("Noise", 2D) = "gray" {}
        _TopColor("Top Wash", Color) = (0.86, 0.76, 0.56, 1)
        _BottomColor("Bottom Wash", Color) = (0.95, 0.9, 0.78, 1)
        _CloudColor("Cloud Wash", Color) = (0.72, 0.6, 0.42, 1)
        _Clouds("Clouds", Range(0, 1)) = 0.5
        _Gold("Gold Leaf", Range(0, 1)) = 0.35
        _Vignette("Burn Vignette", Range(0, 1)) = 0.6
        _Darkness("Night", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_PaperTex); SAMPLER(sampler_PaperTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor, _BottomColor, _CloudColor;
                half _Clouds, _Gold, _Vignette, _Darkness;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float3 wp : TEXCOORD0; float4 sp : TEXCOORD1; };

            V Vert(A i)
            {
                V o;
                o.wp = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.wp);
                o.sp = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 Frag(V i) : SV_Target
            {
                float2 s = i.sp.xy / i.sp.w;
                half3 col = lerp(_BottomColor.rgb, _TopColor.rgb, smoothstep(0.0, 1.0, s.y));
                float2 w = i.wp.xy * 0.02;
                half c1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, w * float2(0.6, 1.6) + _Time.y * float2(0.003, 0)).r;
                half c2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, w * float2(1.4, 3.0) - _Time.y * float2(0.004, 0)).r;
                half cloud = smoothstep(0.45, 0.8, c1 * 0.7 + c2 * 0.3) * _Clouds;
                col = lerp(col, _CloudColor.rgb, cloud);
                half paper = SAMPLE_TEXTURE2D(_PaperTex, sampler_PaperTex, i.wp.xy * 0.05).r;
                col *= lerp(0.9h, 1.05h, paper);
                half g = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.wp.xy * 0.9).r;
                col = lerp(col, half3(1.1, 0.82, 0.38), step(0.86h, g) * _Gold * 0.5h);
                float2 v = s - 0.5;
                half burn = saturate(dot(v, v) * 2.6 + (c2 - 0.5) * 0.2);
                col *= 1.0h - burn * _Vignette * 0.75h;
                col = lerp(col, col * half3(0.12, 0.1, 0.16), _Darkness);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}

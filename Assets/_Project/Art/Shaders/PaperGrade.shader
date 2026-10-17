// Full-screen "printed on old paper" grade for the story scroll, drawn on a quad in front of the camera.
// Pass 1 (multiply): rice-paper fibres, mottled tone, burned edges, a slow-breathing vignette.
// Pass 2 (add): warm light leaks drifting at the frame edges and bright paper grain, like light through the sheet.
Shader "NASCON/PaperGrade"
{
    Properties
    {
        _PaperTex("Paper", 2D) = "white" {}
        _NoiseTex("Noise", 2D) = "gray" {}
        _Fibre("Fibre Strength", Range(0, 1)) = 0.35
        _Burn("Edge Burn", Range(0, 1)) = 0.55
        _BurnColor("Burn Color", Color) = (0.42, 0.24, 0.12, 1)
        _Leak("Light Leak", Range(0, 2)) = 0.6
        _LeakColor("Leak Color", Color) = (1, 0.62, 0.3, 1)
        _Grain("Grain", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay+10" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_PaperTex); SAMPLER(sampler_PaperTex);
        TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
        CBUFFER_START(UnityPerMaterial)
            half _Fibre, _Burn, _Leak, _Grain;
            half4 _BurnColor, _LeakColor;
        CBUFFER_END
        struct A { float4 positionOS : POSITION; };
        struct V { float4 positionCS : SV_POSITION; float4 sp : TEXCOORD0; };
        V Vert(A i) { V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.sp = ComputeScreenPos(o.positionCS); return o; }
        float2 ScreenUV(V i, out float aspect) { aspect = _ScreenParams.x / _ScreenParams.y; return i.sp.xy / i.sp.w; }
        ENDHLSL

        Pass
        {
            Name "PaperMultiply"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend DstColor Zero
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(V i) : SV_Target
            {
                float aspect; float2 s = ScreenUV(i, aspect);
                float2 p = s * float2(aspect, 1);
                half fibre = SAMPLE_TEXTURE2D(_PaperTex, sampler_PaperTex, p * 1.3).r;
                half mottle = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p * 0.35 + 0.17).r;
                half tone = lerp(1.0h, 0.82h + fibre * 0.22h, _Fibre) * lerp(1.0h, 0.92h + mottle * 0.12h, _Fibre);
                float2 v = s - 0.5;
                half edgeNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p * 2.2).r;
                half burn = saturate((dot(v * float2(1.0, 1.25), v * float2(1.0, 1.25)) * 3.2 - 0.55 + (edgeNoise - 0.5) * 0.35) * 1.6);
                burn *= _Burn * (0.92 + 0.08 * sin(_Time.y * 0.7));
                half3 col = tone * lerp(half3(1, 1, 1), _BurnColor.rgb, burn);
                return half4(col, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "PaperLight"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(V i) : SV_Target
            {
                float aspect; float2 s = ScreenUV(i, aspect);
                float t = _Time.y;
                // two soft warm leaks that slowly drift along the left and right edges
                float2 a = float2(-0.05 + 0.04 * sin(t * 0.21), 0.65 + 0.2 * sin(t * 0.13));
                float2 b = float2(1.05 + 0.04 * sin(t * 0.17 + 2.0), 0.3 + 0.2 * sin(t * 0.11 + 1.0));
                float2 sa = (s - a) * float2(aspect * 0.6, 1.0), sb = (s - b) * float2(aspect * 0.6, 1.0);
                half leak = exp(-dot(sa, sa) * 7.0) + exp(-dot(sb, sb) * 9.0) * 0.7;
                half grain = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, s * float2(aspect, 1) * 9.0 + frac(t * 23.0) * 0.37).r;
                grain = saturate((grain - 0.72) * 3.0) * _Grain * 0.12;
                return half4(_LeakColor.rgb * leak * _Leak * 0.35 + grain, 1);
            }
            ENDHLSL
        }
    }
}

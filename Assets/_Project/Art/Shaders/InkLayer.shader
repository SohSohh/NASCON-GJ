// A painted paper layer for the story-scroll prologue. The texture is a mask set (see InkArtGenerator):
//   R coverage, G tone (wash..ink), B accent (lit windows, gold, teal, puppet filigree).
// Ink pools darker at the cut edges, paper grain shows through, far layers melt into the paper colour,
// and _Reveal paints the layer on with a ragged brush wipe.
Shader "NASCON/InkLayer"
{
    Properties
    {
        _MainTex("Mask (R cov, G tone, B accent)", 2D) = "white" {}
        _PaperTex("Paper", 2D) = "white" {}
        _NoiseTex("Noise", 2D) = "gray" {}
        _LightColor("Wash Color", Color) = (0.62, 0.52, 0.4, 1)
        _DarkColor("Ink Color", Color) = (0.08, 0.06, 0.06, 1)
        _AccentColor("Accent Color", Color) = (1, 0.75, 0.35, 1)
        _AccentGlow("Accent Glow", Range(0, 4)) = 0
        _Atmos("Atmosphere (fade to paper)", Range(0, 1)) = 0
        _AtmosColor("Atmosphere Color", Color) = (0.88, 0.8, 0.66, 1)
        _Opacity("Opacity", Range(0, 1)) = 1
        _Reveal("Reveal", Range(0, 1)) = 1
        _RevealDir("Reveal Direction (xy)", Vector) = (1, 0, 0, 0)
        _EdgeDarken("Edge Pooling", Range(0, 1)) = 0.45
        _Wobble("Sway", Range(0, 0.1)) = 0
        _WobbleSpeed("Sway Speed", Float) = 1
        _Silhouette("Silhouette (flat ink)", Range(0, 1)) = 0
        [Toggle] _Lamp("Lamp Layer (glows at night)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Ink"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_TexelSize;
            TEXTURE2D(_PaperTex); SAMPLER(sampler_PaperTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _LightColor, _DarkColor, _AccentColor, _AtmosColor;
                half _AccentGlow, _Atmos, _Opacity, _Reveal, _EdgeDarken, _Wobble, _Silhouette, _Lamp;
                float4 _RevealDir;
                float _WobbleSpeed;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 wp : TEXCOORD1; };

            V Vert(A i)
            {
                V o;
                float3 wp = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(wp);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.wp = wp;
                return o;
            }

            half4 Frag(V i) : SV_Target
            {
                float2 uv = i.uv;
                // sway: grass / banners / hems bend more toward the top of the layer
                uv.x += sin(uv.y * 6.0 + _Time.y * _WobbleSpeed + i.wp.x * 0.7) * _Wobble * uv.y;

                half4 m = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                // ink bleed: the coverage edge is pushed in and out by noise, so cut edges look brushed
                half n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 6.0 + i.wp.xy * 0.05).r;
                half cov = saturate((m.r - 0.5h) * 1.6h + 0.5h + (n - 0.5h) * 0.35h);

                // pigment pools at the edges of the shape
                float2 t = _MainTex_TexelSize.xy * 3.0;
                half around = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(t.x, 0)).r + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(t.x, 0)).r
                            + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, t.y)).r + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, t.y)).r;
                half edge = saturate(1.0h - around * 0.25h) * cov * 2.0h;

                half tone = saturate(m.g + edge * _EdgeDarken);
                half3 col = lerp(_LightColor.rgb, _DarkColor.rgb, tone);
                col = lerp(col, _DarkColor.rgb, _Silhouette);
                half3 accent = _AccentColor.rgb * (1.0h + _AccentGlow);
                col = lerp(col, accent, m.b * (1.0h - _Silhouette));

                // paper grain
                half paper = SAMPLE_TEXTURE2D(_PaperTex, sampler_PaperTex, i.wp.xy * 0.08).r;
                col *= lerp(0.92h, 1.06h, paper);

                // atmosphere: distant layers dissolve into the paper
                col = lerp(col, _AtmosColor.rgb, _Atmos);

                // brush-wipe reveal
                half rn = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * float2(2.5, 9.0)).r;
                float along = dot(uv - 0.5, normalize(_RevealDir.xy)) + 0.5;
                half reveal = smoothstep(along - 0.06, along + 0.02, _Reveal * 1.25 - 0.12 + (rn - 0.5) * 0.25);

                return half4(col, cov * _Opacity * reveal);
            }
            ENDHLSL
        }
    }
}

// Shadow creature sprite, drawn on a mesh that is conformed onto walls/floors at runtime (ShadowSurfaceSkin).
// Sheet channels:  R = body mask,  G = glow (broken halo, eyes, maw, burning rim),  B = inner detail streaks.
// Premultiplied: the body darkens what's behind it (it is a shadow), the glow is added on top.
Shader "NASCON/ShadowCreature"
{
    Properties
    {
        [MainTexture] _MainTex("Sheet", 2D) = "black" {}
        _NoiseTex("Smoke Noise", 2D) = "gray" {}
        _Frame("Frame Rect (xy offset, zw size)", Vector) = (0, 0, 1, 1)
        _Flip("Flip X", Float) = 0
        _BodyColor("Body Color", Color) = (0.015, 0.006, 0.012, 0.94)
        _DetailColor("Detail Color", Color) = (0.11, 0.035, 0.03, 1)
        [HDR] _GlowColor("Glow Color", Color) = (3.2, 1.1, 0.25, 1)
        [HDR] _SheenColor("Low-light Sheen", Color) = (0.09, 0.03, 0.07, 1)
        _Glow("Glow Strength", Range(0, 3)) = 1
        _Dissolve("Dissolve", Range(0, 1)) = 0
        _Wobble("Smoke Wobble", Range(0, 0.05)) = 0.012
        _Opacity("Opacity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Offset -2, -2

        Pass
        {
            Name "ShadowCreature"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float4 _Frame;
                float _Flip;
                half4 _BodyColor;
                half4 _DetailColor;
                half4 _GlowColor;
                half4 _SheenColor;
                half _Glow;
                half _Dissolve;
                half _Wobble;
                half _Opacity;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half fog : TEXCOORD1; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                if (_Flip > 0.5) uv.x = 1.0 - uv.x;

                // smoky wobble, stronger toward the dripping bottom
                half n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * float2(2.0, 3.0) + float2(_Time.y * 0.07, -_Time.y * 0.21)).r;
                uv.x += (n - 0.8h) * _Wobble * (1.6h - uv.y);

                float2 sheetUV = _Frame.xy + saturate(uv) * _Frame.zw;
                half4 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sheetUV);

                half dissolveNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 1.7).r;
                half keep = smoothstep(_Dissolve - 0.05h, _Dissolve + 0.05h, saturate((dissolveNoise - 0.55h) * 2.2h + 0.5h));
                half body = s.r * keep * _Opacity;

                half3 col = lerp(_BodyColor.rgb, _DetailColor.rgb, s.b) * body;
                half alpha = body * _BodyColor.a;
                half3 glow = _GlowColor.rgb * s.g * _Glow * keep * _Opacity;
                // a faint oily sheen on the inner streaks so the shape still reads where there is little light
                glow += _SheenColor.rgb * s.b * body;

                col = MixFog(col, input.fog);
                return half4(col + glow, alpha);
            }
            ENDHLSL
        }
    }
}

// Painterly particle flame (see DOCUMENTATION/CHARACTER SHEETS/fire sheet reference.png):
// hard-edged wisps with a cream-white core, orange body and a thin teal rim, edges broken by scrolling noise.
// Premultiplied blending so overlapping wisps merge into solid painted shapes; HDR core feeds bloom.
// Particle vertex colour (Color over Lifetime) tints the body and drives the fade.
Shader "NASCON/StylizedFlame"
{
    Properties
    {
        [MainTexture] _BaseMap("Shape (soft blob)", 2D) = "white" {}
        _NoiseTex("Edge Noise", 2D) = "gray" {}
        [HDR] _CoreColor("Core Color", Color) = (3.2, 2.7, 2.0, 1)
        [HDR] _EdgeColor("Rim Color", Color) = (0.9, 1.6, 1.5, 1)
        _Intensity("Body Intensity", Range(0, 8)) = 2.4
        _InteractionHighlight("Interaction Highlight", Float) = 0
        _InteractionOutline("Interaction Outline", Float) = 0
        _OutlineWidth("Outline Width (alpha)", Range(0.001, 0.2)) = 0.06
        _Cutoff("Shape Cutoff", Range(0, 1)) = 0.28
        _Softness("Edge Softness", Range(0.001, 0.2)) = 0.03
        _CoreThreshold("Core Threshold", Range(0, 1)) = 0.32
        _RimWidth("Rim Width", Range(0, 0.4)) = 0.1
        _RimAmount("Rim Amount", Range(0, 1)) = 0.55
        _NoiseScale("Noise Scale", Float) = 1.6
        _NoiseSpeed("Noise Scroll Speed", Float) = 1.8
        _NoiseAmount("Noise Amount", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Flame"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NoiseTex_ST;
                half4 _CoreColor;
                half4 _EdgeColor;
                half _Intensity;
                half _InteractionHighlight;
                half _InteractionOutline;
                half _OutlineWidth;
                half _Cutoff;
                half _Softness;
                half _CoreThreshold;
                half _RimWidth;
                half _RimAmount;
                float _NoiseScale;
                float _NoiseSpeed;
                half _NoiseAmount;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color       : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color       : COLOR;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.color = input.color;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half shape = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * input.color.a;

                float2 nuv = input.uv * _NoiseScale + input.positionWS.xz * 2.0 + float2(input.positionWS.y * 1.3, -_Time.y * _NoiseSpeed);
                half n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nuv).r;
                half cut = _Cutoff + (n - 0.8h) * 2.5h * _NoiseAmount;

                half mask = smoothstep(cut - _Softness, cut + _Softness, shape);
                half core = smoothstep(cut + _CoreThreshold, cut + _CoreThreshold + 0.15h, shape);
                half rim = 1.0h - smoothstep(cut, cut + _RimWidth, shape);

                half3 col = input.color.rgb * _Intensity;
                col = lerp(col, _EdgeColor.rgb * max(input.color.rgb, 0.4h), rim * _RimAmount);
                col = lerp(col, _CoreColor.rgb, core);
                col += half3(1.0h, 0.62h, 0.18h) * _InteractionHighlight * (0.25h + rim);
                if (_InteractionOutline > 0.5h)
                {
                    half outline = mask * (1.0h - smoothstep(cut + _Softness, cut + _Softness + _OutlineWidth, shape));
                    col = lerp(col, 0.0h, outline);
                }
                return half4(col * mask, mask);
            }
            ENDHLSL
        }
    }
}

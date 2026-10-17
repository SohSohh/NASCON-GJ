// Fake volumetric light shaft (god ray) for cracks in the ceiling. Additive, soft at the silhouette and at both ends.
Shader "NASCON/LightShaft"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (0.35, 0.42, 0.75, 1)
        _Intensity("Intensity", Range(0, 4)) = 0.6
        _EdgePower("Edge Softness", Range(0.5, 8)) = 2.5
        _TopFade("Top Fade", Range(0, 1)) = 0.15
        _BottomFade("Bottom Fade", Range(0, 1)) = 0.35
        _NoiseTex("Dust Noise", 2D) = "gray" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _EdgePower;
                half _TopFade;
                half _BottomFade;
                float4 _NoiseTex_ST;
            CBUFFER_END
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 viewWS : TEXCOORD2; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.viewWS = GetWorldSpaceViewDir(positionWS);
                o.uv = input.uv;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half facing = abs(dot(normalize(input.normalWS), normalize(input.viewWS)));
                half edge = pow(facing, _EdgePower);
                half v = input.uv.y; // 0 bottom, 1 top
                half ends = smoothstep(0, _BottomFade, v) * (1 - smoothstep(1 - _TopFade, 1, v));
                half dust = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * float2(3, 1.5) + float2(0, _Time.y * 0.02)).r;
                half a = edge * ends * (0.75h + dust * 0.4h) * _Intensity;
                return half4(_Color.rgb * a, 0);
            }
            ENDHLSL
        }
    }
}

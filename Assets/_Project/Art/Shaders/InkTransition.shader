// Full-screen ink transition drawn on a quad in front of the camera.
//  _Mode 0: an ink drop blooms from _Center and swallows the frame (ragged, feathered edge)
//  _Mode 1: a dry-brush sweep across the frame along _Dir
// _Progress 0 = clear, 1 = covered. _Color is the ink (or paper, for a white-out).
Shader "NASCON/InkTransition"
{
    Properties
    {
        _NoiseTex("Noise", 2D) = "gray" {}
        _Color("Ink", Color) = (0.03, 0.02, 0.03, 1)
        _Progress("Progress", Range(0, 1)) = 0
        _Mode("Mode", Float) = 0
        _Center("Center (screen uv)", Vector) = (0.5, 0.5, 0, 0)
        _Dir("Sweep Direction", Vector) = (1, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Progress;
                float _Mode;
                float4 _Center, _Dir;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float4 sp : TEXCOORD0; };
            V Vert(A i) { V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.sp = ComputeScreenPos(o.positionCS); return o; }
            half4 Frag(V i) : SV_Target
            {
                float2 s = i.sp.xy / i.sp.w;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                half n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, s * float2(aspect, 1) * 1.8).r;
                half n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, s * float2(aspect, 1) * 6.0).r;
                half field;
                if (_Mode < 0.5)
                {
                    float2 d = (s - _Center.xy) * float2(aspect, 1);
                    field = length(d) / 1.25 + (n - 0.5) * 0.35 + (n2 - 0.5) * 0.08;
                }
                else
                {
                    float2 dir = normalize(_Dir.xy);
                    field = dot(s - 0.5, dir) * 0.9 + 0.5 + (n - 0.5) * 0.25 + (n2 - 0.5) * 0.12 * (1.0 + sin(s.y * 90.0) * 0.5);
                }
                half a = 1.0h - smoothstep(_Progress * 1.3 - 0.15, _Progress * 1.3 - 0.05, field);
                return half4(_Color.rgb, a * _Color.a);
            }
            ENDHLSL
        }
    }
}

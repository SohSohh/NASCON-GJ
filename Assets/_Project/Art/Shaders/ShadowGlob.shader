// Living-shadow tar: dark glossy blobs that wobble and bulge (vertex noise), with a smouldering ember rim so the
// mass reads even in near darkness, and a burning dissolve edge when light eats into it.
Shader "NASCON/ShadowGlob"
{
    Properties
    {
        _BodyColor("Body", Color) = (0.018, 0.005, 0.012, 1)
        [HDR] _InnerColor("Inner Sheen", Color) = (0.16, 0.03, 0.12, 1)
        [HDR] _RimColor("Ember Rim", Color) = (1.6, 0.32, 0.08, 1)
        _RimPower("Rim Power", Range(0.5, 8)) = 2.4
        _Glow("Glow", Range(0, 3)) = 1
        _Wobble("Wobble", Range(0, 0.5)) = 0.16
        _WobbleSpeed("Wobble Speed", Float) = 1.4
        _Dissolve("Dissolve", Range(0, 1)) = 0
        _NoiseTex("Noise", 2D) = "gray" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+20" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BodyColor;
            half4 _InnerColor;
            half4 _RimColor;
            half _RimPower;
            half _Glow;
            half _Wobble;
            float _WobbleSpeed;
            half _Dissolve;
            float4 _NoiseTex_ST;
        CBUFFER_END
        TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

        // smooth pseudo-noise from layered sines: cheap, no texture lookups in the vertex stage
        float Blob(float3 p, float t)
        {
            return sin(dot(p, float3(3.1, 2.7, 3.7)) + t) * 0.5
                 + sin(dot(p, float3(-2.3, 4.1, 1.7)) * 1.7 - t * 1.3) * 0.33
                 + sin(p.y * 6.1 + p.x * 2.3 + t * 2.2) * 0.17;
        }

        float3 Displace(float3 positionOS, float3 normalOS)
        {
            float3 wp = TransformObjectToWorld(positionOS);
            float n = Blob(wp * 1.3, _Time.y * _WobbleSpeed);
            return positionOS + normalOS * n * _Wobble;
        }

        half DissolveMask(float3 wp)
        {
            half a = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, wp.xy * 0.9 + wp.z * 0.37).r;
            half b = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, wp.zy * 1.7 + 3.1).r;
            return a * 0.6 + b * 0.4;
        }
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; half fog : TEXCOORD2; };

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                float3 pos = Displace(input.positionOS.xyz, input.normalOS);
                o.positionWS = TransformObjectToWorld(pos);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half mask = DissolveMask(input.positionWS);
                clip(mask - _Dissolve * 1.05h);
                half burn = 1.0h - smoothstep(0.0h, 0.09h, mask - _Dissolve * 1.05h);
                burn *= step(0.001h, _Dissolve);

                half3 N = normalize(input.normalWS);
                half3 V = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half ndv = saturate(dot(N, V));
                half rim = pow(1.0h - ndv, _RimPower);
                half flicker = 0.75h + 0.25h * sin(_Time.y * 7.0 + input.positionWS.y * 9.0);

                half3 col = _BodyColor.rgb
                          + _InnerColor.rgb * pow(ndv, 3.0h) * 0.5h
                          + _RimColor.rgb * rim * _Glow * flicker
                          + _RimColor.rgb * burn * 3.0h;
                col = MixFog(col, input.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DVert
            #pragma fragment DFrag
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings DVert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(Displace(input.positionOS.xyz, input.normalOS));
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }
            half DFrag(Varyings input) : SV_Target { clip(DissolveMask(input.positionWS) - _Dissolve * 1.05h); return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex NVert
            #pragma fragment NFrag
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            Varyings NVert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(Displace(input.positionOS.xyz, input.normalOS));
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }
            half4 NFrag(Varyings input) : SV_Target
            {
                clip(DissolveMask(input.positionWS) - _Dissolve * 1.05h);
                return half4(NormalizeNormalPerPixel(input.normalWS), 0);
            }
            ENDHLSL
        }
    }
}

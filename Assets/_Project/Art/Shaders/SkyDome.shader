// Painted sky dome for outdoor cutscenes: vertical gradient, two layers of drifting posterised clouds,
// a brighter glow toward a "sun" direction. Colours come from globals so the director can blend skies.
Shader "NASCON/SkyDome"
{
    Properties
    {
        _NoiseTex("Noise", 2D) = "gray" {}
        _CloudScale("Cloud Scale", Float) = 1.6
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+500" "RenderPipeline" = "UniversalPipeline" }
        Cull Front ZWrite On

        Pass
        {
            Name "Sky"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            float _CloudScale;
            float4 _NasconSkyTop, _NasconSkyHorizon, _NasconSkyCloud, _NasconSkyGlow;
            float4 _NasconSkyGlowDir;

            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            V Vert(A i)
            {
                V o;
                float3 wp = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(wp);
                o.dir = normalize(wp - _WorldSpaceCameraPos);
                return o;
            }

            half4 Frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = saturate(d.y);
                half3 col = lerp(_NasconSkyHorizon.rgb, _NasconSkyTop.rgb, pow(h, 0.55));
                float2 uv = d.xz / max(d.y + 0.18, 0.05);
                float c1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 0.05 * _CloudScale + _Time.y * float2(0.004, 0.001)).r;
                float c2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 0.13 * _CloudScale - _Time.y * float2(0.006, 0.002)).r;
                float cloud = c1 * 0.65 + c2 * 0.35;
                cloud = smoothstep(0.5, 0.56, cloud) * 0.85 + smoothstep(0.6, 0.66, cloud) * 0.15; // posterised painted edges
                cloud *= smoothstep(-0.02, 0.15, d.y);
                float shade = smoothstep(0.3, 0.75, c2);
                half3 cloudCol = lerp(_NasconSkyCloud.rgb, _NasconSkyCloud.rgb * 0.55, shade);
                col = lerp(col, cloudCol, cloud);
                float g = pow(saturate(dot(d, normalize(_NasconSkyGlowDir.xyz))), 6.0);
                col += _NasconSkyGlow.rgb * g * (1.0 - cloud * 0.6);
                if (d.y < 0) col = _NasconSkyHorizon.rgb * 0.8;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}

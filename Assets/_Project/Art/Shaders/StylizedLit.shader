// Painterly lit surface matching the concept boards:
//  - torch light posterised into soft bands (flat painted planes of light)
//  - unlit areas fall to a purple/maroon tint instead of black
//  - world-space "brush grit" breaks up albedo and band edges (triplanar, no UVs needed)
//  - falloff/midtones pushed toward saturated red-orange
//  - optional hard "wet" highlight for floors
// Works with URP Forward / Forward+ (cluster light loop), point/spot shadows and fog.
Shader "NASCON/StylizedLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Painterly Light)]
        _Bands("Light Bands", Range(1, 8)) = 4
        _BandSoftness("Band Softness", Range(0.001, 0.5)) = 0.14
        _BandNoise("Band Brush Noise", Range(0, 0.5)) = 0.12
        _Wrap("Light Wrap", Range(0, 1)) = 0.3
        _ShadowTint("Shadow Tint", Color) = (0.17, 0.1, 0.21, 1)
        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.6
        _MidtoneTint("Midtone Tint", Color) = (1.18, 0.88, 0.62, 1)
        _MidtoneStrength("Midtone Strength", Range(0, 1)) = 0.4

        [Header(Brush Grit)]
        _GritTex("Grit Texture", 2D) = "gray" {}
        _GritScale("Grit World Scale", Float) = 0.6
        _GritStrength("Grit Strength", Range(0, 1)) = 0.45

        [Header(Wet Highlight)]
        _SpecStrength("Spec Strength", Range(0, 4)) = 0
        _SpecSize("Spec Size", Range(0.001, 0.5)) = 0.08

        [Header(Rim)]
        _InteractionHighlight("Interaction Highlight", Float) = 0
        _RimColor("Rim Color", Color) = (0.32, 0.18, 0.42, 1)
        _RimStrength("Rim Strength", Range(0, 1)) = 0.12

        [Header(Interaction Outline)]
        _InteractionOutline("Interaction Outline", Float) = 0
        _OutlineWidth("Outline Width (pixels)", Range(1, 8)) = 10
        [HDR] _OutlineColor("Outline Color", Color) = (4, 0, 0, 1)

        [Header(Emission)]
        [HDR] _EmissionColor("Emission", Color) = (0, 0, 0, 0)

        [Header(Clamp)]
        _MaxLight("Max Light (first-person meshes)", Range(0.5, 64)) = 64
        [Header(First Person)]
        _CamClip("Camera Clip (x radius, y depth below eye; 0 = off)", Vector) = (0, 0, 0, 0)

        [Header(GB Camera Bypass)]
        [Toggle] _GBCameraBypass("Bypass GB Camera Palette", Float) = 0
        [HideInInspector] _GBCameraStencilMask("GB Camera Stencil Mask", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "UniversalMaterialType" = "Lit" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Bands;
            half _BandSoftness;
            half _BandNoise;
            half _Wrap;
            half4 _ShadowTint;
            half _AmbientStrength;
            half4 _MidtoneTint;
            half _MidtoneStrength;
            float4 _GritTex_ST;
            float _GritScale;
            half _GritStrength;
            half _SpecStrength;
            half _SpecSize;
            half4 _RimColor;
            half _RimStrength;
            half _InteractionHighlight;
            half _InteractionOutline;
            half _OutlineWidth;
            half4 _OutlineColor;
            half4 _EmissionColor;
            half _MaxLight;
            float4 _CamClip;
            half _GBCameraBypass;
            float _GBCameraStencilMask;
        CBUFFER_END

        // First-person body: cut away everything in a column around the eye (head, neck, shoulders, hair)
        // so the camera never sees the inside of its own head. Arms reaching forward and legs below stay.
        void CamClip(float3 positionWS)
        {
            if (_CamClip.x <= 0.0) return;
            float2 d = positionWS.xz - _WorldSpaceCameraPos.xz;
            if (dot(d, d) < _CamClip.x * _CamClip.x && positionWS.y > _WorldSpaceCameraPos.y - _CamClip.y) clip(-1);
        }

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_GritTex); SAMPLER(sampler_GritTex);
        ENDHLSL

        Pass
        {
            Name "InteractionOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            ColorMask RGB
            Stencil
            {
                Ref 128
                ReadMask 128
                WriteMask 128
                Comp Always
                Pass Replace
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_instancing

            struct OutlineAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            OutlineVaryings OutlineVert(OutlineAttributes input)
            {
                OutlineVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                float3 normalVS = mul((float3x3)UNITY_MATRIX_V, normalWS);
                float2 normalCS = mul((float2x2)UNITY_MATRIX_P, normalVS.xy);
                float normalLengthSq = dot(normalCS, normalCS);
                if (normalLengthSq > 1e-6)
                    output.positionCS.xy += normalCS * rsqrt(normalLengthSq) *
                        (2.0 * _OutlineWidth / _ScreenParams.xy) * output.positionCS.w;
                return output;
            }

            half4 OutlineFrag(OutlineVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                clip(_InteractionOutline - 0.5);
                return half4(_OutlineColor.rgb, 1);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            // Bit 7 skips GBCamera; bit 6 preserves colour through painterly passes.
            Stencil
            {
                Ref 192
                WriteMask [_GBCameraStencilMask]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half TriplanarGrit(float3 positionWS, half3 normalWS, float scale)
            {
                half3 w = pow(abs(normalWS), 4.0h);
                w /= max(w.x + w.y + w.z, 1e-4h);
                half gx = SAMPLE_TEXTURE2D(_GritTex, sampler_GritTex, positionWS.zy * scale).r;
                half gy = SAMPLE_TEXTURE2D(_GritTex, sampler_GritTex, positionWS.xz * scale).r;
                half gz = SAMPLE_TEXTURE2D(_GritTex, sampler_GritTex, positionWS.xy * scale).r;
                return gx * w.x + gy * w.y + gz * w.z;
            }

            // Posterise light energy in perceptual space, then expand back to HDR.
            half Band(half energy, half noise)
            {
                half c = energy / (1.0h + energy);
                c = saturate(c + noise);
                half x = c * _Bands;
                half steps = floor(x);
                half s = smoothstep(0.5h - _BandSoftness, 0.5h + _BandSoftness, frac(x));
                // top band capped at ~9x (was 24x): a flame lying on the floor no longer blows out a white disk
                half b = min((steps + s) / _Bands, 0.9h);
                return b / (1.0h - b);
            }

            half3 ShadeLight(Light light, half3 N, half3 V, half noise, inout half3 specular)
            {
                half maxc = max(max(light.color.r, light.color.g), max(light.color.b, 1e-4h));
                half3 hue = light.color / maxc;
                half ndl = saturate((dot(N, light.direction) + _Wrap) / (1.0h + _Wrap));
                half atten = light.distanceAttenuation * light.shadowAttenuation;
                half energy = ndl * atten * maxc;

                half3 H = SafeNormalize(light.direction + V);
                half ndh = saturate(dot(N, H));
                // energy capped: right next to a light the inverse-square term made the sheen a solid white disk
                half spec = smoothstep(1.0h - _SpecSize, 1.0h - _SpecSize * 0.5h, ndh) * min(atten * maxc, 0.8h) * saturate(dot(N, light.direction) * 4.0h);
                specular += hue * spec;

                return hue * Band(energy, noise);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                CamClip(input.positionWS);
                half3 N = normalize(input.normalWS);
                half3 V = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // two octaves of brush grit: fine speckle + broad blotches
                half fine = TriplanarGrit(input.positionWS, N, _GritScale);
                half broad = TriplanarGrit(input.positionWS + 17.3, N, _GritScale * 0.21);
                half gritValue = fine * 0.6h + broad * 0.4h;          // ~0.8 mean
                half gritSigned = clamp((gritValue - 0.8h) * 5.0h, -1.0h, 1.0h);
                half3 albedo = base.rgb * lerp(1.0h, gritValue / 0.8h, _GritStrength);
                half bandNoise = gritSigned * _BandNoise;

                half3 lightSum = 0;
                half3 specular = 0;
                half4 shadowMask = half4(1, 1, 1, 1);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                lightSum += ShadeLight(mainLight, N, V, bandNoise, specular);

                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = N;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                uint pixelLightCount = GetAdditionalLightsCount();

                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                    lightSum += ShadeLight(light, N, V, bandNoise, specular);
                }
                #endif

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                    lightSum += ShadeLight(light, N, V, bandNoise, specular);
                LIGHT_LOOP_END
                #endif

                lightSum = min(lightSum, _MaxLight);
                specular = min(specular, _MaxLight);

                // ambient: purple/maroon instead of black
                half3 ambient = albedo * (_ShadowTint.rgb * _AmbientStrength + SampleSH(N) * 0.5h);

                // push midtones (the falloff between lit and dark) toward saturated red-orange
                half litMax = max(lightSum.r, max(lightSum.g, lightSum.b));
                half litC = litMax / (1.0h + litMax);
                half mid = smoothstep(0.02h, 0.25h, litC) * (1.0h - smoothstep(0.35h, 0.8h, litC));
                half3 lit = albedo * lightSum * lerp(1.0h, _MidtoneTint.rgb, mid * _MidtoneStrength);

                half rim = pow(1.0h - saturate(dot(N, V)), 4.0h) * _RimStrength;

                half3 color = ambient + lit + specular * _SpecStrength + _RimColor.rgb * rim + _EmissionColor.rgb;
                // A narrow, light-independent contour stays visible on dark and bright surfaces.
                half edge = pow(1.0h - saturate(dot(N, V)), 8.0h);
                color += half3(1.0h, 0.62h, 0.18h) * _InteractionHighlight * (0.12h + edge * 5.0h);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                return ApplyShadowClamping(positionCS);
            }

            half4 ShadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            DepthVaryings DepthVert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                DepthVaryings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half DepthFrag(DepthVaryings input) : SV_Target { CamClip(input.positionWS); return 0; }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings NormalsVert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 NormalsFrag(Varyings input) : SV_Target
            {
                CamClip(input.positionWS);
                return half4(NormalizeNormalPerPixel(input.normalWS), 0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

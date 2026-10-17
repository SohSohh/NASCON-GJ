// Comic / cartoon finishing pass (runs after the painterly pass, before URP post-processing):
//  1. Ink: thick dark outlines from depth + normal discontinuities (cartoon look)
//  2. Toon colour: saturation lift + soft posterise
//  3. Halftone: comic dot screen in the shadows
//  4. Void flood: everything within a world-space radius of the Void is swallowed by ink, with a ragged burning edge
//  5. Impact frame: a hard black/white negative flash (thunderbolt hit)
// All strengths are globals (_NasconInk, _NasconToon, _NasconHalftone, _NasconVoid, _NasconImpact) so scenes
// and cutscenes drive them without touching the material. Zero = off.
Shader "NASCON/ComicPost"
{
    Properties
    {
        _InkColor("Ink Color", Color) = (0.06, 0.025, 0.05, 1)
        _InkWidth("Ink Width (px)", Range(0.5, 4)) = 1.6
        _DepthSensitivity("Depth Sensitivity", Range(0, 2)) = 0.6
        _NormalSensitivity("Normal Sensitivity", Range(0, 2)) = 0.9
        _InkFade("Ink Fade Distance", Float) = 60
        _HalftoneScale("Halftone Cell (px)", Float) = 6
        _NoiseTex("Noise", 2D) = "gray" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "ComicPost"
            Stencil
            {
                Ref 64
                ReadMask 64
                WriteMask 0
                Comp NotEqual
                Pass Keep
            }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4 _InkColor;
            float _InkWidth;
            float _DepthSensitivity;
            float _NormalSensitivity;
            float _InkFade;
            float _HalftoneScale;
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            // globals
            float _NasconInk;
            float _NasconToon;
            float _NasconHalftone;
            float4 _NasconVoid;       // xyz origin, w radius (0 = off)
            float _NasconVoidSoft;
            float _NasconVoidDark;
            float _NasconImpact;
            float _NasconDrain;

            float EyeDepth(float2 uv) { return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams); }

            half3 Saturate(half3 c, half amount)
            {
                half l = Luminance(c);
                return lerp(l.xxx, c, 1.0h + amount);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half3 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0).rgb;

                float rawDepth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    bool sky = rawDepth <= 1e-5;
                #else
                    bool sky = rawDepth >= 1.0 - 1e-5;
                #endif
                float depth = LinearEyeDepth(rawDepth, _ZBufferParams);

                // ---- toon colour
                if (_NasconToon > 0.0)
                {
                    half3 c = Saturate(color, 0.45h * _NasconToon);
                    half3 compressed = c / (1.0h + c);
                    half3 stepped = floor(compressed * 6.0h + 0.5h) / 6.0h;
                    compressed = lerp(compressed, stepped, 0.35h * _NasconToon);
                    color = compressed / max(1.0h - compressed, 0.02h);
                }

                // ---- drained of colour (after the Void)
                if (_NasconDrain > 0.0) color = lerp(color, Luminance(color).xxx * half3(0.9, 0.85, 0.95), saturate(_NasconDrain));

                // ---- halftone dots in the shadows
                if (_NasconHalftone > 0.0 && !sky)
                {
                    float2 px = uv * _ScreenParams.xy;
                    float2 rot = float2(px.x * 0.7071 - px.y * 0.7071, px.x * 0.7071 + px.y * 0.7071);
                    float2 cell = frac(rot / _HalftoneScale) - 0.5;
                    half lum = Luminance(color / (1.0h + color));
                    half dark = saturate((0.32h - lum) / 0.32h);
                    half dot = step(length(cell), dark * 0.62h);
                    color = lerp(color, color * 0.35h, dot * _NasconHalftone);
                }

                // ---- ink outlines
                if (_NasconInk > 0.0 && !sky)
                {
                    float2 o = _InkWidth / _ScreenParams.xy;
                    float d1 = EyeDepth(uv + float2(o.x, 0)), d2 = EyeDepth(uv - float2(o.x, 0));
                    float d3 = EyeDepth(uv + float2(0, o.y)), d4 = EyeDepth(uv - float2(0, o.y));
                    float dEdge = (abs(d1 - depth) + abs(d2 - depth) + abs(d3 - depth) + abs(d4 - depth)) / max(depth, 0.1);
                    dEdge = saturate((dEdge - 0.08) * 6.0 * _DepthSensitivity);

                    float3 n = SampleSceneNormals(uv);
                    float3 n1 = SampleSceneNormals(uv + float2(o.x, 0)), n2 = SampleSceneNormals(uv - float2(o.x, 0));
                    float3 n3 = SampleSceneNormals(uv + float2(0, o.y)), n4 = SampleSceneNormals(uv - float2(0, o.y));
                    float nEdge = (1 - dot(n, n1)) + (1 - dot(n, n2)) + (1 - dot(n, n3)) + (1 - dot(n, n4));
                    nEdge = saturate((nEdge - 0.35) * 2.5 * _NormalSensitivity);

                    float wobble = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 7.0).r; // hand-drawn line breakup
                    float ink = saturate(max(dEdge, nEdge) * (0.75 + wobble * 0.5));
                    ink *= saturate(1.0 - depth / _InkFade);
                    // watercolour edge: the surface's own pigment pools darker at its edges (no black cartoon line)
                    half3 pigment = Saturate(color, 0.35h) * 0.42h;
                    color = lerp(color, pigment, ink * _NasconInk);
                }

                // ---- the Void's darkness flood
                if (_NasconVoid.w > 0.0)
                {
                    float3 world = sky ? _WorldSpaceCameraPos + normalize(ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP) - _WorldSpaceCameraPos) * 1000.0
                                       : ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                    float n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, world.xz * 0.05 + world.y * 0.03 + _Time.y * 0.08).r;
                    float n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 3.0 - _Time.y * 0.15).r;
                    float dist = distance(world, _NasconVoid.xyz) + (n - 0.5) * _NasconVoidSoft * 2.0 + (n2 - 0.5) * _NasconVoidSoft;
                    // a shockwave of darkness: a black band sweeps outward; what it has passed is left dimmed and sick
                    float inside = 1.0 - smoothstep(_NasconVoid.w - _NasconVoidSoft * 0.25, _NasconVoid.w, dist);
                    float band = inside * smoothstep(_NasconVoid.w - _NasconVoidSoft * 2.2, _NasconVoid.w - _NasconVoidSoft * 0.3, dist);
                    float rim = inside * smoothstep(_NasconVoid.w - _NasconVoidSoft * 0.5, _NasconVoid.w - _NasconVoidSoft * 0.1, dist);
                    half3 dimmed = lerp(color, Luminance(color).xxx * half3(0.8, 0.6, 0.72), 0.35h) * (1.0h - _NasconVoidDark);
                    color = lerp(color, dimmed, inside);
                    color = lerp(color, half3(0.004, 0.0, 0.003), band);
                    color += half3(1.4, 0.15, 0.05) * rim * 0.7;   // a burning rim on the wave front
                }

                // ---- impact frame: hard black/white negative
                if (_NasconImpact > 0.0)
                {
                    half lum = Luminance(color / (1.0h + color));
                    half3 negative = lum > 0.08h ? half3(0.02, 0.0, 0.01) : half3(1.0, 0.94, 0.9);
                    color = lerp(color, negative * 2.0h, saturate(_NasconImpact));
                }

                return half4(max(color, 0), 1);
            }
            ENDHLSL
        }
    }
}

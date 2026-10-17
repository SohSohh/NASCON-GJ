// Full-screen painterly pass (URP Full Screen Pass Renderer Feature, before post-processing).
//  1. Kuwahara filter: flattens the image into brush-stroke patches while keeping edges crisp.
//  2. Palette map: nudges luminance toward the concept-board ramp (purple-black -> maroon -> orange -> cream).
//  3. Paper grain: multiplies a subtle speckle so flat areas read as painted texture.
Shader "NASCON/PainterlyPost"
{
    Properties
    {
        _Radius("Brush Radius (px)", Range(0, 6)) = 3
        _PaletteStrength("Palette Strength", Range(0, 1)) = 0.3
        _PaletteShadow("Palette Shadow", Color) = (0.07, 0.04, 0.08, 1)
        _PaletteMid("Palette Mid", Color) = (0.5, 0.25, 0.12, 1)
        _PaletteLight("Palette Light", Color) = (1, 0.64, 0.3, 1)
        _PaletteHigh("Palette Highlight", Color) = (1, 0.92, 0.76, 1)
        _PaperTex("Paper Grain", 2D) = "gray" {}
        _PaperScale("Paper Scale", Float) = 3
        _PaperStrength("Paper Strength", Range(0, 0.5)) = 0.12
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Painterly"
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

            float _Radius;
            float _NasconBrushReduce; // global, set by the Settings menu (0 = full strength)
            half _PaletteStrength;
            half _NasconPaletteReduce; // global: cutscenes ease the warm palette off for daylight shots (0 = full)
            half4 _PaletteShadow;
            half4 _PaletteMid;
            half4 _PaletteLight;
            half4 _PaletteHigh;
            float _PaperScale;
            half _PaperStrength;
            TEXTURE2D(_PaperTex); SAMPLER(sampler_PaperTex);

            half3 Compress(half3 c) { return c / (1.0h + c); }
            half3 Expand(half3 c) { return c / max(1.0h - c, 0.02h); }

            half3 Palette(half t)
            {
                half3 a = lerp(_PaletteShadow.rgb, _PaletteMid.rgb, smoothstep(0.0h, 0.33h, t));
                half3 b = lerp(a, _PaletteLight.rgb, smoothstep(0.3h, 0.66h, t));
                return lerp(b, _PaletteHigh.rgb, smoothstep(0.62h, 0.95h, t));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 texel = _BlitTexture_TexelSize.xy;
                int r = (int)max(0, _Radius - _NasconBrushReduce);

                half3 color;
                if (r <= 0)
                {
                    color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0).rgb;
                }
                else
                {
                    // Classic 4-quadrant Kuwahara, computed in compressed (0..1) space so bright flames don't dominate.
                    half3 mean[4] = { (half3)0, (half3)0, (half3)0, (half3)0 };
                    half3 sq[4] = { (half3)0, (half3)0, (half3)0, (half3)0 };
                    [loop] for (int y = -r; y <= r; y++)
                    {
                        [loop] for (int x = -r; x <= r; x++)
                        {
                            half3 c = Compress(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(x, y) * texel, 0).rgb);
                            half3 c2 = c * c;
                            if (x <= 0 && y <= 0) { mean[0] += c; sq[0] += c2; }
                            if (x >= 0 && y <= 0) { mean[1] += c; sq[1] += c2; }
                            if (x <= 0 && y >= 0) { mean[2] += c; sq[2] += c2; }
                            if (x >= 0 && y >= 0) { mean[3] += c; sq[3] += c2; }
                        }
                    }
                    half n = (half)((r + 1) * (r + 1));
                    half best = 1e5h;
                    half3 result = 0;
                    [unroll] for (int i = 0; i < 4; i++)
                    {
                        half3 m = mean[i] / n;
                        half3 v = abs(sq[i] / n - m * m);
                        half variance = v.r + v.g + v.b;
                        if (variance < best) { best = variance; result = m; }
                    }
                    color = Expand(result);
                }

                // palette map (luminance preserved)
                half lum = Luminance(color);
                half t = lum / (1.0h + lum);
                half3 mapped = Palette(t);
                mapped *= lum / max(Luminance(mapped), 1e-4h);
                color = lerp(color, mapped, _PaletteStrength * saturate(1.0h - _NasconPaletteReduce));

                // paper grain
                float aspect = _ScreenParams.x / _ScreenParams.y;
                half grain = SAMPLE_TEXTURE2D(_PaperTex, sampler_PaperTex, uv * float2(aspect, 1) * _PaperScale).r;
                color *= 1.0h + (grain - 0.8h) * 5.0h * _PaperStrength;

                return half4(max(color, 0), 1);
            }
            ENDHLSL
        }
    }
}

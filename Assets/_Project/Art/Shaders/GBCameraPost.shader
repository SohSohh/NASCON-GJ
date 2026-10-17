// URP adapter for RogueNoodle/GBCamera's two-channel palette lookup.
Shader "NASCON/GBCameraPost"
{
    Properties
    {
        _Palette("GBCamera Palette", 2D) = "white" {}
        _PixelHeight("Vertical Resolution", Range(72, 720)) = 288
        _PaletteStrength("Palette Strength", Range(0, 1)) = 1
        _Exposure("Palette Exposure", Range(0.25, 4)) = 1
        _LightFalloff("Preserve Light Falloff", Range(0, 1)) = 0.8
        _MidtoneLift("Nearby Detail Lift", Range(0, 0.3)) = 0.08
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off
        Pass
        {
            Name "GBCamera"
            Stencil
            {
                Ref 128
                ReadMask 128
                WriteMask 0
                Comp NotEqual
                Pass Keep
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            TEXTURE2D(_Palette);
            float _PixelHeight;
            half _PaletteStrength;
            half _Exposure;
            half _LightFalloff;
            half _MidtoneLift;
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float height = min(_PixelHeight, _BlitTexture_TexelSize.w);
                float2 grid = float2(round(height * _BlitTexture_TexelSize.z / _BlitTexture_TexelSize.w), height);
                float2 uv = (floor(input.texcoord * grid) + 0.5) / grid;
                half4 source = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0);
                // The supplied GBPalette shader indexes the LUT with red and green, not luminance.
                // Convert the linear camera result to display space before indexing its legacy LUT.
                half3 display = LinearToSRGB(saturate(source.rgb * _Exposure));
                half brightness = dot(display, half3(0.2126h, 0.7152h, 0.0722h));
                // Lift the transition around torchlight, without lifting unlit black levels.
                display = saturate(display + _MidtoneLift * 4.0h * display * (1.0h - display));
                half3 palette = SAMPLE_TEXTURE2D_LOD(_Palette, sampler_PointClamp, saturate(display.rg), 0).rgb;
                // A fixed LUT shadow colour otherwise flattens every dark surface into a green wall.
                half falloff = lerp(0.12h, 1.0h, smoothstep(0.0h, 0.55h, brightness));
                palette *= lerp(1.0h, falloff, _LightFalloff);
                return half4(lerp(source.rgb, palette, _PaletteStrength), source.a);
            }
            ENDHLSL
        }
    }
}

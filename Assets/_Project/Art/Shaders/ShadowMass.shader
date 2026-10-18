// The escape's entity: a seething mass of shadow drawn on meshes conformed onto the walls, ceiling and floor
// (ShadowSurfaceSkin). Never the same shape twice: domain-warped noise boils inside it, tendrils reach out ahead
// of it, ash flakes tear off its edges, and eyes open and blink inside it.
// Per renderer (_Seg): x = distance behind the head at v=0, y = at v=1, z = width across (m), w = seed.
// _Seg2.x = 1 pairs the eyes along the length (walls), 0 across it (floor/ceiling). _Seg2.y = length override.
// _Seg2.z = 1: a veil hung across the corridor (solid middle, ragged edges); _Seg2.w = its height (m).
Shader "NASCON/ShadowMass"
{
    Properties
    {
        _BodyColor("Body", Color) = (0.012, 0.004, 0.012, 0.97)
        _VeinColor("Veins", Color) = (0.22, 0.03, 0.18, 1)
        [HDR] _RimColor("Rim", Color) = (0.9, 0.04, 0.1, 1)
        [HDR] _EyeColor("Eyes", Color) = (4, 0.7, 0.15, 1)
        [HDR] _EmberColor("Embers", Color) = (1.2, 0.25, 0.05, 1)
        _Length("Length (m)", Float) = 8
        _Spread("Spread", Range(0, 1)) = 1
        _Opacity("Opacity", Range(0, 1)) = 1
        _Seg("Segment", Vector) = (8, 0, 4, 0)
        _Seg2("Segment 2", Vector) = (0, 0, 0, 0)
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
            Name "ShadowMass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BodyColor;
                half4 _VeinColor;
                half4 _RimColor;
                half4 _EyeColor;
                half4 _EmberColor;
                float _Length;
                half _Spread;
                half _Opacity;
                float4 _Seg;
                float4 _Seg2;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            float Hash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }

            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float s = 0.0, a = 0.5;
                for (int k = 0; k < 4; k++) { s += a * Noise(p); p = p * 2.03 + 17.1; a *= 0.5; }
                return s;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float seed = _Seg.w;
                float d = lerp(_Seg.x, _Seg.y, input.uv.y);        // metres behind the head
                float x = input.uv.x * _Seg.z;                     // metres across the surface
                float bodyD = d;
                if (_Seg2.z > 0.5)
                {
                    // veil: the pattern runs up it, its body thickens away from its edges
                    float2 m = float2(x, input.uv.y * _Seg2.w);
                    float edge = min(min(m.x, _Seg.z - m.x), min(m.y, _Seg2.w - m.y));
                    d = m.y + 3.0;
                    bodyD = edge * 2.2;
                }

                // the boil: warped noise that keeps changing shape
                float w = Fbm(float2(x * 0.45, d * 0.35 + t * 0.55) + seed);
                float n = Fbm(float2(x * 0.7 + w * 1.6, d * 0.55 + t * 1.15 - w) + seed * 3.1);
                // tendrils: noise stretched along the chase, racing forward out of the head
                float s = Fbm(float2(x * 1.9 + w * 0.8, d * 0.18 + t * 1.9) + seed * 1.7);

                float len = _Seg2.y > 0.0 ? _Seg2.y : _Length;
                float body = smoothstep(0.0, 2.6, bodyD) * (1.0 - smoothstep(len - min(2.0, len * 0.6), len, bodyD));
                float reach = (1.0 - smoothstep(0.0, 2.6, bodyD)) * smoothstep(0.52, 0.72, s);
                float edgeX = smoothstep(0.0, 0.6 + n * 0.8, x) * smoothstep(0.0, 0.6 + n * 0.8, _Seg.z - x);
                float field = (body * 0.72 + n * 0.55 + reach * 0.75) * lerp(0.75, 1.0, edgeX) - (1.0 - _Spread) * 1.15;
                float mask = smoothstep(0.56, 0.6, field);

                // ash tearing off the edges: flakes in the band just outside the mass, drifting toward the head
                float band = smoothstep(0.38, 0.5, field) * (1.0 - mask);
                float2 cell = floor(float2(x * 7.0, d * 7.0 + t * 5.0) + seed * 9.0);
                float flake = step(0.9, Hash(cell)) * band;

                // eyes: some cells hold a pair that opens, stares and blinks
                float2 ec = float2(x / 1.3, (d - 1.0) / 1.5);
                float2 ei = floor(ec), ef = frac(ec) - 0.5;
                float h = Hash(ei + seed * 5.0);
                float open = step(0.68, h) * step(1.4, bodyD) * step(bodyD, len - 1.6);
                float blink = frac(t * (0.18 + h * 0.2) + h * 13.0);
                open *= smoothstep(0.0, 0.06, blink) * (1.0 - smoothstep(0.9, 0.96, blink)) * _Spread;
                float2 q = ef * float2(1.3, 1.5);                  // to metres
                float2 pairAxis = _Seg2.x > 0.5 ? float2(0, 1) : float2(1, 0);
                float2 a = q - pairAxis * 0.17, b = q + pairAxis * 0.17;
                float2 squash = _Seg2.x > 0.5 ? float2(0.06, 0.12) : float2(0.12, 0.06);
                float eye = max(1.0 - length(a / squash), 1.0 - length(b / squash));
                eye = saturate(eye * 3.0) * open * mask;

                // veins and embers glowing faintly inside, so it reads even where it is dark
                float vein = smoothstep(0.62, 0.7, Fbm(float2(x * 2.6 - t * 0.3, d * 2.2 + t * 0.6) + seed * 2.3));
                float ember = step(0.996, Hash(floor(float2(x * 18.0, d * 18.0 - t * 2.0)))) * mask * 0.5;

                // a raw red rim where it meets the world, flickering, so its shape reads even in the dark
                float rim = smoothstep(0.44, 0.56, field) * (1.0 - smoothstep(0.58, 0.7, field));
                rim *= 0.35 + 0.65 * smoothstep(0.35, 0.75, Noise(float2(x * 3.0 + t * 4.0, d * 3.0 - t * 5.0)));

                float alpha = saturate(mask + flake) * _BodyColor.a * _Opacity;
                half3 col = lerp(_BodyColor.rgb, _VeinColor.rgb, vein * mask) * alpha;
                col += _EyeColor.rgb * eye * _Opacity + _EmberColor.rgb * ember * _Opacity + _RimColor.rgb * rim * 0.65 * _Opacity * _Spread;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}

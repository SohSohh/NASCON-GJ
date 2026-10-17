// Analog-horror memory flash for the fall into the tomb (UI RawImage material): torn scanline rows,
// RGB split, rolling, grain, a faded tape tint and a vignette. MemoryFall drives _Glitch/_Split/_Roll per frame.
Shader "NASCON/MemoryGlitch"
{
    Properties
    {
        [PerRendererData] _MainTex("Memory", 2D) = "black" {}
        _Glitch("Glitch", Range(0, 1)) = 0.3
        _Split("RGB Split", Range(0, 0.05)) = 0.006
        _Roll("Vertical Roll", Range(0, 1)) = 0
        _Noise("Grain", Range(0, 1)) = 0.5
        _Tint("Tape Tint", Color) = (0.95, 0.82, 0.7, 1)
        _Fade("Desaturate", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Glitch, _Split, _Roll, _Noise, _Fade;
            fixed4 _Tint;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = floor(_Time.y * 24.0);
                float row = floor(uv.y * 70.0);
                float tear = step(1.0 - 0.35 * _Glitch, hash(float2(row, t)));            // some rows jump sideways
                uv.x += (hash(float2(row * 1.3, t + 7.0)) - 0.5) * 0.18 * _Glitch * tear;
                uv.x += sin(uv.y * 40.0 + _Time.y * 9.0) * 0.003 * _Glitch;               // tape wobble
                uv.y = frac(uv.y + _Roll);
                float s = _Split * (1.0 + tear * 3.0);
                fixed3 c;
                c.r = tex2D(_MainTex, uv + float2(s, 0)).r;
                c.g = tex2D(_MainTex, uv).g;
                c.b = tex2D(_MainTex, uv - float2(s, 0)).b;
                float l = dot(c, float3(0.3, 0.59, 0.11));
                c = lerp(c, l * _Tint.rgb * 1.3, _Fade);
                c *= 0.82 + 0.18 * sin(i.uv.y * 1100.0);                                   // scanlines
                c += (hash(i.uv * 900.0 + _Time.y * 61.0) - 0.5) * 0.3 * _Noise;          // grain
                float2 v = i.uv - 0.5;
                c *= saturate(1.0 - dot(v, v) * 1.9);                                       // vignette
                c *= step(abs(frac(i.uv.y * 0.5 + _Time.y * 0.7) - 0.5), 0.48) * 0.15 + 0.85; // rolling bar
                return fixed4(c, 1) * i.color;
            }
            ENDCG
        }
    }
}

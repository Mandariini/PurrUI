Shader "Hidden/PurrUI/RectangleRenderer"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            // Vertex attributes packed by OnPopulateMesh:
            //   uv0: texU, texV, width, height
            //   uv1: roundness (x, y, z, w)
            //   uv2: outlineSize, shadowSize, shadowBlur, shadowPow
            //   uv3: packedOutlineColor (xy), packedShadowColor (zw)
            //   color: graphicColor * Graphic.color

            struct appdata
            {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float4 tangent  : TANGENT;
                float4 color    : COLOR;
                float4 uv0      : TEXCOORD0;
                float4 uv1      : TEXCOORD1;
                float4 uv2      : TEXCOORD2;
                float4 uv3      : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex       : SV_POSITION;
                fixed4 color        : COLOR;
                float4 texAndSdf    : TEXCOORD0; // xy=texcoord, zw=sdfPosition
                float4 roundness    : TEXCOORD1;
                float4 params       : TEXCOORD2; // xy=halfSize, z=outlineSize, w=shadowSize
                float4 params2      : TEXCOORD3; // x=shadowBlur, y=shadowPow, zw=worldPos
                half4 outlineColor  : TEXCOORD4;
                half4 shadowColor   : TEXCOORD5;
                float3 emboss       : TEXCOORD6; // x=size, y=angle, z=strength
                half4 embossHColor  : TEXCOORD7;
                half4 embossLColor  : TEXCOORD8;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            // Unpack a Color32 from 2 floats (r+g*256, b+a*256)
            half4 UnpackColor(float2 packed)
            {
                half g = floor(packed.x / 256.0);
                half r = packed.x - g * 256.0;
                half a = floor(packed.y / 256.0);
                half b = packed.y - a * 256.0;
                return half4(r, g, b, a) / 255.0;
            }

            v2f vert(appdata v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.color = v.color * _Color;

                // UV transform for padding
                float2 size = v.uv0.zw;
                float padding = v.uv2.x + v.uv2.y + v.uv2.z + 1.0; // outline + shadow + blur + 1px margin
                float2 normPad = padding / size;
                float2 uv = v.uv0.xy * (1 + normPad * 2) - normPad;

                OUT.texAndSdf = float4(uv, (uv - 0.5) * size);
                OUT.roundness = v.uv1;
                OUT.params = float4(size * 0.5, v.uv2.xy);
                OUT.params2 = float4(v.uv2.zw, v.vertex.xy);

                // Unpack colors in vertex shader (4 verts) instead of fragment (thousands)
                OUT.outlineColor = UnpackColor(v.uv3.xy);
                OUT.shadowColor = UnpackColor(v.uv3.zw);
                OUT.emboss = v.normal;
                OUT.embossHColor = UnpackColor(v.tangent.xy);
                OUT.embossLColor = UnpackColor(v.tangent.zw);

                return OUT;
            }

            float sdRoundedBox(float2 p, float2 b, float4 r)
            {
                r.xy = (p.x > 0.0) ? r.xy : r.zw;
                r.x = (p.y > 0.0) ? r.x : r.y;
                float2 q = abs(p) - b + r.x;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 halfSize = IN.params.xy;
                float outlineSize = IN.params.z;
                float shadowSize = IN.params.w;
                float shadowBlur = IN.params2.x;
                float shadowPow = IN.params2.y;

                half4 graphic = tex2D(_MainTex, IN.texAndSdf.xy) + _TextureSampleAdd;

                float dist = sdRoundedBox(IN.texAndSdf.zw, halfSize, IN.roundness);
                float delta = fwidth(dist);

                // Outward-only AA: shapes are solid up to their SDF boundary
                // and only fade outside. Adjacent elements share a solid seam.
                float fill = 1 - smoothstep(0, delta, dist);

                float outline = outlineSize > 0
                    ? 1 - smoothstep(outlineSize, outlineSize + delta, dist)
                    : 0;

                float shadow = 0;
                float shadowRange = shadowSize + shadowBlur;
                if (shadowRange > 0)
                {
                    float shadowEdge = outlineSize + shadowSize;
                    shadow = 1 - smoothstep(
                        shadowEdge - shadowBlur,
                        shadowEdge + delta,
                        dist);
                    shadow = pow(shadow, shadowPow);
                }

                // Emboss: offset SDF samples along light direction for 3D bevel
                half3 fillRgb = graphic.rgb * IN.color.rgb;

                float embossSize = IN.emboss.x;
                if (embossSize > 0)
                {
                    float2 lightDir = float2(cos(IN.emboss.y), sin(IN.emboss.y));
                    float2 offset = lightDir * embossSize;

                    float dA = sdRoundedBox(IN.texAndSdf.zw + offset, halfSize, IN.roundness);
                    float dB = sdRoundedBox(IN.texAndSdf.zw - offset, halfSize, IN.roundness);

                    // Directional derivative ≈ dot(surfaceNormal, lightDir)
                    float light = clamp((dA - dB) / (embossSize * 2.0), -1.0, 1.0);
                    light = sign(light) * pow(abs(light), 1.0 + (1.0 - IN.emboss.z) * 4.0);

                    // Band mask: emboss only near the edge, fades inward
                    float bandMask = smoothstep(-embossSize, 0, dist);

                    // Lerp toward highlight or shadow tint
                    // Use saturate(fill/delta) to mask out AA fringe — emboss only where fill is solid
                    half3 tint = light > 0 ? IN.embossHColor.rgb : IN.embossLColor.rgb;
                    fillRgb = lerp(fillRgb, tint, abs(light) * bandMask * saturate(fill * 8));
                }

                // vertex.color = graphicColor * Graphic.color
                // .a is master alpha (CanvasGroup compatible)
                float masterAlpha = IN.color.a;
                float gA = fill * graphic.a * masterAlpha;
                float oA = outline * IN.outlineColor.a * masterAlpha;
                float sA = shadow * IN.shadowColor.a * masterAlpha;

                // Inlined Over(fill, Over(outline, shadow)) in premultiplied space
                float oneMinusGA = 1 - gA;
                float oneMinusOA = 1 - oA;

                half4 result;
                result.rgb = fillRgb * gA
                    + IN.outlineColor.rgb * oA * oneMinusGA
                    + IN.shadowColor.rgb * sA * oneMinusOA * oneMinusGA;
                result.a = gA
                    + oA * oneMinusGA
                    + sA * oneMinusOA * oneMinusGA;

                #ifdef UNITY_UI_CLIP_RECT
                result *= UnityGet2DClipping(IN.params2.zw, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}

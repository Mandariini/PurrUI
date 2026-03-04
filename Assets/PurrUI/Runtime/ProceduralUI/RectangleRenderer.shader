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

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float2 sdfPosition   : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float4 _Roundness;
            float _Padding;
            float _OutlineSize;
            fixed4 _GraphicColor;
            fixed4 _OutlineColor;

            float _ShadowSize;
            float _ShadowBlur;
            float _ShadowPow;
            fixed4 _ShadowColor;

            float2 _Size;

            v2f vert(appdata v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.color = v.color * _Color;

                // Pre-transform UV and SDF position in vertex shader
                float2 normalizedPadding = _Padding / _Size;
                float2 uv = v.texcoord * (1 + normalizedPadding * 2) - normalizedPadding;
                OUT.texcoord = uv;
                OUT.sdfPosition = (uv - 0.5) * _Size;

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
                float2 halfSize = _Size * 0.5;

                half4 graphic = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * _GraphicColor;

                float dist = sdRoundedBox(IN.sdfPosition, halfSize, _Roundness);
                float delta = fwidth(dist);

                // Full-region coverages for correct Over compositing:
                // each layer covers its full interior so the front layer
                // occludes the back, and at AA edges the back layer
                // fills the remaining coverage with no gap.

                float fill = 1 - smoothstep(-delta, delta, dist);

                float outline = _OutlineSize > 0
                    ? 1 - smoothstep(_OutlineSize - delta, _OutlineSize + delta, dist)
                    : 0;

                float shadow = 0;
                float shadowRange = _ShadowSize + _ShadowBlur;
                if (shadowRange > 0)
                {
                    float shadowEdge = _OutlineSize + _ShadowSize;
                    shadow = 1 - smoothstep(
                        shadowEdge - _ShadowBlur - delta,
                        shadowEdge + delta,
                        dist);
                    shadow = pow(shadow, _ShadowPow);
                }

                // Premultiplied layer alphas
                float gA = fill * graphic.a;
                float oA = outline * _OutlineColor.a;
                float sA = shadow * _ShadowColor.a;

                // Inlined Over(graphic, Over(outline, shadow)) in premultiplied space
                float oneMinusGA = 1 - gA;
                float oneMinusOA = 1 - oA;

                half4 result;
                result.rgb = graphic.rgb * gA
                    + _OutlineColor.rgb * oA * oneMinusGA
                    + _ShadowColor.rgb * sA * oneMinusOA * oneMinusGA;
                result.a = gA
                    + oA * oneMinusGA
                    + sA * oneMinusOA * oneMinusGA;

                // Apply vertex color (Graphic.color) as tint in premultiplied space
                result = half4(result.rgb * IN.color.rgb * IN.color.a, result.a * IN.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                result *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
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

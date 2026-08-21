// 地圖節點的 hover 高亮。
//
// 【為什麼需要自訂 shader】內建的 UI tint（`Image.color`）是**乘法**：
// 節點平常是純白 tint ＝ 已經在上限，往上沒有空間；而這批圖是水墨風的
// 黑筆觸 ＋ 暗紅圈，黑色乘任何數還是黑色。所以「hover 變鮮豔」用 tint 做不出來。
//
// 【視覺通道的分工】明暗（`Image.color` 與 CanvasGroup alpha）已經被**狀態**用掉了
// （當前 / 可前往 / 去不了）。這支只動**飽和度**與**加一層暖光**，
// 兩者互不干擾 —— 暗的節點就算 hover 也還是暗的，不會假裝自己可以點。
//
// 【為什麼整支照抄 UI-Default】UI 的裁切（RectMask2D 的 _ClipRect、Mask 的 stencil）
// 是在 shader 裡做的。自己從頭寫一支簡單的，地圖只要放進任何遮罩就會**穿出去**，
// 而且那種 bug 看起來像排版壞掉，不會有人聯想到 shader。
// 所以這支＝ UI-Default 原封不動 ＋ 最後三行顏色處理。
Shader "TYN/UI/MapNodeHighlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Hover)]
        _Saturation ("飽和度（1 = 原樣）", Range(0, 3)) = 1
        _Glow ("暖光量（0 = 關）", Range(0, 1)) = 0
        _GlowColor ("暖光顏色", Color) = (1, 0.42, 0.36, 1)

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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            half _Saturation;
            half _Glow;
            fixed4 _GlowColor;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // ── hover 高亮：只動顏色，不動 alpha ──
                // 飽和度先做，暖光後做。反過來的話暖光會被飽和度再拉一次，
                // 兩個旋鈕就不獨立，調起來會互相打架。
                half lum = dot(color.rgb, half3(0.299, 0.587, 0.114));
                color.rgb = lerp(half3(lum, lum, lum), color.rgb, _Saturation);
                // 黑筆觸的飽和度是 0，光靠上一行不會有任何變化 ——
                // 所以要再往暖光色 lerp 一次，讓墨線也跟著亮起來
                color.rgb = lerp(color.rgb, _GlowColor.rgb, _Glow);

                return color;
            }
        ENDCG
        }
    }
}

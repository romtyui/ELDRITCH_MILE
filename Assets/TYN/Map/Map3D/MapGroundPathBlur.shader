// 地圖底圖：路徑周圍一圈局部模糊＋提亮（2026-11-23 組員反饋：路徑顏色跟地圖相近，看起來很亂）
//
// 原理：MapView3D 建圖時把每條路的弧線畫進一張遮罩（_PathMask，R 通道，底圖 UV 空間），
// 這裡在遮罩有值的地方把底圖換成模糊版、再往紙色提亮一點 —— 路旁的建築線條、虛線、等高線被抹淡，
// 深色的路線就浮出來了。遮罩外（大部分畫面）只取樣一次，跟原本的 URP Unlit 一樣便宜。
//
// 其他行為照抄原本的設定：Alpha Clip（_Cutoff 0.5）、背面剔除、Queue 2450。
// 屬性名沿用 _BaseMap —— MapView3D.ApplyRegionGround 換地區底圖時寫的就是它。
Shader "EldritchMile/MapGroundPathBlur"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Path Halo)]
        _PathMask ("Path Mask (R, runtime)", 2D) = "black" {}
        _BlurRadius ("Blur Radius (UV)", Range(0, 0.02)) = 0.004
        _BlurStrength ("Blur Strength", Range(0, 1)) = 1
        _Lighten ("Lighten", Range(0, 1)) = 0.35
        _LightenColor ("Lighten Toward", Color) = (0.94, 0.94, 0.96, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Unlit"
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PathMask); SAMPLER(sampler_PathMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                float _BlurRadius;
                half _BlurStrength;
                half _Lighten;
                half4 _LightenColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 maskUv : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.maskUv = v.uv;
                return o;
            }

            // 12 點 Poisson 盤，半徑 1
            static const float2 kTaps[12] =
            {
                float2(-0.326, -0.406), float2(-0.840, -0.074), float2(-0.696,  0.457),
                float2(-0.203,  0.621), float2( 0.962, -0.195), float2( 0.473, -0.480),
                float2( 0.519,  0.767), float2( 0.185, -0.893), float2( 0.507,  0.064),
                float2( 0.896,  0.412), float2(-0.322, -0.933), float2(-0.792, -0.598)
            };

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(c.a - _Cutoff);

                half m = SAMPLE_TEXTURE2D(_PathMask, sampler_PathMask, i.maskUv).r;

                UNITY_BRANCH
                if (m > 0.002)
                {
                    half3 sum = c.rgb;
                    float r = _BlurRadius;
                    UNITY_UNROLL
                    for (int k = 0; k < 12; k++)
                        sum += SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv + kTaps[k] * r).rgb;
                    half3 blurred = sum / 13.0;

                    c.rgb = lerp(c.rgb, blurred, m * _BlurStrength);
                    c.rgb = lerp(c.rgb, _LightenColor.rgb, m * _Lighten);
                }

                return half4(c.rgb * _BaseColor.rgb, 1);
            }
            ENDHLSL
        }
    }
}

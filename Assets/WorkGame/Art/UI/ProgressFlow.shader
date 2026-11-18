Shader "WorkGame/UI/Progress Flow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Pattern", 2D) = "white" {}
        _PatternColor ("Pattern Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,0.68,0.18,1)
        _CompletedColor ("Completed Color", Color) = (0.25,0.85,0.4,1)
        _ColorTransition ("Color Start Time / Duration", Vector) = (0,0,0,0)
        _Progress ("Progress", Range(0,1)) = 0.5
        _ProgressTransition ("From / Start Time / Duration", Vector) = (0,0,0,0)
        _FlowRect ("Local Rect", Vector) = (0,0,100,20)
        _FlowSettings ("Tile Size / Speed / Strength", Vector) = (160,24,0.3,0)
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 patternUV : TEXCOORD0;
                float2 progress : TEXCOORD1;
                float4 mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            fixed4 _PatternColor;
            fixed4 _BaseColor, _CompletedColor;
            float4 _ColorTransition;
            float _Progress;
            float4 _ProgressTransition;
            float4 _FlowRect;
            float4 _FlowSettings;
            float4 _ClipRect;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Canvas batching can transform vertex positions out of RectTransform
                // local space. RawImage's 0..1 UVs remain stable in every canvas mode.
                float2 local = v.texcoord * _FlowRect.zw;
                o.patternUV = local / _FlowSettings.x;
                // Positive speed moves the visible pattern to the right.
                o.patternUV.x -= frac(_Time.y * _FlowSettings.y / _FlowSettings.x);
                float t = _ProgressTransition.z > 0.0
                    ? saturate((_Time.y - _ProgressTransition.y) / max(_ProgressTransition.z, 0.0001))
                    : 1.0;
                float remaining = 1.0 - t;
                float shown = lerp(_ProgressTransition.x, _Progress,
                    1.0 - remaining * remaining * remaining);
                o.progress = float2(v.texcoord.x, shown);
                float2 pixelSize = o.vertex.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = float4(v.vertex.xy * 2 - rect.xy - rect.zw,
                    0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                    v.color.rgb = UIGammaToLinear(v.color.rgb);
                float colorT = _ColorTransition.y > 0.0
                    ? saturate((_Time.y - _ColorTransition.x) / max(_ColorTransition.y, 0.0001))
                    : 1.0;
                colorT = colorT * colorT * (3.0 - 2.0 * colorT);
                colorT *= step(1.0, _Progress);
                o.color = v.color * lerp(_BaseColor, _CompletedColor, colorT);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(i.progress.y - 0.000001);
                clip(i.progress.y - i.progress.x);
                // Explicit repeat also works with a Clamp-imported standalone texture.
                fixed4 pattern = tex2D(_MainTex, frac(i.patternUV));
                fixed4 color = i.color;
                color.rgb = lerp(color.rgb, pattern.rgb * _PatternColor.rgb,
                    saturate(pattern.a * _PatternColor.a * _FlowSettings.z));
                #ifdef UNITY_UI_CLIP_RECT
                half2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                color.a *= mask.x * mask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}

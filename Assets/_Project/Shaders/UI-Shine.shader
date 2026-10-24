// 그림 위로 비스듬한 빛줄기가 천천히 지나가는 UI 셰이더 (타이틀 로고).
// 그림의 불투명한 곳에만 빛이 얹힌다. 한 번 지나간 뒤 잠시 쉬었다가 다시 지나간다.
Shader "ItIsYou/UI/Shine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShineColor ("빛 색", Color) = (1, 0.96, 0.86, 1)
        _ShineStrength ("빛 세기", Range(0, 1.5)) = 0.35
        _ShineWidth ("빛줄기 폭", Range(0.01, 0.5)) = 0.09
        _ShineSlant ("기울기", Range(-1, 1)) = 0.35
        _ShineSweep ("한 번 지나가는 시간(초)", Float) = 1.6
        _ShinePeriod ("되풀이 간격(초)", Float) = 5.5
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
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
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _ShineColor;
            float _ShineStrength, _ShineWidth, _ShineSlant, _ShineSweep, _ShinePeriod;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;

                // 빛줄기 자리: 주기마다 왼쪽 밖 → 오른쪽 밖으로 _ShineSweep 초 동안 지나간다.
                float period = max(_ShinePeriod, _ShineSweep + 0.01);
                float t = fmod(_Time.y, period) / max(_ShineSweep, 0.01);
                float x = i.uv.x + (i.uv.y - 0.5) * _ShineSlant;
                float pos = lerp(-0.3, 1.3 + abs(_ShineSlant), saturate(t));
                float d = (x - pos) / _ShineWidth;
                float band = exp(-d * d) * step(t, 1.0);
                c.rgb += _ShineColor.rgb * band * _ShineStrength * c.a;

                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}

Shader "UI/MenuTextHover"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HoverRect ("Hover Rect", Vector) = (0,0,0,0)
        _HoverAmount ("Hover Amount", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _HoverRect;
            float _HoverAmount;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.texcoord) * input.color;
                float inside = step(_HoverRect.x, input.texcoord.x)
                    * step(_HoverRect.y, input.texcoord.y)
                    * step(input.texcoord.x, _HoverRect.z)
                    * step(input.texcoord.y, _HoverRect.w);

                fixed maximum = max(color.r, max(color.g, color.b));
                fixed minimum = min(color.r, min(color.g, color.b));
                fixed chroma = maximum - minimum;
                fixed luminance = dot(color.rgb, fixed3(0.299, 0.587, 0.114));

                // Chỉ bắt mực chữ gần như đen. Ngưỡng thấp giúp loại bỏ mặt đá,
                // bóng đổ và vết nứt xám nằm trong cùng hitbox.
                float darkText = 1.0 - smoothstep(0.055, 0.14, luminance);
                float neutralInk = 1.0 - smoothstep(0.035, 0.11, chroma);
                float highlight = inside * darkText * neutralInk * _HoverAmount;
                fixed3 glowColor = fixed3(0.70, 1.0, 0.24);
                color.rgb = lerp(color.rgb, glowColor, highlight);
                return color;
            }
            ENDCG
        }
    }
}

Shader "Custom/ElementalSpriteOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Overlay Color", Color) = (1,1,1,1)
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.uv;
                // Combine any colors passed from the material inspector or sprite tint script properties
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 baseColor = tex2D(_MainTex, IN.uv);

                // 1. Keep empty frame pixels completely transparent
                if (baseColor.a < 0.05f)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // 2. DETECT BLACK OUTLINES: If red, green, and blue are all dark, it's your outline pixel
                if (baseColor.r < 0.15f && baseColor.g < 0.15f && baseColor.b < 0.15f)
                {
                    // Return the original dark outline intact, multiplied by alpha for sprite standards
                    return fixed4(baseColor.rgb * baseColor.a, baseColor.a);
                }

                // 3. Extract the brightness footprint from the original green slime channel
                float brightness = baseColor.g;

                // 4. MAP CUSTOM ELEMENTAL HUES: Paint your target color cleanly over the brightness map
                // This replaces the green color, but keeps your highlights bright and your shadow areas dark!
                fixed3 finalRgb = IN.color.rgb * brightness;

                // Return final color with premultiplied alpha channels
                return fixed4(finalRgb * baseColor.a, baseColor.a);
            }
            ENDCG
        }
    }
}
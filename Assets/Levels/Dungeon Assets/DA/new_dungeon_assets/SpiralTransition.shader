Shader "UI/SpiralTransition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Header(Colors)]
        _Color ("Void Color (Inner)", Color) = (0.05, 0.02, 0.08, 1.0)
        _BorderColor ("Edge Rim Color (Glow)", Color) = (0.6, 0.2, 0.9, 1.0)
        _BorderSize ("Edge Rim Width", Range(0.0, 0.5)) = 0.15

        [Header(Archimedean Spiral Settings)]
        _Progress ("Progress", Range(0, 1)) = 0.0
        _SpiralTurns ("Spiral Density (Turns)", Float) = 6.0
        _RotationSpeed ("Vortex Spin Speed", Float) = 1.5

        [Header(Pixel Art Style)]
        _PixelResolution ("Pixel Snap (0 = Smooth, 180-320 = Pixel)", Float) = 240.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            fixed4 _BorderColor;
            float _BorderSize;
            float _Progress;
            float _SpiralTurns;
            float _RotationSpeed;
            float _PixelResolution;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. HARD GUARD: 100% completely clear at Progress 0 (Fixes residue bug)
                if (_Progress <= 0.0005)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // 2. HARD GUARD: Fully opaque solid color at Progress 1
                if (_Progress >= 0.9995)
                {
                    return _Color;
                }

                float2 uv = i.uv;

                // Pixel Art Quantization
                if (_PixelResolution > 0.0)
                {
                    uv = floor(uv * _PixelResolution) / _PixelResolution;
                }

                // Shift center to (0, 0)
                float2 centeredUV = uv - float2(0.5, 0.5);

                // Aspect Ratio Compensation
                float aspect = _ScreenParams.x / _ScreenParams.y;
                centeredUV.x *= aspect;

                // Polar Coordinates
                float dist = length(centeredUV);
                float angle = atan2(centeredUV.y, centeredUV.x);

                // Add active rotation spin
                angle += _Progress * _RotationSpeed * 6.2831853;

                // Normalize angle to [0..1)
                float normalizedAngle = frac((angle + 3.14159265359) / 6.28318530718);

                // True Archimedean Spiral Coordinate (starts at 0 near origin)
                float archimedeanSpiral = (dist * _SpiralTurns) + normalizedAngle;

                // Maximum threshold to guarantee corner coverage
                float maxThreshold = (_SpiralTurns * 0.7 * aspect) + 1.0;
                float threshold = _Progress * maxThreshold;

                // Multi-Layer Color Output
                if (archimedeanSpiral <= threshold)
                {
                    // Inner Void
                    return _Color;
                }
                else if (archimedeanSpiral <= threshold + _BorderSize)
                {
                    // Edge Glow Rim
                    return _BorderColor;
                }

                // Transparent background
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
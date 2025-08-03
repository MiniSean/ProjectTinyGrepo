Shader "Custom/RadialFill"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Progress ("Progress", Range(0, 1)) = 0.0
        _BorderWidth ("Border Width", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

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
            float _Progress;
            float _BorderWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Pass the UV coordinates from the vertex to the fragment shader.
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Remap UV coordinates from [0,1] to [-1,1] so the center is (0,0).
                float2 uv = (i.uv - 0.5) * 2.0;

                // Calculate the distance of the pixel from the center.
                float dist = length(uv);

                // If the pixel is outside the unit circle, discard it (make it transparent).
                if (dist > 1.0)
                {
                    clip(-1);
                }

                // Calculate the angle of the pixel in radians.
                // atan2 gives a result from -PI to +PI. We remap it to [0,1].
                float angle = (-atan2(uv.y, uv.x) / (2.0 * UNITY_PI)) + 0.5;

                // Calculate the target angle based on the progress.
                float progressAngle = _Progress;

                // Determine the alpha value.
                float alpha = 0.0;

                // Is the pixel part of the border?
                if (dist > (1.0 - _BorderWidth))
                {
                    alpha = 1.0; // Solid border
                }
                // Is the pixel part of the filled area?
                else if (angle < progressAngle)
                {
                    alpha = 0.5; // Transparent fill
                }

                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}

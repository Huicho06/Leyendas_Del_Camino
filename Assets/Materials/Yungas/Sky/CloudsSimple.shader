Shader "Custom/CloudsSimple"
{
    Properties
    {
        _MainTex ("Cloud Texture", 2D) = "white" {}
        _Speed ("Scroll Speed", Vector) = (0.01, 0.02, 0, 0)
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Intensity ("Brightness", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Front
        Lighting Off

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Speed;
            float4 _Color;
            float _Intensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Movimiento de textura
                float2 uv = i.uv + _Time.y * _Speed.xy;

                // Color base de la nube
                fixed4 col = tex2D(_MainTex, uv) * _Color * _Intensity;

                // Aplica un "fade" en la parte superior e inferior para ocultar el corte del polo
                float fadeTop = smoothstep(0.95, 1.0, i.uv.y);  // desvanecer arriba
                float fadeBottom = smoothstep(0.05, 0.0, i.uv.y); // desvanecer abajo
                float edgeMask = saturate(1.0 - (fadeTop + fadeBottom));

                col.a *= col.r * edgeMask;

                return col;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}

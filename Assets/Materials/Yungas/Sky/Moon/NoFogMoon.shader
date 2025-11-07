Shader "Custom/NoFogMoon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _EmissionColor ("Emission Color", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma exclude_renderers gles gles3 glcore // solo URP moderno
            #pragma multi_compile_fog OFF // 🚫 Desactiva la niebla

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : NORMAL;
                float2 uv          : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            float4 _BaseColor;
            float _Smoothness;
            float _Metallic;
            float4 _EmissionColor;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.uv = IN.uv;
                return OUT;
            }

half4 frag (Varyings IN) : SV_Target
{
    // Textura base y color
    half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb * _BaseColor.rgb;

    // Normal en espacio mundial
    half3 normalWS = normalize(IN.normalWS);

    // Luz principal
    Light mainLight = GetMainLight();
    half3 lightDir = normalize(mainLight.direction);
    half3 viewDir = normalize(IN.viewDirWS);

    // Difuso simple (Lambert)
    half NdotL = saturate(dot(normalWS, lightDir));
    half3 diffuse = albedo * mainLight.color.rgb * NdotL;

    // Especular suave
    half3 halfDir = normalize(lightDir + viewDir);
    half NdotH = saturate(dot(normalWS, halfDir));
    half3 specular = pow(NdotH, 64.0) * _Smoothness;

    // Emisión controlada
    half3 emission = _EmissionColor.rgb;

    // Color final (balanceado y limitado)
    half3 color = diffuse + specular + emission;
    color = saturate(color); // 🔥 evita valores fuera de rango

    return half4(color, 1.0);
}

            ENDHLSL
        }
    }
}

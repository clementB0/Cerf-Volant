Shader "Cerf-Volant/Kite Guide"
{
    Properties
    {
        [NoScaleOffset] _GuideMap ("Dashes", 2D) = "black" {}
        _GuideColor ("Guide color", Color) = (0.18, 0.19, 0.21, 0.78)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_GuideMap);
            SAMPLER(sampler_GuideMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _GuideColor;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 dash = SAMPLE_TEXTURE2D(_GuideMap, sampler_GuideMap, input.uv);
                half core = saturate(dash.r / max(dash.a, 0.001h));
                half3 color = lerp(half3(1, 1, 1), _GuideColor.rgb, core);
                return half4(color, _GuideColor.a * dash.a);
            }
            ENDHLSL
        }
    }
}

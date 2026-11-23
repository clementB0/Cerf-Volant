Shader "Workshop/GrabOutline"
{
    Properties
    {
        [HideInInspector] _ColorMask ("Color mask", Float) = 15
        [HideInInspector] _StencilTest ("Stencil comparison", Float) = 8
        [HideInInspector] _StencilPass ("Stencil pass", Float) = 0
        [HideInInspector] _PixelOffset ("Offset", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off ZTest LEqual ColorMask [_ColorMask]
            Stencil { Ref 1 ReadMask 1 WriteMask 1 Comp [_StencilTest] Pass [_StencilPass] }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _PixelOffset;
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionCS.xy += _PixelOffset.xy * 3.0 * output.positionCS.w / _ScaledScreenParams.xy;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target { return half4(0.95, 0.84, 0.57, 1); }
            ENDHLSL
        }
    }
}

Shader "Custom/DebugBlur"
{
        SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "Blur"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma multi_compile _UseColor_ON
            #pragma multi_compile _UseNormal_ON
            #pragma multi_compile _UseDepth_ON
            #pragma vertex Vert
            #pragma fragment frag

            TEXTURE2D_X(_CameraBlurTexture);
            SAMPLER(sampler_CameraBlurTexture);

            inline float3 SampleColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_CameraBlurTexture, sampler_CameraBlurTexture, uv);
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(SampleColor(input.texcoord), 0);
            }
            ENDHLSL
        }
    }
}
Shader "Custom/Outline"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "Outline"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            TEXTURE2D_X(_CameraDepthNormalsTexture);
            SAMPLER(sampler_CameraDepthNormalsTexture);

            inline float3 DecodeViewNormalStereo(float4 enc4)
            {
                float kScale = 1.7777;
                float3 nn = enc4.xyz * float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
                float g = 2.0 / dot(nn.xyz, nn.xyz);
                float3 n;
                n.xy = g * nn.xy;
                n.z = g - 1;
                return n;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 color = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, input.texcoord); 
                float3 depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord);
                float3 normal = DecodeViewNormalStereo(SAMPLE_TEXTURE2D_X(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, input.texcoord));

                return half4(normal, 1);
            }
            ENDHLSL
        }
    }
}
Shader "Custom/Outline"
{
    Properties
    {
        _SampleSize("Sample Size", Range(0.0, 25.0)) = 1.0
        [Header(Color)]
        [Toggle] _UseColor("Use Color Edge Detection", Float) = 1
        _ThresholdColor("Threshold Color", Range(0.0, 1.0)) = 1.0

        [Header(Normal)]
        [Toggle] _UseNormal("Use Normal Edge Detection", Float) = 1
        _ThresholdNormal("Threshold Normal", Range(0.0, 1.0)) = 1.0

        [Header(Depth)]
        [Toggle] _UseDepth("Use Depth Edge Detection", Float) = 1
        _ThresholdDepth("Threshold Depth", Range(0.0, 1000.0)) = 1.0
    }

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

            #pragma multi_compile _UseColor_ON
            #pragma multi_compile _UseNormal_ON
            #pragma multi_compile _UseDepth_ON
            #pragma vertex Vert
            #pragma fragment frag

            float _SampleSize;
            bool _UseColor;
            float _ThresholdColor;
            bool _UseNormal;
            float _ThresholdNormal;
            bool _UseDepth;
            float _ThresholdDepth;

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            float4 _CameraOpaqueTexture_TexelSize;

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

            inline float Linear01Depth(float z)
            {
                return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
            }

            inline float LinearEyeDepth(float z)
            {
                return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
            }

            inline float3 SampleColor(float2 uv) 
            {
                return SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
            }

            inline float SampleDepth(float2 uv)
            {
                return LinearEyeDepth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv)).x;
            }

            inline float3 SampleNormal(float2 uv) 
            {
                return DecodeViewNormalStereo(SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uv));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 texel = float2(_CameraOpaqueTexture_TexelSize.x, _CameraOpaqueTexture_TexelSize.y);

                float2 uvs[4];
                uvs[0] = float2(input.texcoord.x + _SampleSize * texel.x, input.texcoord.y);
                uvs[1] = float2(input.texcoord.x - _SampleSize * texel.x, input.texcoord.y);
                uvs[2] = float2(input.texcoord.x, input.texcoord.y + _SampleSize * texel.y);
                uvs[3] = float2(input.texcoord.x, input.texcoord.y - _SampleSize * texel.y);

                float3 color = SampleColor(input.texcoord);
                float3 normal = SampleNormal(input.texcoord);
                float3 depth = SampleDepth(input.texcoord);

                float3 convolutionMatrixResultColor = -4 * color;
                float3 convolutionMatrixResultNormal = -4 * normal;
                float convolutionMatrixResultDepth = -4 * depth;
                for (int i = 0; i < 4; ++i) 
                {
                    convolutionMatrixResultColor += SampleColor(uvs[i]);
                    convolutionMatrixResultNormal += SampleNormal(uvs[i]);
                    convolutionMatrixResultDepth += SampleDepth(uvs[i]);
                }

                convolutionMatrixResultColor = convolutionMatrixResultColor >= _ThresholdColor ? 1 : 0;
                convolutionMatrixResultNormal = convolutionMatrixResultNormal >= _ThresholdNormal ? 1 : 0;
                convolutionMatrixResultDepth = convolutionMatrixResultDepth >= _ThresholdDepth ? 1 : 0;

                float edge = convolutionMatrixResultNormal;

                return edge == 1 ? half4(0,0,0,0) : half4(1,1,1,0);
            }
            ENDHLSL
        }
    }
}
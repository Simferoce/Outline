Shader "Custom/Blur"
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

            int _GuassianKernel_Size;
            float _GuassianKernel[100];

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            float4 _CameraOpaqueTexture_TexelSize;

            inline float3 SampleColor(float2 uv) 
            {
                return SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 texel = float2(_CameraOpaqueTexture_TexelSize.x, _CameraOpaqueTexture_TexelSize.y);

                float3 color = 0;
                float weight = 0;
                for (int x = -_GuassianKernel_Size / 2; x < _GuassianKernel_Size / 2; ++x)
                {
                    for (int y = -_GuassianKernel_Size / 2; y < _GuassianKernel_Size / 2; ++y)
                    {
                        float current_weight = _GuassianKernel[abs(x) * _GuassianKernel_Size + abs(y)];

                        color += current_weight * SampleColor(float2(input.texcoord.x + x * texel.x, input.texcoord.y + y * texel.y));
                        weight += current_weight;
                    }
                }

                color /= weight;
                return half4(color, 0);
            }
            ENDHLSL
        }
    }
}
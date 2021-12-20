Shader "Just/Urp/HierarchicalZBuffer"
{

    SubShader
    {
         Cull Off ZWrite Off ZTest Always
        
        //Blit Depth Pass
        Pass
        {
            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
 
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            Texture2D _CameraDepthTexture;
            SamplerState sampler_CameraDepthTexture;



            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float camDepth = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, input.uv).r  * 1.8;
                return camDepth;
            }

            ENDHLSL
        }
        
        //Reduce Depth Pass
        Pass
        {
            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
 
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float4 _MainTex_TexelSize;

            Texture2D _CameraDepthTexture;
            SamplerState sampler_CameraDepthTexture;

            half4 Fragment(Varyings input) : SV_Target
            {
                int2 xy = (int2) (input.uv * (_MainTex_TexelSize.zw - 1));
                float4 texels[2] = {
                    float4(_MainTex.mips[0][xy].rg, _MainTex.mips[0][xy + int2(1, 0)].rg),
                    float4(_MainTex.mips[0][xy + int2(0, 1)].rg, _MainTex.mips[0][xy + 1].rg)
                };
            
                float4 r = float4(texels[0].rb, texels[1].rb);
                float4 g = float4(texels[0].ga, texels[1].ga);
                

                float minimum = min(min(min(r.x, r.y), r.z), r.w);
                float maximum = max(max(max(g.x, g.y), g.z), g.w);
                return float4(minimum, maximum, 1.0, 1.0);
            }

            ENDHLSL
        }
        
        //For Test
        Pass
        {
            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
 
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float4 _MainTex_TexelSize;


            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 color =  _MainTex.Sample(sampler_MainTex, input.uv);
 
                return color;
            }

            ENDHLSL
        }
    }
}

Shader "Just/Urp/IndirectUnlitShader"
{
    Properties
    {
         _MainTex("MainTexture",2D) = "white"{}
         _MainColor("MainColor",COLOR) =  (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" } 

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Library/IndirectShaderLibrary.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD1;
            };

CBUFFER_START(UnityPerMaterial)
            float4 _MainColor;
CBUFFER_END
            
            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                float3 worldPosition =  GetInstancePositionWS(instanceID,v.vertex); 
                float3 worldNormal = GetInstanceNormalWS(instanceID,v.normal);
                v2f o;
                o.positionWS = worldPosition;
                o.positionCS = TransformWorldToHClip(worldPosition);
                o.uv = v.uv;
                o.normalWS = worldNormal;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                Light light = GetMainLight();
                half ndotl = dot(i.normalWS,light.direction);
                return ndotl; 
            }
            ENDHLSL
        }
    }
}

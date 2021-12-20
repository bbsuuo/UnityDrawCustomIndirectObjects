#ifndef JUST_INDIRECTSHADERLIBRARY_INCLUDE
#define JUST_INDIRECTSHADERLIBRARY_INCLUDE

//Matrix4x4 v = Camera.main.worldToCameraMatrix;
//Matrix4x4 p = Camera.main.projectionMatrix; 

CBUFFER_START(UnityPerMaterial)
            float4x4 _LocalToWorld;
            //用于计算位置的数据,用于根据参数初始化信息  position: xyz, scale : w
            StructuredBuffer<float4x4> _Positions;
            //计算完剪裁后所有需要渲染的ID , 通过该数量调用渲染接口
            StructuredBuffer<uint> _VisibleInstanceIds; 
CBUFFER_END


float4x4 GetInstanceMatrix(uint instanceID)
{
  return _Positions[_VisibleInstanceIds[instanceID]];
}

float4 GetInstancePositionOS(uint instanceID,float4 positionOS)
{
	 float4x4 trsMat = GetInstanceMatrix(instanceID);
    float4 localPosition =   mul(trsMat,float4(positionOS.xyz,1)); //float4( data.xyz + positionOS.xyz * data.w ,1);
    return localPosition;
}

float3 GetInstancePositionWS(uint instanceID,float4 positionOS)
{
    float4 localPosition =   GetInstancePositionOS(instanceID,positionOS);
    float3 worldPosition =   mul(_LocalToWorld,  localPosition).xyz;
    return worldPosition;
} 

float3 GetInstanceNormalWS(uint instanceID,float3 normalOS)
{
   float4x4 rtsMat = GetInstanceMatrix(instanceID);
   return mul((float3x3)_LocalToWorld,mul((float3x3)rtsMat,normalOS));
}

#endif

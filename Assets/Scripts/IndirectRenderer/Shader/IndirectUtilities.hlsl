#ifndef JUST_INDIRECTUTILITIES_INCLUDE
#define JUST_INDIRECTUTILITIES_INCLUDE

//--------------------- Random Abount 
uint randomState;

Texture2D _HiZTextureTex;
SamplerState sampler_HiZTextureTex;
float _HiZTextureSize;

// Hash function www.cs.ubc.ca/~rbridson/docs/schechter-sca08-turbulence.pdf
uint hash(uint state)
{
    state ^= 2747636419u;
    state *= 2654435769u;
    state ^= state >> 16;
    state *= 2654435769u;
    state ^= state >> 16;
    state *= 2654435769u;
    return state;
}

float randomRange01(uint state)
{
    return state / 4294967295.0;
}

float random(float minV,float maxV)
{
	randomState = hash(randomState);
	return lerp(minV,maxV,randomRange01(randomState));
}

//-------------------- Culling About
inline uint IsCameraOutsideObjBounds(float3 pos, float3 minPos, float3 maxPos)
{
    float boundsSize = distance(maxPos, minPos);
    return ((distance(pos, maxPos) > boundsSize)
            + (distance(pos, minPos) > boundsSize));
}

inline uint IsVisibleAfterFrustumCulling(float4 clipPos)
{
    return (clipPos.z > clipPos.w 
            || clipPos.x < -clipPos.w 
            || clipPos.x > clipPos.w 
            || clipPos.y < -clipPos.w 
            || clipPos.y > clipPos.w) 
            ? 0 : 1;
}

// Hi-Z Culling
inline uint IsVisibleAfterOcclusionCulling(float clipMinX, float clipMaxX, float clipMinY, float clipMaxY, float clipMinZ)
{
    // -1 - 1 映射到 0 - 1
    float2 minXY = float2(clipMinX, clipMinY) * 0.5 + 0.5;
    float2 maxXY = float2(clipMaxX, clipMaxY) * 0.5 + 0.5;
    
    // Calculate hi-Z buffer mip
    int2 size = (maxXY - minXY) * _HiZTextureSize.xx;
    float mip = ceil(log2(max(size.x, size.y)));
    mip = clamp(mip, 0, 10);
    
    // Texel footprint for the lower (finer-grained) level
    float  level_lower = max(mip - 1, 0);
    float2 scale = exp2(-level_lower);
    float2 a = floor(minXY * scale);
    float2 b = ceil(maxXY * scale);
    float2 dims = b - a;
    
    // Use the lower level if we only touch <= 2 texels in both dimensions
    if (dims.x <= 2 && dims.y <= 2)
    {
        mip = level_lower;
    }
    
    // find the max depth
    // Hi-Z approach that allows for more samples.
    // https://www.gamedev.net/blogs/entry/2249535-hierarchical-z-buffer-occlusion-culling-updated-07152011/
    //const   int numSamples = 24;
    const   int   xSamples = 8; // numSamples + 1;
    const   int   ySamples = 25; // numSamples + 1;
    float    widthSS = (maxXY.x - minXY.x);
    float   heightSS = (maxXY.y - minXY.y);    
    float  maxSizeSS = max(widthSS * _HiZTextureSize, heightSS * _HiZTextureSize);    
    float      stepX = widthSS / xSamples;    
    float      stepY = heightSS / ySamples;    
    
    float HIZdepth = 1;    
    float yPos = minXY.y;
    for(int y = 0; y < ySamples; ++y)
    {
        float xPos = minXY.x;
        for(int x = 0; x < xSamples; ++x)
        {
            const float2 nCoords0 = float2(xPos, yPos);
            HIZdepth = min(HIZdepth, _HiZTextureTex.SampleLevel(sampler_HiZTextureTex, nCoords0, mip).r);
            xPos += stepX;
        }
        yPos += stepY;    
    }
    
    return (1.0 - clipMinZ) >= HIZdepth - 0.000015; // last is an epsilon
}

#endif

#include "ParticleSystem.hlsli"

#include "../../ConstantBufferData/ViewProjection.hlsli"

struct Particle {
    float3 position;
    float3 velocity;
    float4 color;
    float startLifetime;
    float remainingLifetime;
    float size;
    float rotation;
    float4 startColor;
    float startSize;
    float3 baseVelocity;
    float randomValue;
    uint simulationSpace;
};

struct InstanceOffset {
    uint offset;
};

ConstantBuffer<ViewProjection> viewProjection : register(b0);
StructuredBuffer<Particle> particles : register(t0);

// Billboarding matrices
cbuffer CameraData : register(b1) {
    float4x4 billboardMatrix;
    float4x4 emitterWorldMatrix; // New: To support local space
}

ConstantBuffer<InstanceOffset> instanceOffset : register(b2);

VSOutput main(VSInput input, uint instanceId : SV_InstanceID) {
    VSOutput output;

    uint instanceIndex = instanceId + instanceOffset.offset;
    Particle p = particles[instanceIndex];

    // Get particle center in world space
    float3 worldCenter;
    if (p.simulationSpace == 1) { // Local
        worldCenter = mul(float4(p.position, 1.0f), emitterWorldMatrix).xyz;
    } else { // World
        worldCenter = p.position;
    }

    // Billboarding: rotate the quad to face the camera
    float4 localPos = input.position;
    
    // Apply size and rotation (assuming rotation around Z axis in local quad space)
    float s = sin(p.rotation);
    float c = cos(p.rotation);
    float x = localPos.x * c - localPos.y * s;
    float y = localPos.x * s + localPos.y * c;
    localPos.x = x * p.size;
    localPos.y = y * p.size;

    // Apply billboard rotation
    float4 billboardedOffset = mul(localPos, billboardMatrix);
    
    // Combine center and billboard offset
    float4 worldPos;
    worldPos.xyz = worldCenter + billboardedOffset.xyz;
    worldPos.w = 1.0f;

    output.position = mul(worldPos, viewProjection.matVP);
    output.worldPosition = worldPos;
    output.normal = mul(input.normal, (float3x3)billboardMatrix); // Approximation
    output.uv = input.uv;
    output.instanceId = instanceIndex;
    output.color = p.color;
    
    return output;
}

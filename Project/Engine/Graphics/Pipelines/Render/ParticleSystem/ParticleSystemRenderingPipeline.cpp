#include "ParticleSystemRenderingPipeline.h"

#include "Engine/ECS/EntityComponentSystem/EntityComponentSystem.h"
#include "Engine/Asset/Collection/AssetCollection.h"
#include "Engine/Core/DirectX12/Manager/DxManager.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Camera/CameraComponent.h"
#include "Engine/Core/Utility/Math/Matrix4x4.h"

namespace ONEngine {

ParticleSystemRenderingPipeline::ParticleSystemRenderingPipeline(Asset::AssetCollection* _assetCollection)
    : pAssetCollection_(_assetCollection) {}

ParticleSystemRenderingPipeline::~ParticleSystemRenderingPipeline() {}

void ParticleSystemRenderingPipeline::Initialize(ShaderCompiler* _shaderCompiler, DxManager* _dxm) {
    {
        // shader compile
        Shader shader;
        shader.Initialize(_shaderCompiler);
        shader.CompileShader(L"Packages/Shader/Render/ParticleSystem/ParticleSystem.vs.hlsl", L"vs_6_0", Shader::Type::vs);
        shader.CompileShader(L"Packages/Shader/Render/ParticleSystem/ParticleSystem.ps.hlsl", L"ps_6_0", Shader::Type::ps);

        std::array<std::function<D3D12_BLEND_DESC()>, 5> blendModeFuncs{
            BlendMode::Normal,
            BlendMode::Add,
            BlendMode::Subtract,
            BlendMode::Multiply,
            BlendMode::Screen,
        };

        // Create pipelines for each blend mode
        for (size_t i = 0; i < blendModeFuncs.size(); i++) {
            auto& pipeline = pipelines_[i];

            pipeline = std::make_unique<GraphicsPipeline>();
            pipeline->SetShader(&shader);

            pipeline->AddInputElement("POSITION", 0, DXGI_FORMAT_R32G32B32A32_FLOAT);
            pipeline->AddInputElement("TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT);
            pipeline->AddInputElement("NORMAL", 0, DXGI_FORMAT_R32G32B32_FLOAT);

            pipeline->SetFillMode(D3D12_FILL_MODE_SOLID);
            pipeline->SetCullMode(D3D12_CULL_MODE_NONE);
            pipeline->SetTopologyType(D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE);

            pipeline->AddCBV(D3D12_SHADER_VISIBILITY_VERTEX, 0); // view projection
            pipeline->AddDescriptorRange(0, 1, D3D12_DESCRIPTOR_RANGE_TYPE_SRV); // particles
            pipeline->AddCBV(D3D12_SHADER_VISIBILITY_VERTEX, 1); // camera data (billboard)
            pipeline->AddCBV(D3D12_SHADER_VISIBILITY_VERTEX, 2); // instance offset
            pipeline->AddDescriptorRange(1, 1, D3D12_DESCRIPTOR_RANGE_TYPE_SRV); // materials
            pipeline->AddDescriptorRange(2, 1, D3D12_DESCRIPTOR_RANGE_TYPE_SRV); // textureId
            pipeline->AddDescriptorRange(3, Asset::MAX_TEXTURE_COUNT, D3D12_DESCRIPTOR_RANGE_TYPE_SRV); // textures

            pipeline->AddDescriptorTable(D3D12_SHADER_VISIBILITY_VERTEX, 0); // particles
            pipeline->AddDescriptorTable(D3D12_SHADER_VISIBILITY_PIXEL, 1);  // materials
            pipeline->AddDescriptorTable(D3D12_SHADER_VISIBILITY_PIXEL, 2);  // textureId
            pipeline->AddDescriptorTable(D3D12_SHADER_VISIBILITY_PIXEL, 3);  // textures

            pipeline->AddStaticSampler(D3D12_SHADER_VISIBILITY_PIXEL, 0);

            pipeline->SetBlendDesc(blendModeFuncs[i]());
            pipeline->SetDepthStencilDesc(DefaultDepthStencilDesc());
            pipeline->CreatePipeline(_dxm->GetDxDevice());
        }
    }

    {   // buffer create
        cameraDataBuffer_.Create(_dxm->GetDxDevice());
        instanceOffsetBuffer_.Create(_dxm->GetDxDevice());

        particleBuffer_.Create(static_cast<uint32_t>(kMaxParticlesTotal_), _dxm->GetDxDevice(), _dxm->GetDxSRVHeap());
        materialBuffer_.Create(static_cast<uint32_t>(kMaxParticlesTotal_), _dxm->GetDxDevice(), _dxm->GetDxSRVHeap());
        textureIdBuffer_.Create(static_cast<uint32_t>(kMaxParticlesTotal_), _dxm->GetDxDevice(), _dxm->GetDxSRVHeap());
    }
}

void ParticleSystemRenderingPipeline::Draw(ECSGroup* _ecs, CameraComponent* _camera, DxCommand* _dxCommand) {
    ComponentArray<ParticleSystem>* psArray = _ecs->GetComponentArray<ParticleSystem>();
    if (!psArray || psArray->GetUsedComponents().empty()) {
        return;
    }

    auto cmdList = _dxCommand->GetCommandList();

    // Prepare billboard matrix from camera
    Matrix4x4 matBillboard = _camera->GetOwner()->GetTransform()->matWorld;
    matBillboard.m[3][0] = 0.0f;
    matBillboard.m[3][1] = 0.0f;
    matBillboard.m[3][2] = 0.0f;
    
    CameraData camData;
    camData.billboardMatrix = matBillboard;
    cameraDataBuffer_.SetMappedData(camData);

    size_t globalParticleIndex = 0;

    // We'll group by BlendMode (using Add for now as default) and texture
    size_t defaultBlendMode = 1;

    pipelines_[defaultBlendMode]->SetPipelineStateForCommandList(_dxCommand);

    cmdList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    _camera->GetViewProjectionBuffer().BindForGraphicsCommandList(cmdList, CBV_VIEW_PROJECTION);
    cameraDataBuffer_.BindForGraphicsCommandList(cmdList, CBV_CAMERA_DATA);

    auto& textures = pAssetCollection_->GetTextures();
    cmdList->SetGraphicsRootDescriptorTable(SRV_TEXTURES, (*textures.begin()).GetSRVGPUHandle());

    for (auto& ps : psArray->GetUsedComponents()) {
        if (!ps || !ps->enable || ps->aliveCount == 0) continue;

        std::string texturePath = "./Packages/Textures/Effects/Particle.png"; // Default fallback
        size_t texIndex = pAssetCollection_->GetTextureIndex(texturePath);
        uint32_t texSrvIndex = textures[texIndex].GetSRVDescriptorIndex();

        // Get mesh
        std::string meshPath = "./Packages/Models/primitive/frontToPlane.obj"; // Default billboard quad
        const Asset::Model* model = pAssetCollection_->GetModel(meshPath);
        if (!model) continue;

        // Map data to buffers
        size_t startInstance = globalParticleIndex;
        for (size_t i = 0; i < ps->aliveCount; i++) {
            if (globalParticleIndex >= kMaxParticlesTotal_) break;
            
            particleBuffer_.SetMappedData(static_cast<uint32_t>(globalParticleIndex), ps->particles[i]);
            materialBuffer_.SetMappedData(static_cast<uint32_t>(globalParticleIndex), Vector4::One);
            textureIdBuffer_.SetMappedData(static_cast<uint32_t>(globalParticleIndex), texSrvIndex);

            globalParticleIndex++;
        }

        size_t drawCount = globalParticleIndex - startInstance;
        if (drawCount == 0) continue;

        // Bind buffers
        particleBuffer_.SRVBindForGraphicsCommandList(cmdList, SRV_PARTICLES);
        materialBuffer_.SRVBindForGraphicsCommandList(cmdList, SRV_MATERIALS);
        textureIdBuffer_.SRVBindForGraphicsCommandList(cmdList, SRV_TEXTURE_IDS);

        // Set instance offset
        InstanceOffset offsetData;
        offsetData.offset = static_cast<uint32_t>(startInstance);
        instanceOffsetBuffer_.SetMappedData(offsetData);
        instanceOffsetBuffer_.BindForGraphicsCommandList(cmdList, CBV_INSTANCE_OFFSET);

        // Draw
        for (auto& mesh : model->GetMeshes()) {
            cmdList->IASetVertexBuffers(0, 1, &mesh->GetVBV());
            cmdList->IASetIndexBuffer(&mesh->GetIBV());

            cmdList->DrawIndexedInstanced(
                static_cast<UINT>(mesh->GetIndices().size()),
                static_cast<UINT>(drawCount),
                0, 0, 0
            );
        }
    }
}

}

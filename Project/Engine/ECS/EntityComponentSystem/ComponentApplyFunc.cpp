#include "ComponentApplyFunc.h"

/// std
#include <unordered_map>
#include <memory>

/// engine
#include "Engine/Core/Utility/Utility.h"

/// components
#include "Engine/Asset/Collection/AssetCollection.h"
#include "Engine/ECS/EntityComponentSystem/EntityComponentSystem.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Transform/Transform.h"
#include "Engine/ECS/Component/Components/RendererComponents/Mesh/MeshRenderer.h"
#include "Engine/ECS/Component/Components/RendererComponents/Mesh/DissolveMeshRenderer.h"
#include "Engine/ECS/Component/Components/RendererComponents/Sprite/SpriteRenderer.h"
#include "Engine/Script/MonoScriptEngine.h"


#include "Engine/Graphics/Buffer/Data/UVTransform.h"

using namespace ONEngine;

namespace {

std::unordered_map<MonoClass*, ComponentApplyFunc> gApplyFuncMap;
std::unordered_map<MonoClass*, ComponentFetchFunc> gFetchFuncMap;
std::unordered_map<MonoClass*, size_t> gComponentBatchSize;


struct TransformBatch {
	uint32_t compId;
	Vector3 position;
	Quaternion rotate;
	Vector3 scale;
	Matrix4x4 matWorld;
};

struct MeshRendererBatch {
	uint32_t compId;
	Vector4 color;
	uint32_t postEffectFlags;
	UVTransform uvTransform;
};

struct DissolveBatch {
	uint32_t compId;
	float threshold;
	UVTransform uvTransform;
};

struct SpriteBatch {
	uint32_t compId;
	Vector4 color;
	Vector2 textureSize;
	UVTransform uvTransform;
};

} /// unnamed namespace


void ComponentApplyFuncs::ApplyTransform(void* _element, ECSGroup* _ecsGroup) {
	auto* data = static_cast<TransformBatch*>(_element);
	auto* tArray = _ecsGroup->GetComponentArray<Transform>();
	if(!CheckComponentArrayEnable(tArray)) {
		return;
	}

	if(Transform* t = tArray->GetComponent(data->compId)) {
		t->SetPosition(data->position);
		t->SetRotate(data->rotate);
		t->SetScale(data->scale);
		t->Update();
	}
}

void ComponentApplyFuncs::ApplyMeshRenderer(void* _element, ECSGroup* _ecsGroup) {
	auto* data = static_cast<MeshRendererBatch*>(_element);
	auto* array = _ecsGroup->GetComponentArray<MeshRenderer>();
	if(!CheckComponentArrayEnable(array)) {
		return;
	}

	if(MeshRenderer* mr = array->GetComponent(data->compId)) {
		mr->SetColor(data->color);
		mr->SetPostEffectFlags(data->postEffectFlags);
		mr->SetUVTransform(data->uvTransform);
	}
}

void ONEngine::ComponentApplyFuncs::ApplyDissolve(void* _element, ECSGroup* _ecsGroup) {
	auto* data = static_cast<DissolveBatch*>(_element);
	auto* array = _ecsGroup->GetComponentArray<DissolveMeshRenderer>();
	if(!CheckComponentArrayEnable(array)) {
		return;
	}

	if(DissolveMeshRenderer* mr = array->GetComponent(data->compId)) {
		mr->SetThreshold(data->threshold);
		mr->SetUVTransform(data->uvTransform);
	}
}

void ONEngine::ComponentApplyFuncs::ApplySprite(void* _element, ECSGroup* _ecsGroup) {
	auto* data = static_cast<SpriteBatch*>(_element);
	auto* array = _ecsGroup->GetComponentArray<SpriteRenderer>();
	if(!CheckComponentArrayEnable(array)) {
		return;
	}

	if(SpriteRenderer* sr = array->GetComponent(data->compId)) {
		sr->SetColor(data->color);
		sr->SetUVTransform(data->uvTransform);
	}
}

void ONEngine::ComponentApplyFuncs::FetchTransform(void* _element, ECSGroup* _ecsGroup) {
	auto* data = static_cast<TransformBatch*>(_element);
	auto* array = _ecsGroup->GetComponentArray<Transform>();
	if(!CheckComponentArrayEnable(array)) {
		return;
	}

	if(Transform* t = array->GetComponent(data->compId)) {
		data->position = t->GetPosition();
		data->rotate = t->GetRotate();
		data->scale = t->GetScale();
		data->matWorld = t->GetMatWorld();
	}
}

void ONEngine::ComponentApplyFuncs::FetchMeshRenderer(void* _element, ECSGroup* _ecsGroup) {
	auto* data = static_cast<MeshRendererBatch*>(_element);
	auto* array = _ecsGroup->GetComponentArray<MeshRenderer>();
	if(!CheckComponentArrayEnable(array)) {
		return;
	}

	if(MeshRenderer* mr = array->GetComponent(data->compId)) {
		data->color = mr->GetColor();
		data->postEffectFlags = mr->GetPostEffectFlags();
		data->uvTransform = mr->GetUVTransform();
	}
}

void ONEngine::ComponentApplyFuncs::FetchDissolve(void* _element, ECSGroup* _ecsGroup) {
	auto* data = static_cast<DissolveBatch*>(_element);
	auto* array = _ecsGroup->GetComponentArray<DissolveMeshRenderer>();
	if(!CheckComponentArrayEnable(array)) {
		return;
	}

	if(DissolveMeshRenderer* mr = array->GetComponent(data->compId)) {
		data->threshold = mr->GetDissolveThreshold();
		data->compId = mr->GetDissolveCompare();
		data->uvTransform = mr->GetUVTransform();
	}
}

void ONEngine::ComponentApplyFuncs::FetchSprite(void* _element, ECSGroup* _ecsGroup) {
	auto* data = static_cast<SpriteBatch*>(_element);
	auto* array = _ecsGroup->GetComponentArray<SpriteRenderer>();
	if(!CheckComponentArrayEnable(array)) {
		return;
	}

	if(SpriteRenderer* sr = array->GetComponent(data->compId)) {
		data->color = sr->GetColor();
		data->textureSize = sr->GetTextureSize(Asset::AssetCollection::GetInstance());
		data->uvTransform = sr->GetUVTransform();
	}
}

ComponentApplyFunc ComponentApplyFuncs::GetApplyFunc(MonoClass* _monoClass) {
	auto itr = gApplyFuncMap.find(_monoClass);
	if(itr == gApplyFuncMap.end()) {
		return nullptr;
	}
	return itr->second;
}

ComponentFetchFunc ONEngine::ComponentApplyFuncs::GetFetchFunc(MonoClass* _monoClass) {
	auto itr = gFetchFuncMap.find(_monoClass);
	if(itr == gFetchFuncMap.end()) {
		return nullptr;
	}
	return itr->second;
}

size_t ONEngine::ComponentApplyFuncs::GetBatchElementSize(MonoClass* _monoClass) {
	auto itr = gComponentBatchSize.find(_monoClass);
	if(itr == gComponentBatchSize.end()) {
		return 0;
	}
	return itr->second;
}

void ONEngine::ComponentApplyFuncs::Initialize(MonoImage* _monoImage) {
	{	/// Transform
		MonoClass* monoClass = mono_class_from_name(_monoImage, "", "Transform");
		gApplyFuncMap[monoClass] = ApplyTransform;
		gFetchFuncMap[monoClass] = FetchTransform;
		gComponentBatchSize[monoClass] = sizeof(TransformBatch);
	}

	{	/// MeshRenderer
		MonoClass* monoClass = mono_class_from_name(_monoImage, "", "MeshRenderer");
		gApplyFuncMap[monoClass] = ApplyMeshRenderer;
		gFetchFuncMap[monoClass] = FetchMeshRenderer;
		gComponentBatchSize[monoClass] = sizeof(MeshRendererBatch);
	}

	{	/// DissolveMeshRenderer
		MonoClass* monoClass = mono_class_from_name(_monoImage, "", "DissolveMeshRenderer");
		gApplyFuncMap[monoClass] = ApplyDissolve;
		gFetchFuncMap[monoClass] = FetchDissolve;
		gComponentBatchSize[monoClass] = sizeof(DissolveBatch);
	}

	{	/// SpriteRenderer
		MonoClass* monoClass = mono_class_from_name(_monoImage, "", "SpriteRenderer");
		gApplyFuncMap[monoClass] = ApplySprite;
		gFetchFuncMap[monoClass] = FetchSprite;
		gComponentBatchSize[monoClass] = sizeof(SpriteBatch);
	}
}

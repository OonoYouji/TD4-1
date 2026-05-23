#include "Engine/ECS/Component/Components/ComputeComponents/ParticleSystem/ParticleSystem.h"
#include "ImGuiMath.h"

#define NOMINMAX

/// std
#include <numbers>
#include <format>
#include <variant>
#include <algorithm>
#include <cstdio> 
#include <cmath> 

/// external
#include <imgui_internal.h> // PushMultiItemsWidths に必要
#include <Externals/imgui/dialog/ImGuiFileDialog.h>

/// engine
#include "Engine/Asset/Collection/AssetCollection.h"
#include "Engine/ECS/EntityComponentSystem/EntityComponentSystem.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Light/Light.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Audio/AudioSource.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Effect/Effect.h"
#include "Engine/ECS/Component/Components/RendererComponents/Mesh/MeshRenderer.h"
#include "Engine/ECS/Component/Components/RendererComponents/Mesh/CustomMeshRenderer.h"

/// editor
#include "Engine/Editor/Manager/EditCommand.h"
#include "Engine/Editor/Commands/ImGuiCommand/ImGuiCommand.h" 
#include "Engine/Editor/Math/AssetPayload.h"

using namespace Editor;
using namespace ONEngine;

namespace {

float rotateSpeed = 3.14159f / 100.0f;
std::string variableName = "";

// --- Helpers for ParticleSystem ---

void DrawMinMaxFloat(const char* label, ONEngine::MinMaxFloat& val) {
    ImGui::PushID(label);
    
    float itemWidth = ImGui::GetContentRegionAvail().x * 0.5f;
    ImGui::TextUnformatted(label);
    ImGui::SameLine(ImGui::GetWindowWidth() * 0.4f);
    
    if (val.state == ONEngine::MinMaxState::Constant) {
        ImGui::SetNextItemWidth(itemWidth);
        ImGui::DragFloat("##constant", &val.constant, 0.1f);
    } else {
        ImGui::SetNextItemWidth(itemWidth * 0.45f);
        ImGui::DragFloat("##min", &val.minVal, 0.1f);
        ImGui::SameLine();
        ImGui::Text("-");
        ImGui::SameLine();
        ImGui::SetNextItemWidth(itemWidth * 0.45f);
        ImGui::DragFloat("##max", &val.maxVal, 0.1f);
    }

    ImGui::SameLine();
    if (ImGui::Button("v", ImVec2(20, 0))) {
        ImGui::OpenPopup("MinMaxPopup");
    }

    if (ImGui::BeginPopup("MinMaxPopup")) {
        if (ImGui::MenuItem("Constant", nullptr, val.state == ONEngine::MinMaxState::Constant)) val.state = ONEngine::MinMaxState::Constant;
        if (ImGui::MenuItem("Random Between Two Constants", nullptr, val.state == ONEngine::MinMaxState::RandomBetweenTwoConstants)) val.state = ONEngine::MinMaxState::RandomBetweenTwoConstants;
        ImGui::EndPopup();
    }

    ImGui::PopID();
}

void DrawMinMaxColor(const char* label, ONEngine::MinMaxColor& val) {
    ImGui::PushID(label);
    ImGui::TextUnformatted(label);
    ImGui::SameLine(ImGui::GetWindowWidth() * 0.4f);

    float itemWidth = ImGui::GetContentRegionAvail().x * 0.5f;

    if (val.state == ONEngine::MinMaxState::Constant) {
        ONEngine::Vector4 editColor;
        editColor.x = val.constant.r;
        editColor.y = val.constant.g;
        editColor.z = val.constant.b;
        editColor.w = val.constant.a;

        ImGui::SetNextItemWidth(itemWidth);
        if (Editor::ImGuiColorEdit("##constant", &editColor)) {
            val.constant.r = editColor.x;
            val.constant.g = editColor.y;
            val.constant.b = editColor.z;
            val.constant.a = editColor.w;
        }
    } else {
        ONEngine::Vector4 cmin;
        cmin.x = val.minVal.r; cmin.y = val.minVal.g; cmin.z = val.minVal.b; cmin.w = val.minVal.a;
        ONEngine::Vector4 cmax;
        cmax.x = val.maxVal.r; cmax.y = val.maxVal.g; cmax.z = val.maxVal.b; cmax.w = val.maxVal.a;
        
        ImGui::SetNextItemWidth(itemWidth * 0.45f);
        if (Editor::ImGuiColorEdit("##min", &cmin)) {
            val.minVal.r = cmin.x;
            val.minVal.g = cmin.y;
            val.minVal.b = cmin.z;
            val.minVal.a = cmin.w;
        }
        ImGui::SameLine();
        ImGui::SetNextItemWidth(itemWidth * 0.45f);
        if (Editor::ImGuiColorEdit("##max", &cmax)) {
            val.maxVal.r = cmax.x;
            val.maxVal.g = cmax.y;
            val.maxVal.b = cmax.z;
            val.maxVal.a = cmax.w;
        }
    }

    ImGui::SameLine();
    if (ImGui::Button("v", ImVec2(20, 0))) {
        ImGui::OpenPopup("MinMaxPopup");
    }
    if (ImGui::BeginPopup("MinMaxPopup")) {
        if (ImGui::MenuItem("Constant", nullptr, val.state == ONEngine::MinMaxState::Constant)) val.state = ONEngine::MinMaxState::Constant;
        if (ImGui::MenuItem("Random Between Two Constants", nullptr, val.state == ONEngine::MinMaxState::RandomBetweenTwoConstants)) val.state = ONEngine::MinMaxState::RandomBetweenTwoConstants;
        ImGui::EndPopup();
    }

    ImGui::PopID();
}

}	/// unnamed namespace

ImVec4 ImMathf::ToImVec4(const ONEngine::Vector4& _vec) {
	return ImVec4(_vec.x, _vec.y, _vec.z, _vec.w);
}

ImVec2 ImMathf::ToImVec2(const ONEngine::Vector2& _vec) {
	return ImVec2(_vec.x, _vec.y);
}

bool ImMathf::ColorEdit(const char* _label, ONEngine::Vector4* _color, ImGuiColorEditFlags _flags) {
	if(!_color) return false;
	return ImGui::ColorEdit4(_label, &_color->x, _flags);
}

bool ImMathf::InputText(const char* _label, std::string* _text, ImGuiInputTextFlags _flags) {
	if(!_text) return false;
	return Editor::ImGuiInputText(_label, _text, _flags);
}

bool ImMathf::InputFloat(const char* _label, float* _v, float _step, float _step_fast, const char* _format, ImGuiInputTextFlags _flags) {
	return ImGui::InputFloat(_label, _v, _step, _step_fast, _format, _flags);
}

bool ImMathf::MaterialEdit(const char* _label, ONEngine::GPUMaterial* _material, ONEngine::Asset::AssetCollection* _assetCollection) {
	if(!_material) return false;
	bool isEdit = false;
	if(ImGui::CollapsingHeader(_label)) {
		if(ImGuiColorEdit("BaseColor", &_material->baseColor)) isEdit = true;
		if(UVTransformEdit("UVTransform", &_material->uvTransform)) isEdit = true;
		if(ImGui::CollapsingHeader("PostEffectFlags")) {
			if(ImGui::CheckboxFlags("Lighting", &_material->postEffectFlags, PostEffectFlags_Lighting)) isEdit = true;
			if(ImGui::CheckboxFlags("Grayscale", &_material->postEffectFlags, PostEffectFlags_Grayscale)) isEdit = true;
			if(ImGui::CheckboxFlags("EnvironmentReflection", &_material->postEffectFlags, PostEffectFlags_EnvironmentReflection)) isEdit = true;
			if(ImGui::CheckboxFlags("Shadow", &_material->postEffectFlags, PostEffectFlags_Shadow)) isEdit = true;
		}
		if(ImGui::CollapsingHeader("Texture")) {
			const std::string& texturePath = _assetCollection->GetTexturePath(_material->baseTextureId);
			std::string tempPath = texturePath;
			if(ImMathf::InputText("Base Texture", &tempPath, ImGuiInputTextFlags_ReadOnly)) { /* handle change if needed */ }
            if(ImGui::BeginDragDropTarget()) {
                if(const ImGuiPayload* payload = ImGui::AcceptDragDropPayload("AssetData")) {
                    AssetPayload* assetPayload = *static_cast<AssetPayload**>(payload->Data);
                    if(ONEngine::Asset::GetAssetTypeFromExtension(ONEngine::FileSystem::FileExtension(assetPayload->filePath)) == ONEngine::Asset::AssetType::Texture) {
                        _material->baseTextureId = static_cast<int32_t>(_assetCollection->GetTextureIndex(assetPayload->filePath));
                        isEdit = true;
                    }
                }
                ImGui::EndDragDropTarget();
            }
			if(_material->baseTextureId >= 0) {
				const ONEngine::Asset::Texture* tex = _assetCollection->GetTexture(_assetCollection->GetTexturePath(_material->baseTextureId));
				if(tex) ImGui::Image((ImTextureID)tex->GetSRVGPUHandle().ptr, ImVec2(100, 100));
			}
		}
	}
	return isEdit;
}

bool ImMathf::UVTransformEdit(const char* _label, ONEngine::UVTransform* _uvTransform) {
	if(!_uvTransform) return false;
	bool isEdit = false;
	if(ImGui::CollapsingHeader(_label)) {
		if(ImGui::DragFloat2("offset", &_uvTransform->position.x, 0.01f)) isEdit = true;
		if(ImGui::DragFloat2("scale", &_uvTransform->scale.x, 0.01f, 0.0f, FLT_MAX)) isEdit = true;
		if(ImGui::DragFloat("rotate", &_uvTransform->rotate, 0.01f, -3.14159f, 3.14159f)) isEdit = true;
	}
	return isEdit;
}

ImVec2 ImMathf::CalculateAspectFitSize(const ONEngine::Vector2& _textureSize, float _maxSize) {
	float aspectRatio = _textureSize.x / _textureSize.y;
	return (aspectRatio > 1.0f) ? ImVec2(_maxSize, _maxSize / aspectRatio) : ImVec2(_maxSize * aspectRatio, _maxSize);
}

ImVec2 ImMathf::CalculateAspectFitSize(const ONEngine::Vector2& _textureSize, const ImVec2& _maxSize) {
	float aspectRatio = _textureSize.x / _textureSize.y;
	return (aspectRatio > (_maxSize.x / _maxSize.y)) ? ImVec2(_maxSize.x, _maxSize.x / aspectRatio) : ImVec2(_maxSize.y * aspectRatio, _maxSize.y);
}

bool Editor::ImGuiInputText(const char* _label, std::string* _text, ImGuiInputTextFlags _flags) {
	if(!_text) return false;
	_flags |= ImGuiInputTextFlags_CallbackResize;
	struct CallbackUserData { std::string* text; };
	auto callback = [](ImGuiInputTextCallbackData* data) -> int {
		if(data->EventFlag == ImGuiInputTextFlags_CallbackResize) {
			auto* user = static_cast<CallbackUserData*>(data->UserData);
			user->text->resize(data->BufTextLen);
			data->Buf = user->text->data();
		}
		return 0;
	};
	CallbackUserData userData = { _text };
	return ImGui::InputText(_label, _text->data(), _text->capacity() + 1, _flags, callback, &userData);
}

void Editor::ImGuiInputTextReadOnly(const char* _label, const std::string& _text) {
	std::string temp = _text;
	ImGuiInputText(_label, &temp, ImGuiInputTextFlags_ReadOnly);
}

bool Editor::ImGuiColorEdit(const char* _label, ONEngine::Vector4* _color) {
	return ImMathf::ColorEdit(_label, _color);
}

void ONEngine::DirectionalLightDebug(DirectionalLight* _light) {
	if(!_light) return;
	if(ImGui::CollapsingHeader("DirectionalLight", ImGuiTreeNodeFlags_DefaultOpen)) {
		bool enabled = (_light->enable != 0);
		if (ImGui::Checkbox("enable", &enabled)) {
			_light->enable = enabled ? 1 : 0;
		}

		ONEngine::Vector4 color = _light->GetColor();
		if (Editor::ImGuiColorEdit("color", &color)) {
			_light->SetColor(color);
		}

		float intensity = _light->GetIntensity();
		if (ImGui::DragFloat("intensity", &intensity, 0.1f, 0.0f, 1000.0f)) {
			_light->SetIntensity(intensity);
		}
	}
}

void ONEngine::AudioSourceDebug(AudioSource* _audioSource) {
	if(!_audioSource) return;
	if(ImGui::CollapsingHeader("AudioSource", ImGuiTreeNodeFlags_DefaultOpen)) {
		bool enabled = (_audioSource->enable != 0);
		if (ImGui::Checkbox("enable", &enabled)) {
			_audioSource->enable = enabled ? 1 : 0;
		}
		float volume = _audioSource->GetVolume();
		if (ImGui::DragFloat("volume", &volume, 0.01f, 0.0f, 1.0f)) {
			_audioSource->SetVolume(volume);
		}
        bool loop = _audioSource->GetLoop();
        if (ImGui::Checkbox("loop", &loop)) {
            _audioSource->SetLoop(loop);
        }
	}
}

void ONEngine::CustomMeshRendererDebug(CustomMeshRenderer* _customMeshRenderer) {
	if(!_customMeshRenderer) return;
	if(ImGui::CollapsingHeader("CustomMeshRenderer", ImGuiTreeNodeFlags_DefaultOpen)) {
		bool enabled = (_customMeshRenderer->enable != 0);
		if (ImGui::Checkbox("enable", &enabled)) {
			_customMeshRenderer->enable = enabled ? 1 : 0;
		}
	}
}

void ONEngine::EffectDebug(Effect* _effect) {
	if(!_effect) return;
	if(ImGui::CollapsingHeader("Effect", ImGuiTreeNodeFlags_DefaultOpen)) {
		bool enabled = (_effect->enable != 0);
		if (ImGui::Checkbox("enable", &enabled)) {
			_effect->enable = enabled ? 1 : 0;
		}
        bool isCreate = _effect->IsCreateParticle();
        if (ImGui::Checkbox("isCreateParticle", &isCreate)) {
            _effect->SetIsCreateParticle(isCreate);
        }
	}
}

bool ONEngine::BeginModuleHeader(const char* label, bool* enabled) {
	ImGui::PushID(label);
	if (enabled) {
		ImGui::Checkbox("##enabled", enabled);
		ImGui::SameLine();
	} else {
		ImGui::Dummy(ImVec2(ImGui::GetFrameHeight(), ImGui::GetFrameHeight()));
		ImGui::SameLine();
	}
	bool open = ImGui::CollapsingHeader(label, ImGuiTreeNodeFlags_Framed | ImGuiTreeNodeFlags_AllowItemOverlap);
	if (open && enabled && !(*enabled)) ImGui::BeginDisabled();
	return open;
}

void ONEngine::EndModuleHeader() {
    ImGui::PopID();
}

void ONEngine::ParticleSystemDebug(ParticleSystem* _ps) {
	if (!_ps) return;
	if (ImGui::CollapsingHeader("Particle System", ImGuiTreeNodeFlags_DefaultOpen)) {
		Editor::ImMathf::InputFloat("Duration", &_ps->main.duration);
		ImGui::Checkbox("Looping", &_ps->main.looping);
		ImGui::Checkbox("Prewarm", &_ps->main.prewarm);
		DrawMinMaxFloat("Start Delay", _ps->main.startDelay);
		DrawMinMaxFloat("Start Lifetime", _ps->main.startLifetime);
		DrawMinMaxFloat("Start Speed", _ps->main.startSpeed);
		DrawMinMaxFloat("Start Size", _ps->main.startSize);
		DrawMinMaxFloat("Start Rotation", _ps->main.startRotation);
		DrawMinMaxColor("Start Color", _ps->main.startColor);
		Editor::ImMathf::InputFloat("Gravity Modifier", &_ps->main.gravityModifier);
		Editor::ImMathf::InputEnum<SimulationSpace>("Simulation Space", &_ps->main.simulationSpace);
		ImGui::DragInt("Max Particles", &_ps->main.maxParticles, 1, 1, 1000000);
	}
	if (BeginModuleHeader("Emission", &_ps->emission.enabled)) {
		Editor::ImMathf::InputFloat("Rate over Time", &_ps->emission.rateOverTime);
		if (ImGui::TreeNode("Bursts")) {
			if (ImGui::Button("+")) _ps->emission.bursts.push_back({});
			for (size_t i = 0; i < _ps->emission.bursts.size(); ++i) {
				ImGui::PushID((int)i);
				ImGui::DragFloat("Time", &_ps->emission.bursts[i].time, 0.01f); ImGui::SameLine();
				ImGui::DragInt("Count", &_ps->emission.bursts[i].count); ImGui::SameLine();
				if (ImGui::Button("x")) { _ps->emission.bursts.erase(_ps->emission.bursts.begin() + i); ImGui::PopID(); break; }
				ImGui::PopID();
			}
			ImGui::TreePop();
		}
		if (!_ps->emission.enabled) ImGui::EndDisabled();
	}
	EndModuleHeader();
	if (BeginModuleHeader("Shape", &_ps->shape.enabled)) {
		Editor::ImMathf::InputEnum<ParticleSystemShapeType>("Shape", &_ps->shape.type);
		Editor::ImMathf::InputFloat("Radius", &_ps->shape.radius);
		if (!_ps->shape.enabled) ImGui::EndDisabled();
	}
	EndModuleHeader();
	bool rendererEnabled = true;
	if (BeginModuleHeader("Renderer", &rendererEnabled)) {
		Editor::ImMathf::InputEnum<ParticleSystemRenderer::RenderMode>("Render Mode", &_ps->renderer.renderMode);
		Editor::ImMathf::InputText("Material GUID", &_ps->renderer.materialGuid);
	}
	EndModuleHeader();
}

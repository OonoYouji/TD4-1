#include "AnimationPlayer.h"

/// external
#include <imgui.h>
#include <nlohmann/json.hpp>

/// engine
#include "Engine/ECS/Entity/GameEntity/GameEntity.h"
#include "Engine/Asset/Collection/AssetCollection.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Transform/Transform.h"
#include "Engine/ECS/Component/Components/RendererComponents/Mesh/MeshRenderer.h"
#include "Engine/ECS/Component/Components/RendererComponents/Sprite/SpriteRenderer.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Light/Light.h"

using namespace ONEngine;

AnimationPlayer::AnimationPlayer() {
    Reset();
}

AnimationPlayer::~AnimationPlayer() = default;

void AnimationPlayer::Reset() {
    clipPath = "";
    currentTime = 0.0f;
    speed = 1.0f;
    isPlaying = false;
    isLooping = true;
    autoPlay = true;
    bindings.clear();
    isBound = false;
}

void AnimationPlayer::Play() {
    isPlaying = true;
    if (!isBound) Bind();
}

void AnimationPlayer::Pause() {
    isPlaying = false;
}

void AnimationPlayer::Stop() {
    isPlaying = false;
    currentTime = 0.0f;
}

void AnimationPlayer::SetClip(const std::string& _path) {
    clipPath = _path;
    isBound = false; // クリップが変わったらバインドをやり直す
}

void AnimationPlayer::Bind() {
    bindings.clear();
    isBound = true;

    auto* clip = Asset::AssetCollection::GetInstance()->GetAsset<Asset::AnimationClip>(Asset::AssetCollection::GetInstance()->GetAssetGuidFromPath(clipPath));
    if (!clip) return;

    GameEntity* entity = GetOwner();
    if (!entity) return;

    for (const auto& track : clip->tracks) {
        PropertyBinding binding;
        binding.propertyPath = track.propertyPath;
        binding.targetComponent = entity->GetComponent(track.componentName);

        if (!binding.targetComponent) {
            // "Script:MyScript" の形式をチェック
            if (track.componentName.find("Script:") == 0) {
                binding.targetComponent = entity->GetComponent<Variables>();
                if (binding.targetComponent) {
                    binding.type = PropertyBinding::Type::ScriptVar;
                    binding.scriptGroupName = track.componentName.substr(7);
                    binding.scriptVarName = track.propertyPath;
                    bindings.push_back(binding);
                }
            }
            continue;
        }

        // C++ コンポーネントのプロパティ解決（簡易的な手動バインド）
        std::string compName = track.componentName;
        std::string propPath = track.propertyPath;

        if (compName == "Transform") {
            auto* t = static_cast<Transform*>(binding.targetComponent);
            if (propPath == "position") { binding.dataPtr = &t->position; binding.type = PropertyBinding::Type::Vector3; }
            else if (propPath == "position.x") { binding.dataPtr = &t->position.x; binding.type = PropertyBinding::Type::Float; }
            else if (propPath == "position.y") { binding.dataPtr = &t->position.y; binding.type = PropertyBinding::Type::Float; }
            else if (propPath == "position.z") { binding.dataPtr = &t->position.z; binding.type = PropertyBinding::Type::Float; }
            else if (propPath == "scale") { binding.dataPtr = &t->scale; binding.type = PropertyBinding::Type::Vector3; }
            else if (propPath == "scale.x") { binding.dataPtr = &t->scale.x; binding.type = PropertyBinding::Type::Float; }
            else if (propPath == "scale.y") { binding.dataPtr = &t->scale.y; binding.type = PropertyBinding::Type::Float; }
            else if (propPath == "scale.z") { binding.dataPtr = &t->scale.z; binding.type = PropertyBinding::Type::Float; }
        }
        else if (compName == "MeshRenderer") {
            auto* r = static_cast<MeshRenderer*>(binding.targetComponent);
            if (propPath == "material.uvTransform.position") { binding.dataPtr = &r->material_.uvTransform.position; binding.type = PropertyBinding::Type::Vector2; }
            else if (propPath == "material.uvTransform.position.x") { binding.dataPtr = &r->material_.uvTransform.position.x; binding.type = PropertyBinding::Type::Float; }
            else if (propPath == "material.uvTransform.position.y") { binding.dataPtr = &r->material_.uvTransform.position.y; binding.type = PropertyBinding::Type::Float; }
            else if (propPath == "material.baseColor") { binding.dataPtr = &r->material_.baseColor; binding.type = PropertyBinding::Type::Vector4; }
        }
        else if (compName == "SpriteRenderer") {
            auto* r = static_cast<SpriteRenderer*>(binding.targetComponent);
            if (propPath == "color") { binding.dataPtr = &r->material_.baseColor; binding.type = PropertyBinding::Type::Vector4; }
        }
        else if (compName == "DirectionalLight") {
            auto* l = static_cast<DirectionalLight*>(binding.targetComponent);
            if (propPath == "color") { binding.dataPtr = &l->color_; binding.type = PropertyBinding::Type::Vector4; }
            else if (propPath == "intensity") { binding.dataPtr = &l->intensity_; binding.type = PropertyBinding::Type::Float; }
        }

        if (binding.dataPtr) {
            bindings.push_back(binding);
        }
    }
}

void ONEngine::from_json(const nlohmann::json& _j, AnimationPlayer& _a) {
    _a.clipPath = _j.value("clipPath", "");
    _a.currentTime = _j.value("currentTime", 0.0f);
    _a.speed = _j.value("speed", 1.0f);
    _a.isPlaying = _j.value("isPlaying", false);
    _a.isLooping = _j.value("isLooping", true);
    _a.autoPlay = _j.value("autoPlay", true);
}

void ONEngine::to_json(nlohmann::json& _j, const AnimationPlayer& _a) {
    _j = nlohmann::json{
        { "type", "AnimationPlayer" },
        { "clipPath", _a.clipPath },
        { "currentTime", _a.currentTime },
        { "speed", _a.speed },
        { "isPlaying", _a.isPlaying },
        { "isLooping", _a.isLooping },
        { "autoPlay", _a.autoPlay }
    };
}

void ComponentDebug::AnimationPlayerDebug(AnimationPlayer* _player) {
    if (!_player) return;

    ImGui::Text("Animation Player");
    
    char pathBuf[256];
    strncpy_s(pathBuf, _player->clipPath.c_str(), sizeof(pathBuf));
    if (ImGui::InputText("Clip Path", pathBuf, sizeof(pathBuf))) {
        _player->SetClip(pathBuf);
    }

    ImGui::DragFloat("Current Time", &_player->currentTime, 0.01f);
    ImGui::DragFloat("Speed", &_player->speed, 0.1f);
    ImGui::Checkbox("Is Playing", &_player->isPlaying);
    ImGui::Checkbox("Is Looping", &_player->isLooping);
    ImGui::Checkbox("Auto Play", &_player->autoPlay);

    if (ImGui::Button("Play")) _player->Play();
    ImGui::SameLine();
    if (ImGui::Button("Pause")) _player->Pause();
    ImGui::SameLine();
    if (ImGui::Button("Stop")) _player->Stop();

    if (ImGui::Button("Force Bind")) _player->Bind();
}

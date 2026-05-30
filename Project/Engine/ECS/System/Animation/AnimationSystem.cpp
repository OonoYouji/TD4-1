#include "AnimationSystem.h"

/// engine
#include "Engine/ECS/EntityComponentSystem/ECSGroup.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Animation/AnimationPlayer.h"
#include "Engine/Core/Utility/Time/Time.h"
#include "Engine/Asset/Collection/AssetCollection.h"
#include "Engine/Core/Utility/Math/Interpolation.h"

using namespace ONEngine;

void AnimationSystem::OutsideOfRuntimeUpdate(ECSGroup* _ecs) {
    Update(_ecs, Time::UnscaledDeltaTime());
}

void AnimationSystem::RuntimeUpdate(ECSGroup* _ecs) {
    Update(_ecs, Time::DeltaTime());
}

namespace {
    // 補間用ヘルパー
    template<typename T>
    T EvaluateTrack(const std::vector<Asset::AnimationKeyframe>& _keyframes, float _time) {
        if (_keyframes.empty()) return T{};
        if (_time <= _keyframes.front().time) return std::get<T>(_keyframes.front().value);
        if (_time >= _keyframes.back().time) return std::get<T>(_keyframes.back().value);

        for (size_t i = 0; i < (int)_keyframes.size() - 1; ++i) {
            if (_time >= _keyframes[i].time && _time < _keyframes[i + 1].time) {
                float t = (_time - _keyframes[i].time) / (_keyframes[i + 1].time - _keyframes[i].time);
                const T& v0 = std::get<T>(_keyframes[i].value);
                const T& v1 = std::get<T>(_keyframes[i + 1].value);
                
                if (_keyframes[i].interpolation == "Step") return Math::Step(v0, v1, t);
                return Math::Lerp(v0, v1, t);
            }
        }
        return std::get<T>(_keyframes.back().value);
    }
}

void AnimationSystem::Update(ECSGroup* _ecs, float _deltaTime) {
    ComponentArray<AnimationPlayer>* playerArray = _ecs->GetComponentArray<AnimationPlayer>();
    if (!playerArray) return;

    auto* ac = Asset::AssetCollection::GetInstance();

    for (auto& player : playerArray->GetUsedComponents()) {
        if (!player || !player->enable || !player->isPlaying) continue;

        // 時間の進行
        player->currentTime += _deltaTime * player->speed;

        // クリップの取得 (パス正規化を適用しつつ取得)
        std::string path = player->clipPath;
        // AnimationPlayer::SetClipでも行っているが、ここでも安全のために確認
        if (!path.empty() && !path.starts_with("./") && !path.starts_with("/") && (path.starts_with("Assets") || path.starts_with("Packages"))) {
            path = "./" + path;
        }

        auto guid = ac->GetAssetGuidFromPath(path);
        auto* clip = ac->GetAsset<Asset::AnimationClip>(guid);
        
        if (!clip) {
            static float logTimer = 0;
            logTimer += _deltaTime;
            if (logTimer > 2.0f) {
                ONEngine::Console::LogWarning(std::format("[Anim] Missing clip: '{}' (GUID valid:{}) for '{}'", 
                    path, guid.CheckValid(), player->GetOwner()->GetName()));
                logTimer = 0;
            }
            continue;
        }

        // バインドの確認
        if (!player->isBound || player->bindings.size() != clip->tracks.size()) {
            ONEngine::Console::Log(std::format("[Anim] Binding tracks for '{}' using clip '{}'", player->GetOwner()->GetName(), clip->name));
            player->Bind();
        }

        // ループ/終了処理
        if (player->currentTime > clip->duration) {
            if (player->isLooping) {
                player->currentTime = std::fmod(player->currentTime, clip->duration);
            } else {
                player->currentTime = clip->duration;
                player->isPlaying = false;
            }
        }

        // 各トラックの値を適用
        for (size_t i = 0; i < (int)clip->tracks.size() && i < (int)player->bindings.size(); ++i) {
            const auto& track = clip->tracks[i];
            auto& binding = player->bindings[i];

            if (binding.type == AnimationPlayer::PropertyBinding::Type::ScriptVar) {
                auto* vars = static_cast<Variables*>(binding.targetComponent);
                if (track.keyframes.empty()) continue;
                
                if (std::holds_alternative<float>(track.keyframes[0].value)) {
                    vars->SetVariable(binding.scriptGroupName, binding.scriptVarName, EvaluateTrack<float>(track.keyframes, player->currentTime));
                } else if (std::holds_alternative<Vector2>(track.keyframes[0].value)) {
                    vars->SetVariable(binding.scriptGroupName, binding.scriptVarName, EvaluateTrack<Vector2>(track.keyframes, player->currentTime));
                } else if (std::holds_alternative<Vector3>(track.keyframes[0].value)) {
                    vars->SetVariable(binding.scriptGroupName, binding.scriptVarName, EvaluateTrack<Vector3>(track.keyframes, player->currentTime));
                } else if (std::holds_alternative<Vector4>(track.keyframes[0].value)) {
                    vars->SetVariable(binding.scriptGroupName, binding.scriptVarName, EvaluateTrack<Vector4>(track.keyframes, player->currentTime));
                }
            } else if (binding.dataPtr) {
                switch (binding.type) {
                case AnimationPlayer::PropertyBinding::Type::Float:
                    *static_cast<float*>(binding.dataPtr) = EvaluateTrack<float>(track.keyframes, player->currentTime);
                    break;
                case AnimationPlayer::PropertyBinding::Type::Vector2:
                    *static_cast<Vector2*>(binding.dataPtr) = EvaluateTrack<Vector2>(track.keyframes, player->currentTime);
                    break;
                case AnimationPlayer::PropertyBinding::Type::Vector3:
                    *static_cast<Vector3*>(binding.dataPtr) = EvaluateTrack<Vector3>(track.keyframes, player->currentTime);
                    break;
                case AnimationPlayer::PropertyBinding::Type::Vector4:
                    *static_cast<Vector4*>(binding.dataPtr) = EvaluateTrack<Vector4>(track.keyframes, player->currentTime);
                    break;
                case AnimationPlayer::PropertyBinding::Type::TransformRotationEuler:
                {
                    Vector3 euler = EvaluateTrack<Vector3>(track.keyframes, player->currentTime);
                    
                    // 度数法(Degree)から弧度法(Radian)へ変換
                    Vector3 radians = {
                        euler.x * (std::numbers::pi_v<float> / 180.0f),
                        euler.y * (std::numbers::pi_v<float> / 180.0f),
                        euler.z * (std::numbers::pi_v<float> / 180.0f)
                    };
                    *static_cast<Quaternion*>(binding.dataPtr) = Quaternion::FromEuler(radians);
                    break;
                }
                }
            }
        }
    }
}

#include "AnimationSystem.h"

/// engine
#include "Engine/ECS/EntityComponentSystem/ECSGroup.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Animation/AnimationPlayer.h"
#include "Engine/Core/Utility/Time/Time.h"
#include "Engine/Asset/Collection/AssetCollection.h"
#include "Engine/Core/Utility/Math/Interpolation.h"

using namespace ONEngine;

void AnimationSystem::OutsideOfRuntimeUpdate(ECSGroup* _ecs) {
    Update(_ecs, Time::DeltaTime());
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

        for (size_t i = 0; i < _keyframes.size() - 1; ++i) {
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

        // クリップの取得
        auto* clip = ac->GetAsset<Asset::AnimationClip>(ac->GetAssetGuidFromPath(player->clipPath));
        if (!clip) continue;

        // バインドの確認
        if (!player->isBound) player->Bind();

        // 時間の進行
        player->currentTime += _deltaTime * player->speed;

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
        for (size_t i = 0; i < clip->tracks.size() && i < player->bindings.size(); ++i) {
            const auto& track = clip->tracks[i];
            auto& binding = player->bindings[i];

            if (binding.type == AnimationPlayer::PropertyBinding::Type::ScriptVar) {
                auto* vars = static_cast<Variables*>(binding.targetComponent);
                // ScriptVarは常にfloat、Vector2/3/4のどれかとして扱う
                // キーフレームの最初の型を見て判断
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
                // C++プロパティへの直接適用
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
                }
            }
        }
    }
}

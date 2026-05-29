#include "AnimationSystem.h"

/// engine
#include "Engine/ECS/EntityComponentSystem/ECSGroup.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Animation/AnimationPlayer.h"
#include "Engine/Core/Utility/Time/Time.h"

using namespace ONEngine;

void AnimationSystem::OutsideOfRuntimeUpdate(ECSGroup* _ecs) {
    // エディタ上でもアニメーションを確認したい場合はここに記述
    Update(_ecs, Time::DeltaTime());
}

void AnimationSystem::RuntimeUpdate(ECSGroup* _ecs) {
    Update(_ecs, Time::DeltaTime());
}

void AnimationSystem::Update(ECSGroup* _ecs, float _deltaTime) {
    ComponentArray<AnimationPlayer>* playerArray = _ecs->GetComponentArray<AnimationPlayer>();
    if (!playerArray) return;

    for (auto& player : playerArray->GetUsedComponents()) {
        if (!player || !player->enable || !player->isPlaying) continue;

        // 時間の進行
        player->currentTime += _deltaTime * player->speed;

        // ループ処理などはAnimationClipのdurationが判明したPhase 2/3で詳細実装
    }
}

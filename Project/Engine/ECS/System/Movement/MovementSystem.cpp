#include "MovementSystem.h"
#include "Engine/ECS/EntityComponentSystem/ECSGroup.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Agent/AgentIntentComponent.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Transform/Transform.h"
#include "Engine/Core/Utility/Time/Time.h"
#include "Engine/ECS/Component/Array/ComponentArray.h"
#include "Engine/ECS/Entity/GameEntity/GameEntity.h"

namespace ONEngine {

MovementSystem::MovementSystem() {}

void MovementSystem::RuntimeUpdate(ECSGroup* _ecs) {
    if (!_ecs) {
        return;
    }

    // AIの意図に基づいて移動を処理する
    auto* intentArray = _ecs->GetComponentArray<AgentIntentComponent>();
    if (!intentArray) {
        return;
    }

    for (auto& intent : intentArray->GetUsedComponents()) {
        if (!intent || !intent->enable) {
            continue;
        }

        // 移動方向がゼロベクトルに近い場合は何もしない
        if (intent->desiredMoveDirection.LengthSquared() < 0.001f) {
            continue;
        }

        auto* owner = intent->GetOwner();
        if (!owner) {
            continue;
        }
        
        // ここでは単純な移動を実装する
        // 将来的にはキャラクターコントローラーや物理演算を介すべき
        float speed = 5.0f; // 仮のスピード
        Vector3 velocity = intent->desiredMoveDirection.Normalize() * speed * Time::DeltaTime();
        Vector3 oldPos = owner->GetLocalPosition();
        Vector3 newPos = oldPos + velocity;
        owner->SetPosition(newPos);

        // ログを出力して動作を確認 (Bossエンティティを想定)
        if (owner->GetName().find("Boss") != std::string::npos || owner->GetId() == 1) {
            Console::Log("C++ MovementSystem: Entity " + owner->GetName() + " moving. Dir: (" + 
                std::to_string(intent->desiredMoveDirection.x) + ", " + 
                std::to_string(intent->desiredMoveDirection.y) + ", " + 
                std::to_string(intent->desiredMoveDirection.z) + "), Pos: (" + 
                std::to_string(newPos.x) + ", " + std::to_string(newPos.y) + ", " + std::to_string(newPos.z) + ")", LogCategory::Engine);
        }
    }
}

}

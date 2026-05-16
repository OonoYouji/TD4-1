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
        Vector3 newPos = owner->GetLocalPosition() + velocity;
        owner->SetPosition(newPos);

        // ログを出力して動作を確認
        if (owner->GetId() == 1) {
            Console::Log("C++ MovementSystem: Entity 1 moved to (" + std::to_string(newPos.x) + ", " + std::to_string(newPos.y) + ", " + std::to_string(newPos.z) + ")", LogCategory::Engine);
        }
    }
}

}

#pragma once

/// engine
#include "Engine/ECS/Component/Components/ComputeComponents/Transform/Transform.h"
#include "../IEditCommand.h"

namespace Editor {

/// ///////////////////////////////////////////////
/// Transformの値を変更するコマンド
/// ///////////////////////////////////////////////
class ModifyTransformCommand : public IEditCommand {
public:
    enum class Target { Position, Rotation, Scale };

    ModifyTransformCommand(ONEngine::Transform* _transform, Target _target, const ONEngine::Vector3& _oldVal, const ONEngine::Vector3& _newVal)
        : pTransform_(_transform), target_(_target), oldVal_(_oldVal), newVal_(_newVal) {}

    EDITOR_STATE Execute() override {
        if (!pTransform_) return EDITOR_STATE_FAILED;
        ApplyValue(newVal_);
        return EDITOR_STATE_FINISH;
    }

    EDITOR_STATE Undo() override {
        if (!pTransform_) return EDITOR_STATE_FAILED;
        ApplyValue(oldVal_);
        return EDITOR_STATE_FINISH;
    }

private:
    void ApplyValue(const ONEngine::Vector3& _val) {
        switch (target_) {
        case Target::Position: 
            pTransform_->position = _val; 
            break;
        case Target::Rotation: 
            // 度数法(Degrees) -> 弧度法(Euler Radian) -> Quaternion の順で変換
            pTransform_->euler = _val; 
            pTransform_->SyncQuaternionFromEuler();
            break;
        case Target::Scale:    
            pTransform_->scale = _val;    
            break;
        }
        pTransform_->Update();
    }

    ONEngine::Transform* pTransform_;
    Target target_;
    ONEngine::Vector3 oldVal_; // Degrees if Rotation
    ONEngine::Vector3 newVal_; // Degrees if Rotation
};

} /// namespace Editor

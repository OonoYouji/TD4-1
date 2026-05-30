#pragma once

/// std
#include <string>
#include <vector>

/// engine
#include "../../Interface/IComponent.h"
#include "Engine/Asset/Guid/Guid.h"

namespace ONEngine {

class AnimationPlayer : public IComponent {
public:
    AnimationPlayer();
    ~AnimationPlayer() override;

    void Reset() override;

    /// @brief アニメーションを再生する
    void Play();
    /// @brief アニメーションを一時停止する
    void Pause();
    /// @brief アニメーションを停止する
    void Stop();

    /// @brief 使用するアニメーションクリップを設定する
    void SetClip(const std::string& _path);

    /// ===============================================
    /// public : objects
    /// ===============================================

    std::string clipPath;
    float currentTime = 0.0f;
    float speed = 1.0f;
    bool isPlaying = false;
    bool isLooping = true;
    bool autoPlay = true;
    bool shouldApplyOnce = false; // Stop時などに一度だけ値を適用するためのフラグ

    /// アニメーション対象のプロパティを解決したキャッシュ
    struct PropertyBinding {
        IComponent* targetComponent;
        std::string propertyPath;
        void* dataPtr = nullptr; // 直接値を書き換えるためのポインタ
        enum class Type {
            Float, Vector2, Vector3, Vector4,
            TransformRotationEuler, // 特殊対応：Vector3 Euler -> Quaternion
            ScriptVar // Variablesコンポーネント経由
        } type;
        std::string scriptGroupName; // ScriptVarの場合のみ使用
        std::string scriptVarName;   // ScriptVarの場合のみ使用
    };
    std::vector<PropertyBinding> bindings;
    bool isBound = false;

    /// @brief エンティティのコンポーネントに対してプロパティをバインドする
    void Bind();
};

void from_json(const nlohmann::json& _j, AnimationPlayer& _a);
void to_json(nlohmann::json& _j, const AnimationPlayer& _a);

namespace ComponentDebug {
    void AnimationPlayerDebug(AnimationPlayer* _player);
}

} /// namespace ONEngine

#pragma once

/// std
#include <nlohmann/json.hpp>

/// engine
#include "../../Interface/IComponent.h"
#include <Engine/Core/Utility/Math/Vector3.h>


/// ----- 前方宣言 ----- ///
namespace ONEngine {

class AgentIntentComponent;

/// Json変換
void from_json(const nlohmann::json& _j, AgentIntentComponent& _c);
void to_json(nlohmann::json& _j, const AgentIntentComponent& _c);

namespace ComponentDebug {
void AgentIntentComponentDebug(AgentIntentComponent* comp);
}


/// ///////////////////////////////////////////////////
/// AIの「意図」を格納するコンポーネント
/// C#側で計算され、C++側で行動に変換される
/// ///////////////////////////////////////////////////
class AgentIntentComponent : public IComponent {
public:
	/// <summary>
	/// C++とC#でデータを一括同期するための構造体
	/// </summary>
	struct BatchData {
	    uint32_t compId;
	    Vector3 desiredMoveDirection;
	    uint8_t isAttacking; // boolの代わりにuint8_tを使用して互換性を確保
	    int32_t targetEntityId;
	};
	/// ----- friend class ----- ///
	friend class MovementSystem;

public:
	/// ===================================================
	/// public : methods
	/// ===================================================

	AgentIntentComponent() {
		Reset();
	}

	void Reset() override {
		desiredMoveDirection = Vector3::Zero;
		isAttacking = false;
		targetEntityId = 0; // 0: invalid id
	}

public:
	/// ===================================================
	/// public : objects
	/// ===================================================

	/// @brief C#側が設定する「移動したい方向」
	Vector3 desiredMoveDirection;

	/// @brief C#側が設定する「攻撃中か」のフラグ
	bool isAttacking;

	/// @brief C#側が設定する「追従対象のEntityID」
	int32_t targetEntityId;
};

inline void from_json(const nlohmann::json& _j, AgentIntentComponent& _c) {
	_c.enable = _j.at("enable").get<int>();
	if(_j.contains("desiredMoveDirection")) {
		_c.desiredMoveDirection = _j.at("desiredMoveDirection").get<Vector3>();
	}
	if(_j.contains("isAttacking")) {
		_c.isAttacking = _j.at("isAttacking").get<bool>();
	}
	if(_j.contains("targetEntityId")) {
		_c.targetEntityId = _j.at("targetEntityId").get<int32_t>();
	}
}

inline void to_json(nlohmann::json& _j, const AgentIntentComponent& _c) {
	_j = nlohmann::json{
		{ "type", "AgentIntentComponent" },
		{ "enable", _c.enable },
		{ "desiredMoveDirection", _c.desiredMoveDirection },
		{ "isAttacking", _c.isAttacking },
		{ "targetEntityId", _c.targetEntityId }
	};
}

} /// ONEngine


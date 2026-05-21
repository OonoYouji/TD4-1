#include "AgentIntentComponent.h"
#include <nlohmann/json.hpp>
#include "Engine/Editor/Commands/ComponentEditCommands/ComponentJsonConverter.h"

namespace ONEngine {


void ComponentDebug::AgentIntentComponentDebug(AgentIntentComponent* comp) {
	if(!comp) {
		return;
	}



}

void from_json(const nlohmann::json& _j, AgentIntentComponent& _c) {
	_c.enable = _j.at("enable").get<int>();
	if (_j.contains("desiredMoveDirection")) {
		_c.desiredMoveDirection = _j.at("desiredMoveDirection").get<Vector3>();
	}
	if (_j.contains("desiredRotation")) {
		_c.desiredRotation = _j.at("desiredRotation").get<Quaternion>();
	}
	if (_j.contains("rotationSpeed")) {
		_c.rotationSpeed = _j.at("rotationSpeed").get<float>();
	}
	if (_j.contains("useDesiredRotation")) {
		_c.useDesiredRotation = _j.at("useDesiredRotation").get<bool>();
	}
	if (_j.contains("isAttacking")) {
		_c.isAttacking = _j.at("isAttacking").get<bool>();
	}
	if (_j.contains("targetEntityId")) {
		_c.targetEntityId = _j.at("targetEntityId").get<int32_t>();
	}
}

void to_json(nlohmann::json& _j, const AgentIntentComponent& _c) {
	_j = nlohmann::json{
		{ "type", "AgentIntentComponent" },
		{ "enable", _c.enable },
		{ "desiredMoveDirection", _c.desiredMoveDirection },
		{ "desiredRotation", _c.desiredRotation },
		{ "rotationSpeed", _c.rotationSpeed },
		{ "useDesiredRotation", _c.useDesiredRotation },
		{ "isAttacking", _c.isAttacking },
		{ "targetEntityId", _c.targetEntityId }
	};
}


}
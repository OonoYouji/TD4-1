#include "SphereCollider.h"

/// std
#include <bit>

/// external
#include <magic_enum/magic_enum.hpp>
#include <imgui.h>

/// engine
#include "Engine/Core/Utility/Utility.h"
#include "Engine/Editor/Commands/ImGuiCommand/ImGuiCommand.h"

using namespace ONEngine;

void ComponentDebug::SphereColliderDebug(SphereCollider* _c) {
	if(!_c) {
		return;
	}

	/// 共通パラメータ
	ImGui::SeparatorText("common parameter");
	{	/// CollisionState
		int currentIndex = static_cast<int>(_c->collisionState_);

		auto names = magic_enum::enum_names<CollisionState>();
		std::vector<const char*> items;
		for(auto& n : names) items.push_back(n.data());

		if(ImGui::Combo("CollisionState", &currentIndex, items.data(), (int)items.size())) {
			_c->collisionState_ = static_cast<CollisionState>(currentIndex);
		}
	}

	{	/// CollisionFilter (ImGui側の処理)

		constexpr auto entries = magic_enum::enum_entries<CollisionFilter>();
		std::vector<const char*> items;
		for(const auto& entry : entries) {
			items.push_back(entry.second.data());
		}

		ImGui::Text("Collision Settings");
		ImGui::Separator();

		// ---------------------------------------------------------
		// 1. Category の設定 (Combo)
		// ---------------------------------------------------------
		uint32_t currentCategoryBit = _c->GetCategoryBits();
		int categoryIdx = 0;

		for(size_t i = 0; i < entries.size(); ++i) {
			if(currentCategoryBit == static_cast<uint32_t>(entries[i].first)) {
				categoryIdx = static_cast<int>(i);
				break;
			}
		}

		if(ImGui::Combo("Category", &categoryIdx, items.data(), static_cast<int>(items.size()))) {
			_c->SetCategoryBits(static_cast<uint32_t>(entries[categoryIdx].first));
		}

		// ---------------------------------------------------------
		// 2. Mask の設定 (CheckboxFlags)
		// ---------------------------------------------------------
		ImGui::Spacing();
		ImGui::Text("Collides With (Mask):");

		uint32_t currentMask = _c->GetMaskBits();

		if(ImGui::Button("Select All")) {
			currentMask = 0xFFFFFFFF; // ALL定数
			_c->SetMaskBits(currentMask);
		}
		ImGui::SameLine();
		if(ImGui::Button("Clear All")) {
			currentMask = 0;
			_c->SetMaskBits(currentMask);
		}

		// 各レイヤーをチェックボックスとして表示 (複数選択可能)
		for(const auto& entry : entries) {
			uint32_t bitValue = static_cast<uint32_t>(entry.first);
			if(ImGui::CheckboxFlags(entry.second.data(), &currentMask, bitValue)) {
				_c->SetMaskBits(currentMask);
			}
		}
	}



	/// sphere parameter
	ImGui::SeparatorText("sphere parameter");
	Editor::ImMathf::DragFloat("radius", &_c->radius_, 0.1f, 0.0f, 100.0f);

}


void ONEngine::from_json(const nlohmann::json& _j, SphereCollider& _s) {
	_s.enable = _j.value("enable", 1);
	_s.radius_ = _j.value("radius", 1.0f);
	_s.collisionState_ = magic_enum::enum_cast<CollisionState>(_j.value("state", "Dynamic")).value_or(CollisionState::Dynamic);
	_s.categoryBits_ = _j.value("categoryBits", static_cast<uint32_t>(CollisionFilter::Default));
	_s.maskBits_ = _j.value("maskBits", static_cast<uint32_t>(CollisionFilter::ALL));
}

void ONEngine::to_json(nlohmann::json& _j, const SphereCollider& _s) {
	_j = nlohmann::json{
		{ "type", "SphereCollider" },
		{ "enable", _s.enable },
		{ "radius", _s.GetRadius() },
		{ "state", magic_enum::enum_name(_s.collisionState_) },
		{ "categoryBits", _s.categoryBits_ },
		{ "maskBits", _s.maskBits_ }
	};
}


SphereCollider::SphereCollider() {
	// デフォルトの値をセット
	radius_ = 1.0f;
}

void SphereCollider::SetRadius(float _radius) {
	radius_ = _radius;
}

float SphereCollider::GetRadius() const {
	return radius_;
}


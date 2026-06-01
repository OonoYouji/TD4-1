#include "HierarchyWindow.h"

/// std
#include <filesystem>
#include <algorithm>
#include <fstream>

/// external
#include <imgui.h>
#include <dialog/ImGuiFileDialog.h>
#include <nlohmann/json.hpp>

/// engine
#include "Engine/Core/Config/EngineConfig.h"
#include "Engine/Core/Utility/Math/Math.h"
#include "Engine/ECS/EntityComponentSystem/EntityComponentSystem.h"
#include "Engine/ECS/Entity/GameEntity/GameEntity.h"
#include "Engine/ECS/Entity/EntityJsonConverter.h"
#include "Engine/Scene/SceneManager.h"

/// editor
#include "Engine/Editor/Commands/WorldEditorCommands/WorldEditorCommands.h"
#include "Engine/Editor/Manager/EditCommand.h"
#include "Engine/Editor/Manager/EditorManager.h"
#include "Engine/Editor/Math/ImGuiMath.h"
#include "Engine/Editor/Math/ImGuiSelection.h"
#include "Engine/Editor/Commands/ImGuiCommand/FocusEntityCommand.h"
#include "Engine/Editor/Math/AssetPayload.h"

namespace Editor {

HierarchyWindow::HierarchyWindow(
	const std::string& windowName,
	ONEngine::EntityComponentSystem* ecs,
	ONEngine::ECSGroup* ecsGroup,
	EditorManager* editorManager,
	ONEngine::SceneManager* sceneManager)
	: windowName_(windowName), pEcs_(ecs), pEcsGroup_(ecsGroup), pEditorManager_(editorManager),
	pSceneManager_(sceneManager) {

	newName_.reserve(1024);
	isNodeOpen_ = false;

	// 無効なGuidで初期化しておく
	renameEntityGuid_ = ONEngine::Guid::kInvalid;
}

void HierarchyWindow::ShowImGui() {
	if(!ImGui::Begin(windowName_.c_str(), nullptr)) {
		ImGui::End();
		return;
	}

	/// ヒエラルキーの描画
	DrawHierarchy();

	/// ドラッグ＆ドロップの受け入れ（ルートへの移動用余白を確保）
	HandleRootDragDrop();

	ImGui::End();

	/// 各種ポップアップの表示
	ShowInvalidParentPopup();
	DrawDialog();
	DrawSceneSaveDialog();
}

void HierarchyWindow::DrawHierarchy() {
	flatHierarchyGuids_.clear();
	const auto& entities = pEcsGroup_->GetEntities();

	// ---------------------------------------------------
	// 1. 各エンティティの描画
	// ---------------------------------------------------
	for(const auto& entity : entities) {
		// ルートエンティティのみ開始
		if(!entity->GetParent()) {
			DrawEntity(entity.get());
		}
	}

	// ---------------------------------------------------
	// 2. 背景クリックで選択解除
	// ---------------------------------------------------
	if(ImGui::IsWindowHovered() && ImGui::IsMouseClicked(ImGuiMouseButton_Left) && !ImGui::IsAnyItemActive() && !ImGui::GetIO().KeyCtrl) {
		ImGuiSelection::SetSelectedObject(ONEngine::Guid::kInvalid, SelectionType::None);
	}

	// ---------------------------------------------------
	// 3. ボックス選択 (Marquee Selection)
	// ---------------------------------------------------
	if (ImGui::IsWindowHovered() && ImGui::IsMouseClicked(ImGuiMouseButton_Left) && !ImGui::IsAnyItemHovered()) {
		isMarqueeSelecting_ = true;
		marqueeStartPos_ = ImGui::GetMousePos();
		if (!ImGui::GetIO().KeyCtrl) {
			ImGuiSelection::ClearSelection();
		}
	}

	if (isMarqueeSelecting_) {
		if (ImGui::IsMouseReleased(ImGuiMouseButton_Left)) {
			isMarqueeSelecting_ = false;
		} else {
			ImVec2 mousePos = ImGui::GetMousePos();
			marqueeMin_ = ImVec2((std::min)(marqueeStartPos_.x, mousePos.x), (std::min)(marqueeStartPos_.y, mousePos.y));
			marqueeMax_ = ImVec2((std::max)(marqueeStartPos_.x, mousePos.x), (std::max)(marqueeStartPos_.y, mousePos.y));

			// 選択範囲の可視化
			ImGui::GetForegroundDrawList()->AddRectFilled(marqueeMin_, marqueeMax_, ImColor(100, 150, 255, 50));
			ImGui::GetForegroundDrawList()->AddRect(marqueeMin_, marqueeMax_, ImColor(100, 150, 255, 200));
		}
	}

	/// 遅延削除の実行
	if(!deleteQueue_.empty()) {
		for(const auto& guid : deleteQueue_) {
			ONEngine::GameEntity* entity = pEcsGroup_->GetEntityFromGuid(guid);
			if(entity) {
				pEditorManager_->ExecuteCommand<DeleteEntityCommand>(pEcsGroup_, entity);
			}
		}
		deleteQueue_.clear();
	}
}

void HierarchyWindow::EntityRename(ONEngine::GameEntity* entity) {
	if(ImGuiInputText("##rename", &newName_, ImGuiInputTextFlags_CallbackAlways | ImGuiInputTextFlags_EnterReturnsTrue)) {
		pEditorManager_->ExecuteCommand<EntityRenameCommand>(entity, newName_);
		renameEntityGuid_ = ONEngine::Guid::kInvalid; // 完了したらリセット
	}

	// フォーカスが外れたらリネームキャンセル
	if(ONEngine::Input::TriggerMouse(ONEngine::Mouse::Right) || ONEngine::Input::TriggerKey(DIK_ESCAPE)) {
		renameEntityGuid_ = ONEngine::Guid::kInvalid;
	}
}

void HierarchyWindow::DrawDialog() {
	if(ImGuiFileDialog::Instance()->Display("Dialog", ImGuiWindowFlags_NoDocking)) {
		if(ImGuiFileDialog::Instance()->IsOk()) {
			std::string filePathName = ImGuiFileDialog::Instance()->GetFilePathName();
			std::string sceneName = std::filesystem::path(filePathName).stem().string();
			pEcsGroup_->RemoveEntityAll();
			pSceneManager_->GetSceneIO()->Input(sceneName, pEcsGroup_);
		}
		ImGuiFileDialog::Instance()->Close();
	}
}

void HierarchyWindow::DrawSceneSaveDialog() {
	if(ImGuiFileDialog::Instance()->Display("save file dialog")) {
		if(ImGuiFileDialog::Instance()->IsOk()) {
			std::string filePathName = ImGuiFileDialog::Instance()->GetFilePathName();
			nlohmann::json j = nlohmann::json::object();
			std::ofstream ofs(filePathName, std::ios::out | std::ios::binary);
			if(ofs) {
				ofs << j.dump(4);
				ofs.close();
			} else {
				ONEngine::Console::LogError("Failed to create file: " + filePathName);
			}
		}
		ImGuiFileDialog::Instance()->Close();
	}
}

bool HierarchyWindow::IsDescendant(ONEngine::GameEntity* ancestor, ONEngine::GameEntity* descendant) {
	if(!descendant) return false;
	ONEngine::GameEntity* current = descendant->GetParent();
	while(current) {
		if(current == ancestor) return true;
		current = current->GetParent();
	}
	return false;
}

void HierarchyWindow::ShowInvalidParentPopup() {
	if(showInvalidParentPopup_) {
		ImGui::OpenPopup("Invalid Parent");
		if(ImGui::BeginPopupModal("Invalid Parent", nullptr, ImGuiWindowFlags_AlwaysAutoResize)) {
			ImGui::Text("Cannot set a descendant as a parent!");
			if(ImGui::Button("OK")) {
				ImGui::CloseCurrentPopup();
				showInvalidParentPopup_ = false;
			}
			ImGui::EndPopup();
		}
	}
}

void HierarchyWindow::DrawEntity(ONEngine::GameEntity* entity) {
	bool hasChildren = !entity->GetChildren().empty();
	flatHierarchyGuids_.push_back(entity->GetGuid());

	ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags_OpenOnArrow | ImGuiTreeNodeFlags_SpanFullWidth | ImGuiTreeNodeFlags_AllowItemOverlap | ImGuiTreeNodeFlags_FramePadding;
	ImGui::PushID(entity->GetId());
	bool isSelected = ImGuiSelection::IsSelected(entity->GetGuid());
	if(isSelected) flags |= ImGuiTreeNodeFlags_Selected;
	if(!hasChildren) flags |= ImGuiTreeNodeFlags_Leaf | ImGuiTreeNodeFlags_NoTreePushOnOpen;

	bool nodeOpen = ImGui::TreeNodeEx((void*)entity, flags, "");
	HandleEntityDragDrop(entity);

	if (isMarqueeSelecting_) {
		ImVec2 itemMin = ImGui::GetItemRectMin();
		ImVec2 itemMax = ImGui::GetItemRectMax();
		if (itemMax.x > marqueeMin_.x && itemMin.x < marqueeMax_.x && itemMax.y > marqueeMin_.y && itemMin.y < marqueeMax_.y) {
			ImGuiSelection::AddSelectedObject(entity->GetGuid(), SelectionType::Entity);
		}
	}

	if(DrawEntityContextMenu(entity, isSelected)) {
		ImGui::PopID();
		if(hasChildren && nodeOpen) ImGui::TreePop();
		return;
	}

	if(ImGui::IsItemHovered()) {
		if(ImGui::IsMouseClicked(ImGuiMouseButton_Left)) {
			if(ImGui::GetIO().KeyCtrl) {
				if(isSelected) ImGuiSelection::RemoveSelectedObject(entity->GetGuid());
				else ImGuiSelection::AddSelectedObject(entity->GetGuid(), SelectionType::Entity);
			} else {
				ImGuiSelection::SetSelectedObject(entity->GetGuid(), SelectionType::Entity);
			}
		}
		if(ImGui::IsMouseDoubleClicked(ImGuiMouseButton_Left)) {
			renameEntityGuid_ = entity->GetGuid();
			newName_ = entity->GetName();
		}
	}

	ImGui::SameLine();
	
	// アクティブフラグのチェックボックスを復元
	{
		std::string label = "##active" + std::to_string(entity->GetId());
		if (ImGui::Checkbox(label.c_str(), &entity->active)) {
			// 必要に応じてコマンド化
		}
	}

	ImGui::SameLine();

	if(renameEntityGuid_ == entity->GetGuid()) {
		EntityRename(entity);
	} else {
		ImGui::Text("%s", entity->GetName().c_str());
	}

	HandleEntityShortcuts(entity, isSelected);
	ImGui::PopID();

	if(hasChildren && nodeOpen) {
		for(auto* child : entity->GetChildren()) DrawEntity(child);
		ImGui::TreePop();
	}
}

void HierarchyWindow::HandleRootDragDrop() {
	ImGui::Spacing();
	ImVec2 windowSize = ImGui::GetContentRegionAvail();
	
	// クラッシュ防止：サイズが 0 以下にならないようにガード
	windowSize.x = (std::max)(windowSize.x, 1.0f);
	windowSize.y = (std::max)(windowSize.y, 20.0f);

	ImGui::InvisibleButton("HierarchyDropArea", windowSize);
	if(ImGui::BeginDragDropTarget()) {
		if(const ImGuiPayload* payload = ImGui::AcceptDragDropPayload("EntityData")) {
			ONEngine::GameEntity** srcEntityPtr = static_cast<ONEngine::GameEntity**>(payload->Data);
			ONEngine::GameEntity* srcEntity = *srcEntityPtr;
			pEditorManager_->ExecuteCommand<ChangeEntityParentCommand>(srcEntity, nullptr);
		}
		ImGui::EndDragDropTarget();
	}
}

void HierarchyWindow::HandleEntityDragDrop(ONEngine::GameEntity* entity) {
	if(ImGui::BeginDragDropSource()) {
		ImGui::Text(entity->GetName().c_str());
		ONEngine::GameEntity** entityPtr = &entity;
		ImGui::SetDragDropPayload("EntityData", entityPtr, sizeof(ONEngine::GameEntity**));
		ImGui::EndDragDropSource();
	}

	if(ImGui::BeginDragDropTarget()) {
		if(const ImGuiPayload* payload = ImGui::AcceptDragDropPayload("EntityData")) {
			ONEngine::GameEntity** srcEntityPtr = static_cast<ONEngine::GameEntity**>(payload->Data);
			ONEngine::GameEntity* srcEntity = *srcEntityPtr;
			if(srcEntity != entity) {
				if(!IsDescendant(srcEntity, entity)) {
					float mouseY = ImGui::GetMousePos().y;
					float itemMinY = ImGui::GetItemRectMin().y;
					float itemMaxY = ImGui::GetItemRectMax().y;
					float height = itemMaxY - itemMinY;
					if (mouseY < itemMinY + height * 0.25f || mouseY > itemMinY + height * 0.75f) {
						// Reorder logic (simplified)
						pEditorManager_->ExecuteCommand<ChangeEntityParentCommand>(srcEntity, entity->GetParent());
					} else {
						pEditorManager_->ExecuteCommand<ChangeEntityParentCommand>(srcEntity, entity);
					}
				} else {
					showInvalidParentPopup_ = true;
				}
			}
		}
		ImGui::EndDragDropTarget();
	}
}

bool HierarchyWindow::DrawEntityContextMenu(ONEngine::GameEntity* entity, bool selected) {
	bool isDeleted = false;
	if(ImGui::IsItemHovered() && ImGui::IsMouseClicked(ImGuiMouseButton_Right)) ImGui::OpenPopup("EntityContextMenu");
	if(ImGui::BeginPopup("EntityContextMenu")) {
		if(ImGui::BeginMenu("create")) {
			if(ImGui::MenuItem("empty object")) pEditorManager_->ExecuteCommand<CreateGameObjectCommand>(pEcsGroup_, "NewEntity", entity);
			ImGui::Separator();
			if(ImGui::MenuItem("Camera")) pEditorManager_->ExecuteCommand<CreatePrimitiveCommand>(pEcsGroup_, CreatePrimitiveCommand::Type::Camera, entity);
			if(ImGui::MenuItem("Directional Light")) pEditorManager_->ExecuteCommand<CreatePrimitiveCommand>(pEcsGroup_, CreatePrimitiveCommand::Type::DirectionalLight, entity);
			if(ImGui::MenuItem("Mesh")) pEditorManager_->ExecuteCommand<CreatePrimitiveCommand>(pEcsGroup_, CreatePrimitiveCommand::Type::Mesh, entity);
			ImGui::EndMenu();
		}
		if(ImGui::MenuItem("rename")) { renameEntityGuid_ = entity->GetGuid(); newName_ = entity->GetName(); }
		if(ImGui::MenuItem("delete")) {
			deleteQueue_.push_back(entity->GetGuid());
			if(selected) ImGuiSelection::SetSelectedObject(ONEngine::Guid::kInvalid, SelectionType::None);
			isDeleted = true;
		}
		ImGui::Separator();
		if (ImGui::MenuItem("Create Prefab")) {
			std::string name = entity->GetName();
			pEditorManager_->ExecuteCommand<CreatePrefabCommand>(entity);
			// エンジン側のPrefabキャッシュを更新
			pEcs_->ReloadPrefab(name + ".prefab");
			// このエンティティ自体のPrefab参照を更新
			entity->SetPrefabName(name);
		}
		ImGui::EndPopup();
	}
	return isDeleted;
}

void HierarchyWindow::HandleEntityShortcuts(ONEngine::GameEntity* entity, bool selected) {
	// 修正: DuplicateEntityCommandは存在しないため削除
	if(selected && ImGui::IsWindowFocused() && ImGui::IsKeyPressed(ImGuiKey_Delete)) deleteQueue_.push_back(entity->GetGuid());
}

/// NormalHierarchyWindow Implementation
NormalHierarchyWindow::NormalHierarchyWindow(const std::string& windowName, ONEngine::EntityComponentSystem* ecs, EditorManager* editorManager, ONEngine::SceneManager* sceneManager)
	: HierarchyWindow(windowName, ecs, ecs->GetCurrentGroup(), editorManager, sceneManager), pEcs_(ecs) {}

void NormalHierarchyWindow::ShowImGui() {
	pEcsGroup_ = pEcs_->GetCurrentGroup();
	HierarchyWindow::ShowImGui();
}

void NormalHierarchyWindow::DrawSceneDialog() {}
void NormalHierarchyWindow::HandleGlobalShortcuts() {}

} /// namespace Editor

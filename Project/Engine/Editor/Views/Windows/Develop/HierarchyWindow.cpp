#include "HierarchyWindow.h"

/// std
#include <filesystem>
#include <algorithm>

/// external
#include <imgui.h>
#include <dialog/ImGuiFileDialog.h>
#include <nlohmann/json.hpp>

/// engine
#include "Engine/Core/Config/EngineConfig.h"
#include "Engine/Core/Utility/Math/Math.h"
#include "Engine/ECS/EntityComponentSystem/EntityComponentSystem.h"
#include "Engine/ECS/Entity/GameEntity/GameEntity.h"
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

	/// ヒエラルキーの表示
	DrawHierarchy();

	ImGui::End();
}

void HierarchyWindow::PrefabDragAndDrop() {
	/// ----- Prefabのドラッグアンドドロップ処理 ----- ///

	if(ImGui::BeginDragDropTarget()) {
		if(const ImGuiPayload* payload = ImGui::AcceptDragDropPayload("AssetData")) {
			if(payload->Data) {
				Editor::AssetPayload* assetPayload = *static_cast<Editor::AssetPayload**>(payload->Data);
				const std::string path = assetPayload->filePath;

				if(path.find(".prefab") != std::string::npos) {
					// filesystemを使って安全かつシンプルにファイル名を抽出
					std::string fileName = std::filesystem::path(path).filename().string();

					pEcsGroup_->GenerateEntityFromPrefab(fileName, ONEngine::DebugConfig::isDebugging);
					ONEngine::Console::Log(std::format("entity name set to: {}", fileName));
				} else {
					ONEngine::Console::Log("[error] Invalid entity format. Please use \".prefab\"");
				}
			}
		}

		ImGui::EndDragDropTarget();
	}
}

void HierarchyWindow::DrawMenuBar() {
	/// ----- MenuBarの表示 ----- ///

	if(ImGui::BeginMenuBar()) {
		if(ImGui::BeginMenu("+")) {
			DrawMenuEntity();
			DrawMenuScene();
			ImGui::EndMenu();
		}
		ImGui::EndMenuBar();
	}

	/// シーンの保存、読み込みに使うダイアログの表示
	DrawDialog();
	DrawSceneSaveDialog();
}

void HierarchyWindow::DrawMenuEntity() {
	if(ImGui::BeginMenu("create")) {
		if(ImGui::MenuItem("create empty object")) {
			pEditorManager_->ExecuteCommand<CreateGameObjectCommand>(pEcsGroup_);
		}

		ImGui::Separator();

		if(ImGui::MenuItem("Camera")) {
			pEditorManager_->ExecuteCommand<CreatePrimitiveCommand>(pEcsGroup_, CreatePrimitiveCommand::Type::Camera);
		}
		if(ImGui::MenuItem("Directional Light")) {
			pEditorManager_->ExecuteCommand<CreatePrimitiveCommand>(pEcsGroup_, CreatePrimitiveCommand::Type::DirectionalLight);
		}
		if(ImGui::MenuItem("Mesh")) {
			pEditorManager_->ExecuteCommand<CreatePrimitiveCommand>(pEcsGroup_, CreatePrimitiveCommand::Type::Mesh);
		}

		ImGui::EndMenu();
	}
}

void HierarchyWindow::DrawMenuScene() {
	/// ----- sceneメニューの表示 ----- ///
	if(ImGui::BeginMenu("scene")) {
		if(ImGui::MenuItem("create scene")) {
			IGFD::FileDialogConfig config;
			config.path = "./Assets/Scene";
			ImGuiFileDialog::Instance()->OpenDialog("save file dialog", "ファイル保存", ".json", config);
		}

		if(ImGui::MenuItem("save scene")) {
			pSceneManager_->SaveScene(pEcsGroup_->GetGroupName(), pEcsGroup_);
		}

		if(ImGui::BeginMenu("load scene")) {
			if(ImGui::MenuItem("open explorer")) {
				IGFD::FileDialogConfig config;
				config.path = "./Assets/Scene";
				ImGuiFileDialog::Instance()->OpenDialog("Dialog", "Choose File", ".json", config);
			}
			ImGui::EndMenu();
		}
		ImGui::EndMenu();
	}
}

void HierarchyWindow::DrawHierarchy() {
	std::string groupName = pEcsGroup_->GetGroupName();
	if(pSceneManager_->IsDirty()) {
		groupName += " *";
	}

	flatHierarchyGuids_.clear();
	clickedGuid_ = ONEngine::Guid::kInvalid;

	/// ===================================================
	/// ボックス選択の事前計算
	/// ===================================================
	if (ImGui::IsWindowHovered() && !ImGui::IsAnyItemHovered() && ImGui::IsMouseClicked(ImGuiMouseButton_Left)) {
		isMarqueeSelecting_ = true;
		marqueeStartPos_ = ImGui::GetIO().MousePos;
		if (!ImGui::GetIO().KeyCtrl) {
			ImGuiSelection::ClearSelection();
		}
	}

	if (isMarqueeSelecting_) {
		if (ImGui::IsMouseReleased(ImGuiMouseButton_Left)) {
			isMarqueeSelecting_ = false;
		} else {
			ImVec2 mousePos = ImGui::GetIO().MousePos;
			marqueeMin_ = ImVec2((std::min)(marqueeStartPos_.x, mousePos.x), (std::min)(marqueeStartPos_.y, mousePos.y));
			marqueeMax_ = ImVec2((std::max)(marqueeStartPos_.x, mousePos.x), (std::max)(marqueeStartPos_.y, mousePos.y));
			
			// 背景に矩形を描画
			ImVec4 col = ImGui::GetStyleColorVec4(ImGuiCol_HeaderActive);
			col.w = 0.3f;
			ImGui::GetWindowDrawList()->AddRectFilled(marqueeMin_, marqueeMax_, ImGui::GetColorU32(col));
			col.w = 0.8f;
			ImGui::GetWindowDrawList()->AddRect(marqueeMin_, marqueeMax_, ImGui::GetColorU32(col));
		}
	}

	if(ImGui::CollapsingHeader(groupName != "" ? groupName.c_str() : "Unnamed Group", ImGuiTreeNodeFlags_DefaultOpen)) {

		for(auto& entity : pEcsGroup_->GetEntities()) {
			if(!entity->GetParent()) {
				DrawEntity(entity.get());
			}
		}

		HandleRootDragDrop();

		ShowInvalidParentPopup();
	}

	/// ===================================================
	/// クリックによる選択処理 (遅延実行)
	/// ===================================================
	if (clickedGuid_.CheckValid()) {
		if (wasCtrlClicked_) {
			// Ctrl+クリック: 選択状態を反転
			if (ImGuiSelection::IsSelected(clickedGuid_)) {
				ImGuiSelection::RemoveSelectedObject(clickedGuid_);
			} else {
				ImGuiSelection::AddSelectedObject(clickedGuid_, SelectionType::Entity);
			}
			shiftStartGuid_ = clickedGuid_;
		} else if (wasShiftClicked_) {
			// Shift+クリック: 範囲選択
			ONEngine::Guid startGuid = shiftStartGuid_.CheckValid() ? shiftStartGuid_ : ImGuiSelection::GetLastSelectedObject();
			if (startGuid.CheckValid() && startGuid != clickedGuid_) {
				bool inRange = false;
				for (const auto& guid : flatHierarchyGuids_) {
					if (guid == startGuid || guid == clickedGuid_) {
						ImGuiSelection::AddSelectedObject(guid, SelectionType::Entity);
						inRange = !inRange;
					} else if (inRange) {
						ImGuiSelection::AddSelectedObject(guid, SelectionType::Entity);
					}
				}
			} else {
				ImGuiSelection::SetSelectedObject(clickedGuid_, SelectionType::Entity);
				shiftStartGuid_ = clickedGuid_;
			}
		} else {
			// 通常クリック: 単一選択
			ImGuiSelection::SetSelectedObject(clickedGuid_, SelectionType::Entity);
			shiftStartGuid_ = clickedGuid_;
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

			// filesystemを使用して、ディレクトリパスと拡張子を除外したファイル名だけを取得
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
	if(!descendant) {
		return false;
	}

	ONEngine::GameEntity* current = descendant->GetParent();
	while(current) {
		if(current == ancestor) {
			return true;
		}
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



///	-------------------------------------------------------------------------------------------------------------------------------------------------------------------
/// エンティティのエディタ表示
///	-------------------------------------------------------------------------------------------------------------------------------------------------------------------


///
/// エンティティのエディタ表示
/// 
void HierarchyWindow::DrawEntity(ONEngine::GameEntity* entity) {
	bool hasChildren = !entity->GetChildren().empty();

	flatHierarchyGuids_.push_back(entity->GetGuid());

	ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags_OpenOnArrow | ImGuiTreeNodeFlags_SpanFullWidth | ImGuiTreeNodeFlags_AllowItemOverlap | ImGuiTreeNodeFlags_FramePadding;

	ImGui::PushID(entity->GetId());

	bool isSelected = ImGuiSelection::IsSelected(entity->GetGuid());

	if(isSelected) {
		flags |= ImGuiTreeNodeFlags_Selected;
	}
	if(!hasChildren) {
		flags |= ImGuiTreeNodeFlags_Leaf | ImGuiTreeNodeFlags_NoTreePushOnOpen;
	}

	/// ===================================================
	/// 1. 開閉ボタン（矢印）と行ベースの描画
	/// ===================================================
	// ラベルを空文字列 ("") にして、矢印と背景の選択ハイライトだけを描画させる
	bool nodeOpen = ImGui::TreeNodeEx((void*)entity, flags, "");

	HandleEntityDragDrop(entity);

	// ボックス選択の判定
	if (isMarqueeSelecting_) {
		ImVec2 itemMin = ImGui::GetItemRectMin();
		ImVec2 itemMax = ImGui::GetItemRectMax();

		// 交差判定
		if (itemMax.x > marqueeMin_.x && itemMin.x < marqueeMax_.x && itemMax.y > marqueeMin_.y && itemMin.y < marqueeMax_.y) {
			ImGuiSelection::AddSelectedObject(entity->GetGuid(), SelectionType::Entity);
		}
	}

	// コンテキストメニューで削除されたかどうかをチェック
	if(DrawEntityContextMenu(entity, isSelected)) {
		// 削除された場合はImGuiのスタックを戻して、即座に関数を抜ける
		ImGui::PopID();

		// ★修正箇所：子を持たない（Leaf）の場合は TreePop を呼んではいけない
		if(hasChildren && nodeOpen) {
			ImGui::TreePop();
		}
		return;
	}

	// 行がホバーされているときの処理
	if(ImGui::IsItemHovered()) {
		// クリックで選択
		if(ImGui::IsMouseClicked(ImGuiMouseButton_Left) && !ImGui::IsItemToggledOpen()) {
			clickedGuid_ = entity->GetGuid();
			wasShiftClicked_ = ImGui::GetIO().KeyShift;
			wasCtrlClicked_ = ImGui::GetIO().KeyCtrl;
		}

		// ダブルクリックでフォーカス
		if(ImGui::IsMouseDoubleClicked(ImGuiMouseButton_Left)) {
			EditCommand::Execute<FocusEntityCommand>(pEcs_, entity);
		}
	}

	/// ===================================================
	/// 2. アクティブ / 非アクティブ のチェックボックス
	/// ===================================================
	ImGui::SameLine();

	bool isActive = entity->active;
	if(ImGui::Checkbox("##Active", &isActive)) {
		entity->active = isActive;
	}

	/// ===================================================
	/// 3. 名前の表示 または リネーム入力欄
	/// ===================================================
	ImGui::SameLine();

	if(renameEntityGuid_.CheckValid() && renameEntityGuid_ == entity->GetGuid()) {
		EntityRename(entity);
	} else {
		// 通常の名前表示
		ImGui::Text("%s", entity->GetName().c_str());

		// テキスト部分がホバーされているときの処理
		if(ImGui::IsItemHovered()) {
			// クリックで選択（直感的なUXのため）
			if(ImGui::IsMouseClicked(ImGuiMouseButton_Left)) {
				ImGuiSelection::SetSelectedObject(entity->GetGuid(), SelectionType::Entity);
			}
			// ★追加：テキスト部分のダブルクリックでフォーカス
			if(ImGui::IsMouseDoubleClicked(ImGuiMouseButton_Left)) {
				// 例: pEditorManager_->ExecuteCommand<FocusEntityCommand>(entity);
				// （Fキーを押した際と同じ関数やコマンドをここに記述してください）
			}
		}
	}

	/// ===================================================
	/// 4. ショートカット処理の呼び出し
	/// ===================================================
	HandleEntityShortcuts(entity, isSelected);

	ImGui::PopID();

	/// 子エンティティの再帰的描画
	if(hasChildren && nodeOpen) {
		for(auto* child : entity->GetChildren()) {
			DrawEntity(child);
		}
		ImGui::TreePop();
	}
}


///
/// 親子関係の解除/ルートへの移動
///
void HierarchyWindow::HandleRootDragDrop() {
	// 階層の下部にドロップエリアを確保
	ImGui::Spacing();
	ImGui::Spacing();

	ImVec2 windowSize = ImGui::GetContentRegionAvail();
	windowSize.y = (std::max)(windowSize.y, 20.0f); // 最低20pxのドロップ領域
	if(windowSize.x == 0.0f) {
		windowSize.x = 20.0f;
	}

	// 階層の隙間に透明な判定エリアを作る
	ImGui::InvisibleButton("HierarchyDropArea", windowSize);
	if(ImGui::BeginDragDropTarget()) {
		if(const ImGuiPayload* payload = ImGui::AcceptDragDropPayload("EntityData")) {
			ONEngine::GameEntity** srcEntityPtr = static_cast<ONEngine::GameEntity**>(payload->Data);
			ONEngine::GameEntity* srcEntity = *srcEntityPtr;

			// ルートの最後に移動
			uint32_t lastIndex = static_cast<uint32_t>(pEcsGroup_->GetEntities().size());
			pEditorManager_->ExecuteCommand<ReorderEntityCommand>(pEcsGroup_, srcEntity, nullptr, lastIndex);
		}
		ImGui::EndDragDropTarget();
	}
}


///
/// エンティティ同士の親子関係の構築
///
void HierarchyWindow::HandleEntityDragDrop(ONEngine::GameEntity* entity) {
	// ドラッグ開始（Source）
	if(ImGui::BeginDragDropSource()) {
		ImGui::Text(entity->GetName().c_str());
		ONEngine::GameEntity** entityPtr = &entity;
		ImGui::SetDragDropPayload("EntityData", entityPtr, sizeof(ONEngine::GameEntity**));
		ImGui::EndDragDropSource();
	}

	// ドロップ受け入れ（Target）
	if(ImGui::BeginDragDropTarget()) {
		if(const ImGuiPayload* payload = ImGui::AcceptDragDropPayload("EntityData")) {
			ONEngine::GameEntity** srcEntityPtr = static_cast<ONEngine::GameEntity**>(payload->Data);
			ONEngine::GameEntity* srcEntity = *srcEntityPtr;

			// 自分自身や、自分の子孫ノードを親にしようとしていないかチェック
			if(srcEntity != entity) {
				if(!IsDescendant(srcEntity, entity)) {

					// ドロップ位置によって、親子関係か並び替えかを判断する
					float mouseY = ImGui::GetMousePos().y;
					float itemMinY = ImGui::GetItemRectMin().y;
					float itemMaxY = ImGui::GetItemRectMax().y;
					float height = itemMaxY - itemMinY;

					if (mouseY < itemMinY + height * 0.25f) {
						// 上部にドロップ：ターゲットの前に移動（同階層）
						uint32_t targetIndex = 0;
						ONEngine::GameEntity* parent = entity->GetParent();
						if (parent) {
							const auto& children = parent->GetChildren();
							auto it = std::find(children.begin(), children.end(), entity);
							targetIndex = static_cast<uint32_t>(std::distance(children.begin(), it));
						} else {
							const auto& entities = pEcsGroup_->GetEntities();
							auto it = std::find_if(entities.begin(), entities.end(), [entity](const auto& e) { return e.get() == entity; });
							targetIndex = static_cast<uint32_t>(std::distance(entities.begin(), it));
						}
						pEditorManager_->ExecuteCommand<ReorderEntityCommand>(pEcsGroup_, srcEntity, parent, targetIndex);
					} else if (mouseY > itemMinY + height * 0.75f) {
						// 下部にドロップ：ターゲットの後に移動（同階層）
						uint32_t targetIndex = 0;
						ONEngine::GameEntity* parent = entity->GetParent();
						if (parent) {
							const auto& children = parent->GetChildren();
							auto it = std::find(children.begin(), children.end(), entity);
							targetIndex = static_cast<uint32_t>(std::distance(children.begin(), it)) + 1;
						} else {
							const auto& entities = pEcsGroup_->GetEntities();
							auto it = std::find_if(entities.begin(), entities.end(), [entity](const auto& e) { return e.get() == entity; });
							targetIndex = static_cast<uint32_t>(std::distance(entities.begin(), it)) + 1;
						}
						pEditorManager_->ExecuteCommand<ReorderEntityCommand>(pEcsGroup_, srcEntity, parent, targetIndex);
					} else {
						// 中央にドロップ：ターゲットの子にする
						pEditorManager_->ExecuteCommand<ChangeEntityParentCommand>(srcEntity, entity);
					}

				} else {
					showInvalidParentPopup_ = true;
					ONEngine::Console::LogError("ドロップ先エンティティがドラッグ元エンティティの子であるためドロップできません");
				}
			}
		}

		// プレビューの描画
		float mouseY = ImGui::GetMousePos().y;
		float itemMinY = ImGui::GetItemRectMin().y;
		float itemMaxY = ImGui::GetItemRectMax().y;
		float height = itemMaxY - itemMinY;

		if (mouseY < itemMinY + height * 0.25f) {
			ImGui::GetWindowDrawList()->AddLine(ImVec2(ImGui::GetItemRectMin().x, ImGui::GetItemRectMin().y), ImVec2(ImGui::GetItemRectMax().x, ImGui::GetItemRectMin().y), ImColor(255, 255, 0), 2.0f);
		} else if (mouseY > itemMinY + height * 0.75f) {
			ImGui::GetWindowDrawList()->AddLine(ImVec2(ImGui::GetItemRectMin().x, ImGui::GetItemRectMax().y), ImVec2(ImGui::GetItemRectMax().x, ImGui::GetItemRectMax().y), ImColor(255, 255, 0), 2.0f);
		} else {
			ImGui::GetWindowDrawList()->AddRect(ImGui::GetItemRectMin(), ImGui::GetItemRectMax(), ImColor(255, 255, 0), 0.0f, 0, 2.0f);
		}

		ImGui::EndDragDropTarget();
	}
}

///
/// エンティティの右クリックメニューの処理
///
bool HierarchyWindow::DrawEntityContextMenu(ONEngine::GameEntity* entity, bool selected) {
	bool isDeleted = false;

	if(ImGui::IsItemHovered() && ImGui::IsMouseClicked(ImGuiMouseButton_Right)) {
		ImGui::OpenPopup("EntityContextMenu");
	}

	if(ImGui::BeginPopup("EntityContextMenu")) {
		if(ImGui::BeginMenu("create")) {
			if(ImGui::MenuItem("create empty object")) {
				static int count = 0;
				count++;
				std::string name = "NewEntity_" + std::to_string(count);
				pEditorManager_->ExecuteCommand<CreateGameObjectCommand>(pEcsGroup_, name, entity);
			}

			ImGui::Separator();

			if(ImGui::MenuItem("Camera")) {
				pEditorManager_->ExecuteCommand<CreatePrimitiveCommand>(pEcsGroup_, CreatePrimitiveCommand::Type::Camera, entity);
			}
			if(ImGui::MenuItem("Directional Light")) {
				pEditorManager_->ExecuteCommand<CreatePrimitiveCommand>(pEcsGroup_, CreatePrimitiveCommand::Type::DirectionalLight, entity);
			}
			if(ImGui::MenuItem("Mesh")) {
				pEditorManager_->ExecuteCommand<CreatePrimitiveCommand>(pEcsGroup_, CreatePrimitiveCommand::Type::Mesh, entity);
			}

			ImGui::EndMenu();
		}

		if(ImGui::MenuItem("rename")) {
			renameEntityGuid_ = entity->GetGuid();
			newName_ = entity->GetName();
		}

		if(ImGui::MenuItem("delete")) {
			deleteQueue_.push_back(entity->GetGuid());
			renameEntityGuid_ = ONEngine::Guid::kInvalid;

			// 削除したエンティティが選択中だった場合は、選択状態も解除する
			if(selected) {
				ImGuiSelection::SetSelectedObject(ONEngine::Guid::kInvalid, SelectionType::None);
			}
			isDeleted = true;
		}
		ImGui::EndPopup();
	}

	return isDeleted;
}

///
/// ノードごとのショートカット入力
///
void HierarchyWindow::HandleEntityShortcuts(ONEngine::GameEntity* entity, bool selected) {
	if(selected) {
		if(ONEngine::Input::PressKey(DIK_LCONTROL) || ONEngine::Input::PressKey(DIK_RCONTROL)) {
			if(ONEngine::Input::TriggerKey(DIK_C)) {
				EditCommand::Execute<CopyEntityCommand>(entity);
			}
		}
	}
}


/// /////////////////////////////////////////////////////////////////////////
/// ImGuiNormalHierarchyWindow
/// /////////////////////////////////////////////////////////////////////////

NormalHierarchyWindow::NormalHierarchyWindow(const std::string& windowName, ONEngine::EntityComponentSystem* _ecs, EditorManager* editorManager, ONEngine::SceneManager* sceneManager)
	: HierarchyWindow(windowName, _ecs, nullptr, editorManager, sceneManager) {
	pEcs_ = _ecs;
}

void NormalHierarchyWindow::ShowImGui() {
	if(!ImGui::Begin(windowName_.c_str(), nullptr, ImGuiWindowFlags_MenuBar)) {
		ImGui::End();
		return;
	}

	// ショートカット処理の前に最新のグループを取得しておく
	pEcsGroup_ = pEcs_->GetCurrentGroup();

	HandleGlobalShortcuts();

	PrefabDragAndDrop();

	if(ImGui::BeginMenuBar()) {
		if(ImGui::BeginMenu("+")) {
			DrawMenuEntity();
			DrawMenuScene();
			ImGui::EndMenu();
		}
		ImGui::EndMenuBar();
	}

	DrawSceneDialog();
	DrawSceneSaveDialog();
	DrawHierarchy();

	ImGui::End();
}

void NormalHierarchyWindow::DrawSceneDialog() {
	if(ImGuiFileDialog::Instance()->Display("Dialog", ImGuiWindowFlags_NoDocking)) {
		if(ImGuiFileDialog::Instance()->IsOk()) {
			std::string filePathName = ImGuiFileDialog::Instance()->GetFilePathName();

			// filesystemで簡潔に拡張子を除去
			std::string sceneName = std::filesystem::path(filePathName).stem().string();

			pSceneManager_->LoadScene(sceneName);
		}
		ImGuiFileDialog::Instance()->Close();
	}
}

void NormalHierarchyWindow::HandleGlobalShortcuts() {
	if(ImGui::IsWindowFocused(ImGuiFocusedFlags_RootAndChildWindows)) {
		if(ONEngine::Input::PressKey(DIK_LCONTROL) || ONEngine::Input::PressKey(DIK_RCONTROL)) {
			if(ONEngine::Input::TriggerKey(DIK_V)) {
				if (!pEcsGroup_) return;

				ONEngine::Guid selectedGuid = ImGuiSelection::GetLastSelectedObject();
				if(selectedGuid.CheckValid()) {
					ONEngine::GameEntity* targetEntity = pEcsGroup_->GetEntityFromGuid(selectedGuid);
					if(targetEntity) {
						EditCommand::Execute<PasteEntityCommand>(pEcsGroup_, targetEntity);
					} else {
						// 選択中だが現在のグループにいない場合はルートにペースト
						EditCommand::Execute<PasteEntityCommand>(pEcsGroup_, nullptr);
					}
				} else {
					EditCommand::Execute<PasteEntityCommand>(pEcsGroup_, nullptr);
				}
			}
		}
	}
}

} /// namespace Editor

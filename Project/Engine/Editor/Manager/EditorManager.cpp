#include "EditorManager.h"

/// engine
#include "Engine/Core/DirectX12/Manager/DxManager.h"
#include "Engine/Scene/SceneManager.h"
#include "Engine/Core/Utility/Utility.h"
#include "Engine/Core/Utility/Input/Input.h"
#include "Engine/Asset/Collection/AssetCollection.h"

#include "EditCommand.h"
#include "Engine/Editor/Commands/WorldEditorCommands/WorldEditorCommands.h"

/// editor compute
#include "Engine/Editor/EditorCompute/TerrainEditor/TerrainDataOutput.h"
#include "Engine/Editor/EditorCompute/TerrainEditor/TerrainVertexCreator.h"
#include "Engine/Editor/EditorCompute/TerrainEditor/TerrainVertexEditorCompute.h"
#include "Engine/Editor/EditorCompute/TerrainEditor/RiverTerrainAdjustPipeline.h"
#include "Engine/Editor/EditorCompute/River/RiverMeshGeneratePipeline.h"
#include "Engine/Editor/EditorCompute/Grass/GrassArrangementPipeline.h"
#include "Engine/Editor/EditorCompute/VoxelTerrainEditor/VoxelTerrainEditorComputePipeline.h"
#include "Engine/Editor/EditorCompute/GameEntityPicking/GameEntityPickingPipeline.h"

#include "HotReloadManager.h"
#include "Engine/Script/MonoScriptEngine.h"

using namespace Editor;

EditorManager::EditorManager(ONEngine::EntityComponentSystem* _ecs) : pEcs_(_ecs) {}
EditorManager::~EditorManager() = default;

void EditorManager::Initialize(ONEngine::DxManager* dxm, ONEngine::ShaderCompiler* sc, ONEngine::SceneManager* sm) {
	pDxManager_ = dxm;
	pSceneManager_ = sm;
	runningCommand_ = nullptr;

	/// EditCommandへEditorManagerのポインタを渡す
	EditCommand::pEditorManager_ = this;

	/// editor compute の登録
	AddEditorCompute(dxm, sc, std::make_unique<GameEntityPickingPipeline>());
	AddEditorCompute(dxm, sc, std::make_unique<TerrainDataOutput>());
	AddEditorCompute(dxm, sc, std::make_unique<TerrainVertexCreator>());
	AddEditorCompute(dxm, sc, std::make_unique<TerrainVertexEditorCompute>());
	AddEditorCompute(dxm, sc, std::make_unique<RiverMeshGeneratePipeline>());
	AddEditorCompute(dxm, sc, std::make_unique<RiverTerrainAdjustPipeline>());
	AddEditorCompute(dxm, sc, std::make_unique<GrassArrangementPipeline>());
	AddEditorCompute(dxm, sc, std::make_unique<VoxelTerrainEditorComputePipeline>());
}

void EditorManager::Update(ONEngine::Asset::AssetCollection* ac) {

	/// ホットリロードリクエストの処理（フレームの開始時に行うことでD3D12の状態整合性を保つ）
	auto hrRequests = Editor::HotReloadManager::GetInstance().ConsumeRequests();
	bool isReloaded = false;
	for (const auto& path : hrRequests.assetPaths) {
		ONEngine::Console::Log("[HotReload] Reloading asset: " + path, ONEngine::LogCategory::Engine);
		ac->ReloadAsset(path);
		isReloaded = true;
	}
	if (hrRequests.scriptHotReload) {
		ONEngine::Console::Log("[HotReload] Script hot-reload requested.", ONEngine::LogCategory::ScriptEngine);
		ONEngine::MonoScriptEngine::GetInstance().HotReload();
		isReloaded = true;
	}

	/// リロードが行われた場合はコマンドリストがリセットされているため、ヒープを再バインドする
	if (isReloaded) {
		pDxManager_->HeapBindToCommandList();
	}

	/// エディタのコマンドを実行する
	for (auto& compute : editorComputes_) {
		compute->Execute(pEcs_, pDxManager_->GetDxCommand(), ac);
	}

	if (runningCommand_) {
		EDITOR_STATE state = runningCommand_->Execute();
		if (state != EDITOR_STATE_RUNNING) {
			runningCommand_ = nullptr;
		} else {
			ONEngine::Console::Log("editor command is running");
		}

	} else {
#ifdef DEBUG_MODE
		// undo, redo を行う
		if (ONEngine::Input::PressKey(DIK_LCONTROL) && ONEngine::Input::TriggerKey(DIK_Z)) {
			Undo();
		}

		if (ONEngine::Input::PressKey(DIK_LCONTROL) && ONEngine::Input::TriggerKey(DIK_Y)) {
			Redo();
		}

		// Ctrl+S でシーンを保存
		if (ONEngine::Input::PressKey(DIK_LCONTROL) && ONEngine::Input::TriggerKey(DIK_S)) {
			pSceneManager_->SaveCurrentScene();
			ONEngine::Console::Log("Scene saved via Ctrl+S.");
		}
#endif // DEBUG_MODE
	}

}



void EditorManager::Undo() {
	if (commandStack_.empty()) {
		return;
	}
	std::unique_ptr<IEditCommand> command = std::move(commandStack_.back());
	command->Undo();
	redoStack_.push_back(std::move(command));
	commandStack_.pop_back();

	MarkSceneDirty();
}

void EditorManager::Redo() {
	if (redoStack_.empty()) {
		return;
	}

	/// stackから実行する
	std::unique_ptr<IEditCommand> command = std::move(redoStack_.back());
	command->Execute();
	redoStack_.pop_back();

	/// command stackに戻す
	commandStack_.push_back(std::move(command));

	MarkSceneDirty();
}

void EditorManager::MarkSceneDirty() {
	if (pSceneManager_) {
		pSceneManager_->MarkDirty();
	}
}

void EditorManager::AddEditorCompute(ONEngine::DxManager* dxm, ONEngine::ShaderCompiler* sc, std::unique_ptr<IEditorCompute> compute) {
	compute->Initialize(sc, dxm);

	editorComputes_.push_back(std::move(compute));
}

#include "SceneIO.h"

using namespace ONEngine;

/// std
#include <iostream>
#include <fstream>

/// external
#include <nlohmann/json.hpp>

/// engine
#include "Engine/ECS/Entity/EntityJsonConverter.h"
#include "Engine/ECS/EntityComponentSystem/EntityComponentSystem.h"
#include "Engine/Editor/Commands/ComponentEditCommands/ComponentJsonConverter.h"
#include "Engine/Script/MonoScriptEngine.h"
#include "Engine/Core/Utility/FileSystem/FileSystem.h"

SceneIO::SceneIO(EntityComponentSystem* _ecs) : pEcs_(_ecs) {
	fileName_ = "";
	fileDirectory_ = "./Assets/Scene/";
}
SceneIO::~SceneIO() {}

void SceneIO::Output(const std::string& _sceneName, ECSGroup* _ecsGroup) {
	/* sceneをjsonに保存する */
	fileName_ = _sceneName + ".scene";
	SaveScene(fileName_, _ecsGroup);
}

void SceneIO::Input(const std::string& _sceneName, ECSGroup* _ecsGroup) {
	/* jsonを読み込んでsceneに変換する */
	fileName_ = _sceneName + ".scene";
	LoadScene(fileName_, _ecsGroup);
}

void SceneIO::OutputTemporary(ECSGroup* _ecsGroup) {
	tempSceneJson_.clear();
	SaveSceneToJson(tempSceneJson_, _ecsGroup);
}

void SceneIO::InputTemporary(ECSGroup* _ecsGroup) {
	MonoScriptEngine::GetInstance().ClearECSGroup(_ecsGroup->GetGroupName());
	LoadSceneFromJson(tempSceneJson_, _ecsGroup);
}

void SceneIO::SaveScene(const std::string& _filename, ECSGroup* _ecsGroup) {
	nlohmann::json sceneJson = nlohmann::json::object();
	std::string sceneName = FileSystem::FileNameWithoutExtension(_filename);
	std::string sceneDir = fileDirectory_ + sceneName + "/";
	std::filesystem::create_directories(sceneDir);

	auto& entities = _ecsGroup->GetEntities();
	for (auto& entity : entities) {
		if (entity->GetId() < 0) continue;

		if (Variables* var = entity->GetComponent<Variables>()) {
			var->ReloadScriptVariables();
		}

		nlohmann::json entityJson = EntityJsonConverter::ToJson(entity.get());
		if (entityJson.empty()) continue;

		std::string entityFileName = entity->GetName() + ".entity";
		std::string entityPath = sceneDir + entityFileName;

		// .entityファイルを保存
		std::ofstream ofs(entityPath);
		if (ofs) {
			ofs << entityJson.dump(4);
			ofs.close();
		}

		// シーンファイルには参照を保存
		nlohmann::json reference;
		reference["path"] = "./" + sceneName + "/" + entityFileName;
		reference["id"] = entity->GetId();
		if (entity->GetParent()) {
			reference["parent"] = entity->GetParent()->GetId();
		} else {
			reference["parent"] = nullptr;
		}
		sceneJson["entities"].push_back(reference);
	}

	OutputJson(sceneJson, _filename);
}

void SceneIO::LoadScene(const std::string& _filename, ECSGroup* _ecsGroup) {
	MonoScriptEngine::GetInstance().ClearECSGroup(_ecsGroup->GetGroupName());

	std::ifstream inputFile(fileDirectory_ + _filename);
	if (!inputFile.is_open()) {
		Console::Log("SceneIO: ファイルのオープンに失敗しました: " + fileDirectory_ + _filename);
		return;
	}

	nlohmann::json sceneJson;
	inputFile >> sceneJson;
	inputFile.close();

	if (!sceneJson.contains("entities")) return;

	nlohmann::json fullSceneJson = nlohmann::json::object();
	for (const auto& entityRef : sceneJson["entities"]) {
		if (entityRef.contains("path")) {
			std::string entityPath = entityRef["path"];
			std::ifstream entityFile(fileDirectory_ + entityPath);
			if (entityFile.is_open()) {
				nlohmann::json entityJson;
				entityFile >> entityJson;
				entityFile.close();

				// シーンファイル側の情報を優先（親子関係など）
				if (entityRef.contains("id")) entityJson["id"] = entityRef["id"];
				if (entityRef.contains("parent")) entityJson["parent"] = entityRef["parent"];

				fullSceneJson["entities"].push_back(entityJson);
			} else {
				Console::LogError("SceneIO: Entityファイルの読み込みに失敗しました: " + entityPath);
			}
		} else {
			// 旧フォーマット（直接エンティティデータが入っている場合）への対応
			fullSceneJson["entities"].push_back(entityRef);
		}
	}

	LoadSceneFromJson(fullSceneJson, _ecsGroup);
}

void SceneIO::SaveSceneToJson(nlohmann::json& _output, ECSGroup* _ecsGroup) {

	auto& entities = _ecsGroup->GetEntities();
	for (auto& entity : entities) {
		/// マイナスIDはruntimeに生成されたエンティティなのでスキップ
		if (entity->GetId() < 0) {
			continue;
		}

		if (Variables* var = entity->GetComponent<Variables>()) {
			var->ReloadScriptVariables();
		}

		nlohmann::json entityJson = EntityJsonConverter::ToJson(entity.get());
		if (entityJson.empty()) {
			continue; // エンティティの情報が空ならスキップ
		}

		_output["entities"].push_back(entityJson);
	}

}

void SceneIO::LoadSceneFromJson(const nlohmann::json& _input, ECSGroup* _ecsGroup) {
	std::unordered_map<uint32_t, GameEntity*> entityMap;

	if (!_input.contains("entities")) {
		return;
	}

	/// 実際にシーンに変換する
	for (const auto& entityJson : _input["entities"]) {
		const std::string& prefabName = entityJson.value("prefabName", "");
		const std::string& entityName = entityJson.value("name", "");
		const uint32_t entityId = entityJson.value("id", 0);

		/// guidの取得、無効値なら新規生成
		Guid guid = entityJson.value("guid", GenerateGuid());
		if (guid == Guid::kInvalid) {
			guid = GenerateGuid();
		}

		GameEntity* entity = _ecsGroup->GenerateEntity(guid, false);
		if (!prefabName.empty()) {
			_ecsGroup->GetEntityCollection()->ApplyPrefabToEntity(entity, prefabName);
		}

		if (entity) {
			entity->prefabName_ = prefabName;
			entity->name_ = entityName;

			/// シーンに保存されたjsonからエンティティを復元
			EntityJsonConverter::FromJson(entityJson, entity, _ecsGroup->GetGroupName());

			entityMap[entityId] = entity;
		}
	}


	/// エンティティの親子関係を設定
	for (const auto& entityJson : _input["entities"]) {
		int32_t entityId = entityJson["id"];
		if (entityMap.find(entityId) == entityMap.end()) {
			continue; // エンティティが見つからない場合はスキップ
		}

		GameEntity* entity = entityMap[entityId];
		if (entityJson.contains("parent") && !entityJson["parent"].is_null()) {
			int32_t parentId = entityJson["parent"];
			if (entityMap.find(parentId) != entityMap.end()) {
				entity->SetParent(entityMap[parentId]);
			}
		}
	}

	// C++でロードしたデータをC#側に同期する
	MonoScriptEngine::GetInstance().SyncInitialComponentsToCS(_ecsGroup);
}

void SceneIO::OutputJson(const nlohmann::json& _json, const std::string& _filename) {
	/// ファイルが無かったら生成する
	if (!std::filesystem::exists(fileDirectory_ + _filename)) {
		std::filesystem::create_directories(fileDirectory_);
	}

	/// ファイルに保存する
	std::ofstream outputFile(fileDirectory_ + _filename);
	if (!outputFile.is_open()) {
		Console::LogError("SceneIO: ファイルのオープンに失敗しました: " + fileDirectory_ + _filename);
	}

	outputFile << _json.dump(4);
	outputFile.close();
}

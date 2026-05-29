#include "Variables.h"

/// std
#include <fstream>
#include <filesystem>
#include <format>
#include <vector>
#include <variant>

/// external
#include <imgui.h>
#include <Externals/imgui/dialog/ImGuiFileDialog.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/blob.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/class.h>

/// engine
#include "Engine/Core/Utility/Math/Math.h"
#include "Engine/ECS/EntityComponentSystem/ECSGroup.h"
#include "Engine/ECS/Entity/GameEntity/GameEntity.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Script/Script.h"
#include "Engine/ECS/Entity/EntityJsonConverter.h"
#include "Engine/Editor/Commands/ComponentEditCommands/ComponentJsonConverter.h"
#include "Engine/Script/MonoScriptEngine.h"

/// editor 
#include "Engine/Editor/Math/ImGuiMath.h"

using namespace ONEngine;
using json = nlohmann::json;

namespace {

	bool IsVectorN(const json& j, int n) {
		if (!j.is_object()) {
			return false;
		}

		static const char* keys[] = { "x", "y", "z", "w" };
		for (int i = 0; i < n; ++i) {
			if (!j.contains(keys[i]) || !j[keys[i]].is_number()) {
				return false;
			}
		}
		return true;
	}

}	/// namespace





void ONEngine::from_json(const nlohmann::json& _j, Variables& _v) {
	_v.groupKeyMap_.clear();
	_v.groups_.clear();

	for (auto& [groupKey, groupValue] : _j.items()) {
		if (groupKey == "type") continue;

		/// グループが存在しない場合は新規追加
		if (!_v.HasGroup(groupKey)) {
			_v.AddGroup(groupKey);
		}

		/// グループを取得
		Variables::Group& group = _v.groups_[_v.groupKeyMap_.at(groupKey)];

		for (auto& [varKey, varValue] : groupValue.items()) {
			/// ---------------------------------------------------
			/// 変数の型をチェックして追加
			/// ---------------------------------------------------
			if (varValue.is_array()) {
				if (varValue.empty()) {
					// 空の配列の場合は一旦intのリストとして扱う（後で型が判明した時に上書きされる想定）
					group.Add(varKey, std::vector<int>());
				} else {
					if (varValue[0].is_number_integer()) {
						group.Add(varKey, varValue.get<std::vector<int>>());
					} else if (varValue[0].is_number_float()) {
						group.Add(varKey, varValue.get<std::vector<float>>());
					} else if (varValue[0].is_boolean()) {
						group.Add(varKey, varValue.get<std::vector<bool>>());
					} else if (varValue[0].is_string()) {
						group.Add(varKey, varValue.get<std::vector<std::string>>());
					} else if (IsVectorN(varValue[0], 3)) {
						std::vector<Vector3> vecs;
						for (const auto& v : varValue) vecs.push_back(v);
						group.Add(varKey, vecs);
					}
				}
			} else if (varValue.is_number_integer()) {
				/// ----- int ----- ///
				group.Add(varKey, varValue.get<int>());
			} else if (varValue.is_number_float()) {
				/// ----- float ----- ///
				group.Add(varKey, varValue.get<float>());
			} else if (varValue.is_boolean()) {
				/// ----- bool ----- ///
				group.Add(varKey, varValue.get<bool>());
			} else if (varValue.is_string()) {
				/// ----- string ----- ///
				group.Add(varKey, varValue.get<std::string>());

			} else if (IsVectorN(varValue, 4)) {
				/// ----- Vector4 ----- ///
				Vector4 value = varValue;
				group.Add(varKey, value);
			} else if (IsVectorN(varValue, 3)) {
				/// ----- Vector3 ----- ///
				Vector3 value = varValue;
				group.Add(varKey, value);
			} else if (IsVectorN(varValue, 2)) {
				/// ----- Vector2 ----- ///
				Vector2 value = varValue;
				group.Add(varKey, value);
			}
		}
	}
}

void ONEngine::to_json(nlohmann::json& _j, const Variables& _v) {
	_j = nlohmann::json::object();
	_j["type"] = "Variables";

	for (const auto& [groupKey, value] : _v.groupKeyMap_) {
		_j[groupKey] = nlohmann::json::object();
		for (const auto& [varKey, varValue] : _v.groups_[value].keyMap) {

			std::visit([&_j, &groupKey, &varKey](auto&& _arg) {
				using T = std::decay_t<decltype(_arg)>;
				if constexpr (std::is_same_v<T, int>) {
					_j[groupKey][varKey] = _arg;
				} else if constexpr (std::is_same_v<T, float>) {
					_j[groupKey][varKey] = _arg;
				} else if constexpr (std::is_same_v<T, bool>) {
					_j[groupKey][varKey] = _arg;
				} else if constexpr (std::is_same_v<T, std::string>) {
					_j[groupKey][varKey] = _arg;
				} else if constexpr (std::is_same_v<T, Vector2>) {
					_j[groupKey][varKey] = _arg;
				} else if constexpr (std::is_same_v<T, Vector3>) {
					_j[groupKey][varKey] = _arg;
				} else if constexpr (std::is_same_v<T, Vector4>) {
					_j[groupKey][varKey] = _arg;
				} else if constexpr (std::is_same_v<T, std::vector<int>>) {
					_j[groupKey][varKey] = _arg;
				} else if constexpr (std::is_same_v<T, std::vector<float>>) {
					_j[groupKey][varKey] = _arg;
				} else if constexpr (std::is_same_v<T, std::vector<bool>>) {
					_j[groupKey][varKey] = _arg;
				} else if constexpr (std::is_same_v<T, std::vector<std::string>>) {
					_j[groupKey][varKey] = _arg;
				} else if constexpr (std::is_same_v<T, std::vector<Vector3>>) {
					_j[groupKey][varKey] = nlohmann::json::array();
					for (const auto& v : _arg) {
						_j[groupKey][varKey].push_back(v);
					}
				}
				}, _v.groups_[value].variables[varValue]);
		}
	}
}




Variables::Variables() {
	groupKeyMap_.clear();
	groups_.clear();
}

Variables::~Variables() = default;

void Variables::LoadJson(const std::string& _path) {
	/// 拡張子チェック (.json または .entity)
	std::string ext = FileSystem::FileExtension(_path);
	if (ext != ".json" && ext != ".entity") {
		return;
	}

	/// fileが存在するのかチェック
	if (!std::filesystem::exists(_path)) {
		return;
	}

	nlohmann::json j;

	{	/// load json
		std::ifstream ifs(_path);
		if (!ifs.is_open()) return;
		ifs >> j;
		ifs.close();
	}

	// もし .entity ファイル（エンティティ全体のデータ）が渡された場合、
	// Variables コンポーネントのデータ部分だけを抽出する
	if (j.contains("components")) {
		for (const auto& compJson : j["components"]) {
			if (compJson.value("type", "") == "Variables") {
				from_json(compJson, *this);
				break;
			}
		}
	} else {
		// 純粋な変数データの場合
		from_json(j, *this);
	}


	/// スクリプトの変数を登録
	if (Script* script = GetOwner()->GetComponent<Script>()) {
		for (const auto& data : script->GetScriptDataList()) {
			SetScriptVariables(data.scriptName);
		}
	}

}


void Variables::SaveJson(const std::string& _path) {
	nlohmann::json j;

	/// スクリプトごとにgroupを生成する
	GameEntity* owner = GetOwner();
	if (!owner) {
		Console::LogError("Variables::SaveJson();  owner is nullptr...");
		return;
	}

	to_json(j, *this);
	// to_json includes "type", but for the standalone file we might want to remove it if needed,
	// however, LoadJson uses from_json which ignores "type".
	if (j.contains("type")) {
		j.erase("type");
	}

	std::filesystem::path path(_path);
	std::filesystem::create_directories(path.parent_path());

	std::ofstream ofs(_path);
	if (!ofs) {
		throw std::runtime_error("ファイルを開けませんでした: " + _path);
	}
	ofs << j.dump(4);
}

void Variables::RegisterScriptVariables() {

	Script* script = GetOwner()->GetComponent<Script>();
	if (!script) {
		Console::LogError("Variables::SaveJson();  owner has no Script component...");
		return;
	}

	for (const auto& data : script->GetScriptDataList()) {
		size_t groupIndex = 0;

		/// 新規のグループを追加するか、既存のグループを取得する
		if (!HasGroup(data.scriptName)) {
			groupIndex = AddGroup(data.scriptName);
		} else {
			groupIndex = groupKeyMap_.at(data.scriptName);
		}

		/// スクリプトの変数をグループに追加
		Group& group = groups_[groupIndex];

		{
			MonoScriptEngine& monoEngine = MonoScriptEngine::GetInstance();
			GameEntity* entity = GetOwner();
			MonoObject* safeObj = monoEngine.GetMonoBehaviorFromCS(entity->GetECSGroup()->GetGroupName(), entity->GetId(), data.scriptName);

			if (!safeObj) {
				continue; //!< 対象のスクリプトがない場合はスキップ
			}

			MonoClass* monoClass = mono_object_get_class(safeObj);
			MonoClassField* field = nullptr;
			void* iter = nullptr;

			while ((field = mono_class_get_fields(monoClass, &iter))) {
				const char* fieldName = mono_field_get_name(field);

				// SerializeFieldチェックを削除
				MonoType* fieldType = mono_field_get_type(field);
				int type = mono_type_get_type(fieldType);

				/// 持っている変数ならスキップ
				if (group.Has(fieldName)) {
					continue;
				}

				switch (type) {
				case MONO_TYPE_I4: /// int
				case MONO_TYPE_ENUM: /// enum
				{
					int value = 0;
					mono_field_get_value(safeObj, field, &value);
					group.Add(fieldName, value);
				}
				break;
				case MONO_TYPE_R4: /// float
				{
					float value = 0.0f;
					mono_field_get_value(safeObj, field, &value);
					group.Add(fieldName, value);
				}
				break;
				case MONO_TYPE_BOOLEAN: /// bool
				{
					bool value = false;
					mono_field_get_value(safeObj, field, &value);
					group.Add(fieldName, value);
				}
				break;
				case MONO_TYPE_STRING: /// string
				{
					MonoString* monoStr = nullptr;
					mono_field_get_value(safeObj, field, &monoStr);
					if (monoStr) {
						std::string value = mono_string_to_utf8(monoStr);
						group.Add(fieldName, value);
					}
				}
				break;
				case MONO_TYPE_VALUETYPE: /// 構造体
				{
					MonoClass* fieldClass = mono_class_from_mono_type(fieldType);
					const char* className = mono_class_get_name(fieldClass);

					if (mono_class_is_enum(fieldClass)) {
						int value = 0;
						mono_field_get_value(safeObj, field, &value);
						group.Add(fieldName, value);

					} else if (strcmp(className, "Vector2") == 0) {
						// Vector2
						Vector2 vec2;
						mono_field_get_value(safeObj, field, &vec2);
						group.Add(fieldName, vec2);

					} else if (strcmp(className, "Vector3") == 0) {
						// Vector3
						Vector3 vec3;
						mono_field_get_value(safeObj, field, &vec3);
						group.Add(fieldName, vec3);

					} else if (strcmp(className, "Vector4") == 0) {
						// Vector4
						Vector4 vec4;
						mono_field_get_value(safeObj, field, &vec4);
						group.Add(fieldName, vec4);

					}
				}
				break;
				case MONO_TYPE_GENERICINST:
				{
					MonoObject* listObj = mono_field_get_value_object(mono_domain_get(), field, safeObj);
					if (!listObj) break;

					MonoClass* listClass = mono_object_get_class(listObj);
					if (strcmp(mono_class_get_name(listClass), "List`1") != 0) break;

					MonoMethod* getCountMethod = mono_class_get_method_from_name(listClass, "get_Count", 0);
					MonoObject* countObj = mono_runtime_invoke(getCountMethod, listObj, nullptr, nullptr);
					int count = *(int*)mono_object_unbox(countObj);

					MonoMethod* getItemMethod = mono_class_get_method_from_name(listClass, "get_Item", 1);

					// 型引数の取得
					MonoMethodSignature* sig = mono_method_signature(getItemMethod);
					MonoType* elemType = mono_signature_get_return_type(sig);
					int elemTypeId = mono_type_get_type(elemType);

					if (elemTypeId == MONO_TYPE_I4) {
						std::vector<int> list(count);
						for (int i = 0; i < count; ++i) {
							void* args[1] = { &i };
							MonoObject* item = mono_runtime_invoke(getItemMethod, listObj, args, nullptr);
							list[i] = *(int*)mono_object_unbox(item);
						}
						group.Add(fieldName, list);
					} else if (elemTypeId == MONO_TYPE_R4) {
						std::vector<float> list(count);
						for (int i = 0; i < count; ++i) {
							void* args[1] = { &i };
							MonoObject* item = mono_runtime_invoke(getItemMethod, listObj, args, nullptr);
							list[i] = *(float*)mono_object_unbox(item);
						}
						group.Add(fieldName, list);
					} else if (elemTypeId == MONO_TYPE_BOOLEAN) {
						std::vector<bool> list(count);
						for (int i = 0; i < count; ++i) {
							void* args[1] = { &i };
							MonoObject* item = mono_runtime_invoke(getItemMethod, listObj, args, nullptr);
							list[i] = *(bool*)mono_object_unbox(item);
						}
						group.Add(fieldName, list);
					} else if (elemTypeId == MONO_TYPE_STRING) {
						std::vector<std::string> list(count);
						for (int i = 0; i < count; ++i) {
							void* args[1] = { &i };
							MonoString* item = (MonoString*)mono_runtime_invoke(getItemMethod, listObj, args, nullptr);
							if (item) list[i] = mono_string_to_utf8(item);
						}
						group.Add(fieldName, list);
					} else if (elemTypeId == MONO_TYPE_VALUETYPE) {
						MonoClass* elemClass = mono_class_from_mono_type(elemType);
						if (strcmp(mono_class_get_name(elemClass), "Vector3") == 0) {
							std::vector<Vector3> list(count);
							for (int i = 0; i < count; ++i) {
								void* args[1] = { &i };
								MonoObject* item = mono_runtime_invoke(getItemMethod, listObj, args, nullptr);
								list[i] = *(Vector3*)mono_object_unbox(item);
							}
							group.Add(fieldName, list);
						}
					}
				}
				break;
				}

			}
		}


	}
}

void Variables::ReloadScriptVariables() {
	Script* script = GetOwner()->GetComponent<Script>();
	if (!script) {
		return;
	}

	for (const auto& data : script->GetScriptDataList()) {
		size_t groupIndex = 0;

		/// 既存のグループを取得するか、新規作成する (クリアはしない)
		if (!HasGroup(data.scriptName)) {
			groupIndex = AddGroup(data.scriptName);
		} else {
			groupIndex = groupKeyMap_.at(data.scriptName);
		}

		/// スクリプトの変数をグループに追加
		Group& group = groups_[groupIndex];

		{
			MonoScriptEngine& monoEngine = MonoScriptEngine::GetInstance();
			GameEntity* entity = GetOwner();
			MonoObject* safeObj = monoEngine.GetMonoBehaviorFromCS(entity->GetECSGroup()->GetGroupName(), entity->GetId(), data.scriptName);

			if (!safeObj) {
				continue; //!< 対象のスクリプトがない場合はスキップ (既存の値を維持)
			}

			MonoClass* monoClass = mono_object_get_class(safeObj);
			MonoClassField* field = nullptr;
			void* iter = nullptr;

			while ((field = mono_class_get_fields(monoClass, &iter))) {
				const char* fieldName = mono_field_get_name(field);

				MonoType* fieldType = mono_field_get_type(field);
				int type = mono_type_get_type(fieldType);

				switch (type) {
				case MONO_TYPE_I4: /// int
				case MONO_TYPE_ENUM: /// enum
				{
					int value = 0;
					mono_field_get_value(safeObj, field, &value);
					group.Add(fieldName, value);
				}
				break;
				case MONO_TYPE_R4: /// float
				{
					float value = 0.0f;
					mono_field_get_value(safeObj, field, &value);
					group.Add(fieldName, value);
				}
				break;
				case MONO_TYPE_BOOLEAN: /// bool
				{
					bool value = false;
					mono_field_get_value(safeObj, field, &value);
					group.Add(fieldName, value);
				}
				break;
				case MONO_TYPE_STRING: /// string
				{
					MonoString* monoStr = nullptr;
					mono_field_get_value(safeObj, field, &monoStr);
					if (monoStr) {
						std::string value = mono_string_to_utf8(monoStr);
						group.Add(fieldName, value);
					}
				}
				break;
				case MONO_TYPE_VALUETYPE: /// 構造体
				{
					MonoClass* fieldClass = mono_class_from_mono_type(fieldType);
					const char* className = mono_class_get_name(fieldClass);

					if (mono_class_is_enum(fieldClass)) {
						int value = 0;
						mono_field_get_value(safeObj, field, &value);
						group.Add(fieldName, value);

					} else if (strcmp(className, "Vector2") == 0) {
						Vector2 vec2;
						mono_field_get_value(safeObj, field, &vec2);
						group.Add(fieldName, vec2);
					} else if (strcmp(className, "Vector3") == 0) {
						Vector3 vec3;
						mono_field_get_value(safeObj, field, &vec3);
						group.Add(fieldName, vec3);
					} else if (strcmp(className, "Vector4") == 0) {
						Vector4 vec4;
						mono_field_get_value(safeObj, field, &vec4);
						group.Add(fieldName, vec4);
					}
				}
				break;
				case MONO_TYPE_GENERICINST:
				{
					MonoObject* listObj = mono_field_get_value_object(mono_domain_get(), field, safeObj);
					if (!listObj) break;

					MonoClass* listClass = mono_object_get_class(listObj);
					if (strcmp(mono_class_get_name(listClass), "List`1") != 0) break;

					MonoMethod* getCountMethod = mono_class_get_method_from_name(listClass, "get_Count", 0);
					MonoObject* countObj = mono_runtime_invoke(getCountMethod, listObj, nullptr, nullptr);
					int count = *(int*)mono_object_unbox(countObj);

					MonoMethod* getItemMethod = mono_class_get_method_from_name(listClass, "get_Item", 1);

					// 型引数の取得
					MonoMethodSignature* sig = mono_method_signature(getItemMethod);
					MonoType* elemType = mono_signature_get_return_type(sig);
					int elemTypeId = mono_type_get_type(elemType);

					if (elemTypeId == MONO_TYPE_I4) {
						std::vector<int> list(count);
						for (int i = 0; i < count; ++i) {
							void* args[1] = { &i };
							MonoObject* item = mono_runtime_invoke(getItemMethod, listObj, args, nullptr);
							list[i] = *(int*)mono_object_unbox(item);
						}
						group.Add(fieldName, list);
					} else if (elemTypeId == MONO_TYPE_R4) {
						std::vector<float> list(count);
						for (int i = 0; i < count; ++i) {
							void* args[1] = { &i };
							MonoObject* item = mono_runtime_invoke(getItemMethod, listObj, args, nullptr);
							list[i] = *(float*)mono_object_unbox(item);
						}
						group.Add(fieldName, list);
					} else if (elemTypeId == MONO_TYPE_BOOLEAN) {
						std::vector<bool> list(count);
						for (int i = 0; i < count; ++i) {
							void* args[1] = { &i };
							MonoObject* item = mono_runtime_invoke(getItemMethod, listObj, args, nullptr);
							list[i] = *(bool*)mono_object_unbox(item);
						}
						group.Add(fieldName, list);
					} else if (elemTypeId == MONO_TYPE_STRING) {
						std::vector<std::string> list(count);
						for (int i = 0; i < count; ++i) {
							void* args[1] = { &i };
							MonoString* item = (MonoString*)mono_runtime_invoke(getItemMethod, listObj, args, nullptr);
							if (item) list[i] = mono_string_to_utf8(item);
						}
						group.Add(fieldName, list);
					} else if (elemTypeId == MONO_TYPE_VALUETYPE) {
						MonoClass* elemClass = mono_class_from_mono_type(elemType);
						if (strcmp(mono_class_get_name(elemClass), "Vector3") == 0) {
							std::vector<Vector3> list(count);
							for (int i = 0; i < count; ++i) {
								void* args[1] = { &i };
								MonoObject* item = mono_runtime_invoke(getItemMethod, listObj, args, nullptr);
								list[i] = *(Vector3*)mono_object_unbox(item);
							}
							group.Add(fieldName, list);
						}
					}
				}
				break;
				}
			}
		}
	}
}

void Variables::SetScriptVariables(const std::string& _scriptName) {
	/* ----- スクリプトに対して変数の値を適用する ----- */

	GameEntity* owner = GetOwner();
	if (!owner) {
		Console::LogError("Variables::SetScriptVariables();  owner is nullptr...");
		return;
	}

	Script* script = owner->GetComponent<Script>();
	if (!script) {
		Console::LogError("Variables::SetScriptVariables();  owner has no Script component...");
		return;
	}

	/// 適用の処理
	for (auto& data : script->GetScriptDataList()) {
		///!< 引数のスクリプト名と一致するかチェック
		if (data.scriptName != _scriptName) {
			continue;
		}

		/// 対象のスクリプトのデータを持っているかチェック
		if (!HasGroup(data.scriptName)) {
			continue;
		}

		Group& group = groups_[groupKeyMap_.at(data.scriptName)];


		/// C#側のオブジェクトを取得
		MonoScriptEngine& monoEngine = MonoScriptEngine::GetInstance();
		std::string ecsGroupName = owner->GetECSGroup()->GetGroupName();
		MonoObject* safeObj = monoEngine.GetMonoBehaviorFromCS(ecsGroupName, owner->GetId(), data.scriptName);

		if (!safeObj) {
			continue;
		}

		MonoClass* monoClass = mono_object_get_class(safeObj);
		MonoClassField* field = nullptr;
		void* iter = nullptr;

		while ((field = mono_class_get_fields(monoClass, &iter))) {
			const char* fieldName = mono_field_get_name(field);
			if (!group.Has(fieldName)) {
				continue;
			}

			auto& value = group.Get(fieldName);

			if (std::holds_alternative<int>(value)) {
				/// int
				int val = std::get<int>(value);
				mono_field_set_value(safeObj, field, &val);
			} else if (std::holds_alternative<float>(value)) {
				/// float
				float val = std::get<float>(value);
				mono_field_set_value(safeObj, field, &val);
			} else if (std::holds_alternative<bool>(value)) {
				/// bool
				bool val = std::get<bool>(value);
				mono_field_set_value(safeObj, field, &val);
			} else if (std::holds_alternative<std::string>(value)) {
				/// string
				const std::string& str = std::get<std::string>(value);
				MonoString* monoStr = mono_string_new(mono_domain_get(), str.c_str());
				mono_field_set_value(safeObj, field, monoStr);
			} else if (std::holds_alternative<Vector2>(value)) {
				/// Vector2
				Vector2 vec2 = std::get<Vector2>(value);
				mono_field_set_value(safeObj, field, &vec2);
			} else if (std::holds_alternative<Vector3>(value)) {
				/// Vector3
				Vector3 vec3 = std::get<Vector3>(value);
				mono_field_set_value(safeObj, field, &vec3);
			} else if (std::holds_alternative<Vector4>(value)) {
				/// Vector4
				Vector4 vec4 = std::get<Vector4>(value);
				mono_field_set_value(safeObj, field, &vec4);
			} else if (std::holds_alternative<std::vector<int>>(value)) {
				MonoObject* listObj = mono_field_get_value_object(mono_domain_get(), field, safeObj);
				if (listObj) {
					MonoClass* listClass = mono_object_get_class(listObj);
					MonoMethod* clearMethod = mono_class_get_method_from_name(listClass, "Clear", 0);
					mono_runtime_invoke(clearMethod, listObj, nullptr, nullptr);
					MonoMethod* addMethod = mono_class_get_method_from_name(listClass, "Add", 1);
					auto& list = std::get<std::vector<int>>(value);
					for (int v : list) {
						void* args[1] = { &v };
						mono_runtime_invoke(addMethod, listObj, args, nullptr);
					}
				}
			} else if (std::holds_alternative<std::vector<float>>(value)) {
				MonoObject* listObj = mono_field_get_value_object(mono_domain_get(), field, safeObj);
				if (listObj) {
					MonoClass* listClass = mono_object_get_class(listObj);
					MonoMethod* clearMethod = mono_class_get_method_from_name(listClass, "Clear", 0);
					mono_runtime_invoke(clearMethod, listObj, nullptr, nullptr);
					MonoMethod* addMethod = mono_class_get_method_from_name(listClass, "Add", 1);
					auto& list = std::get<std::vector<float>>(value);
					for (float v : list) {
						void* args[1] = { &v };
						mono_runtime_invoke(addMethod, listObj, args, nullptr);
					}
				}
			} else if (std::holds_alternative<std::vector<bool>>(value)) {
				MonoObject* listObj = mono_field_get_value_object(mono_domain_get(), field, safeObj);
				if (listObj) {
					MonoClass* listClass = mono_object_get_class(listObj);
					MonoMethod* clearMethod = mono_class_get_method_from_name(listClass, "Clear", 0);
					mono_runtime_invoke(clearMethod, listObj, nullptr, nullptr);
					MonoMethod* addMethod = mono_class_get_method_from_name(listClass, "Add", 1);
					auto& list = std::get<std::vector<bool>>(value);
					for (bool v : list) {
						int bv = v ? 1 : 0;
						void* args[1] = { &bv };
						mono_runtime_invoke(addMethod, listObj, args, nullptr);
					}
				}
			} else if (std::holds_alternative<std::vector<std::string>>(value)) {
				MonoObject* listObj = mono_field_get_value_object(mono_domain_get(), field, safeObj);
				if (listObj) {
					MonoClass* listClass = mono_object_get_class(listObj);
					MonoMethod* clearMethod = mono_class_get_method_from_name(listClass, "Clear", 0);
					mono_runtime_invoke(clearMethod, listObj, nullptr, nullptr);
					MonoMethod* addMethod = mono_class_get_method_from_name(listClass, "Add", 1);
					auto& list = std::get<std::vector<std::string>>(value);
					for (const auto& v : list) {
						MonoString* ms = mono_string_new(mono_domain_get(), v.c_str());
						void* args[1] = { ms };
						mono_runtime_invoke(addMethod, listObj, args, nullptr);
					}
				}
			} else if (std::holds_alternative<std::vector<Vector3>>(value)) {
				MonoObject* listObj = mono_field_get_value_object(mono_domain_get(), field, safeObj);
				if (listObj) {
					MonoClass* listClass = mono_object_get_class(listObj);
					MonoMethod* clearMethod = mono_class_get_method_from_name(listClass, "Clear", 0);
					mono_runtime_invoke(clearMethod, listObj, nullptr, nullptr);
					MonoMethod* addMethod = mono_class_get_method_from_name(listClass, "Add", 1);
					auto& list = std::get<std::vector<Vector3>>(value);
					for (auto v : list) {
						void* args[1] = { &v };
						mono_runtime_invoke(addMethod, listObj, args, nullptr);
					}
				}
			}
		}

	}

}

size_t Variables::AddGroup(const std::string& _name) {

	/// 同じ名前のグループがあるかチェック
	if (groupKeyMap_.contains(_name)) {
		return groupKeyMap_.at(_name);
	}

	/// 新規グループを作成
	Group group;
	group.name = _name;

	size_t index = groups_.size();
	groups_.push_back(group);
	groupKeyMap_[_name] = index;

	return index;
}

const Variables::Group& Variables::GetGroup(const std::string& _name) const {
	return groups_[groupKeyMap_.at(_name)];
}

bool Variables::HasGroup(const std::string& _name) const {
	return groupKeyMap_.contains(_name);
}

const std::unordered_map<std::string, size_t>& Variables::GetGroupKeyMap() const {
	return groupKeyMap_;
}

const std::vector<Variables::Group>& Variables::GetGroups() const {
	return groups_;
}


void Variables::SetVariable(const std::string& _groupName, const std::string& _varName, const Var& _value) {
	size_t groupIdx = 0;
	if (!HasGroup(_groupName)) {
		groupIdx = AddGroup(_groupName);
	} else {
		groupIdx = groupKeyMap_.at(_groupName);
	}

	groups_[groupIdx].Add(_varName, _value);
}


void ComponentDebug::VariablesDebug(Variables* _variables) {
	if (!_variables) {
		return;
	}

	if (ImGui::Button("export entity")) {
		GameEntity* entity = _variables->GetOwner();
		const std::string& ownerName = entity->GetName();
		const std::string& groupName = entity->GetECSGroup()->GetGroupName();

		_variables->ReloadScriptVariables();

		// エンティティ全体のデータを取得して保存
		nlohmann::json entityJson = EntityJsonConverter::ToJson(entity);
		std::string path = "Assets/Scene/" + groupName + "/" + ownerName + ".entity";

		std::filesystem::path fsPath(path);
		std::filesystem::create_directories(fsPath.parent_path());

		std::ofstream ofs(path);
		if (ofs) {
			ofs << entityJson.dump(4);
			ofs.close();
			Console::Log("Exported entity to: " + path);
		} else {
			Console::LogError("Failed to export entity to: " + path);
		}
	}
}

const Variables::Var& Variables::Group::Get(const std::string& _name) const {
	return variables[keyMap.at(_name)];
}

bool Variables::Group::Has(const std::string& _name) const {
	return keyMap.contains(_name);
}

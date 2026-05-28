#include "ImGuiShowField.h"

/// external
#include <mono/metadata/object.h>
#include <mono/metadata/attrdefs.h>
#include <mono/metadata/appdomain.h> ///< mono_domain_get のため追加
#include <mono/metadata/class.h>     ///< mono_class_vtable 等のため追加
#include <mono/metadata/blob.h>
#include <mono/metadata/loader.h>

/// std
#include <vector>
#include <format>

/// engine
#include "Engine/Core/Utility/Utility.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Variables/Variables.h"
#include "Engine/ECS/EntityComponentSystem/EntityComponentSystem.h"
#include "Engine/Script/MonoScriptEngine.h"

/// editor
#include "ImGuiMath.h"
#include "../Commands/ImGuiCommand/ImGuiCommand.h"
#include "../Commands/ComponentEditCommands/ModifyScriptVariableCommand.h"
#include "../Manager/EditorManager.h"
#include "../Manager/EditCommand.h"

using namespace Editor;

namespace {
std::unordered_map<int, std::unique_ptr<CSGui::ImGuiShowField>> gFieldDrawers;

void RegisterFieldDrawers() {
	/// ここで必要なフィールドドロワーを登録する
	gFieldDrawers[MONO_TYPE_I4] = std::make_unique<CSGui::IntField>();
	gFieldDrawers[MONO_TYPE_R4] = std::make_unique<CSGui::FloatField>();
	gFieldDrawers[MONO_TYPE_R8] = std::make_unique<CSGui::DoubleField>();
	gFieldDrawers[MONO_TYPE_BOOLEAN] = std::make_unique<CSGui::BoolField>();
	gFieldDrawers[MONO_TYPE_STRING] = std::make_unique<CSGui::StringField>();
	gFieldDrawers[MONO_TYPE_VALUETYPE] = std::make_unique<CSGui::StructGui>();

	/// Enum型のドロワーを登録
	gFieldDrawers[MONO_TYPE_ENUM] = std::make_unique<CSGui::EnumField>();

	/// List型のドロワーを登録
	gFieldDrawers[MONO_TYPE_GENERICINST] = std::make_unique<CSGui::ListField>();

	// 他の型も同様に登録
	ONEngine::Console::Log("Field drawers registered.");
}

} /// namespace


void CSGui::ShowField(const std::string& _scriptName, int _type, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	if(gFieldDrawers.empty()) {
		RegisterFieldDrawers();  ///< 初回呼び出し時にフィールドドロワーを登録
	}

	if(gFieldDrawers.find(_type) == gFieldDrawers.end()) {
		ONEngine::Console::Log("[error] Unsupported field type: " + std::to_string(_type));
		return;
	}

	/// Typeごとに登録されたフィールドドロワーを使用して描画
	gFieldDrawers[_type]->Draw(_scriptName, _obj, _field, _name);
}


void CSGui::ShowFieldForVariables(ONEngine::Variables* _vars, const std::string& _groupName, int _type, MonoClassField* _field, const char* _name) {
	if(!_vars) return;

	if(!_vars->HasGroup(_groupName)) {
		_vars->AddGroup(_groupName);
	}

	auto& group = const_cast<ONEngine::Variables::Group&>(_vars->GetGroup(_groupName));

	switch(_type) {
	case MONO_TYPE_I4:
	case MONO_TYPE_ENUM:
	{
		if(!group.Has(_name)) group.Add(_name, 0);
		int value = group.Get<int>(_name);

		if(_type == MONO_TYPE_ENUM) {
			MonoType* fieldType = mono_field_get_type(_field);
			MonoClass* fieldClass = mono_class_from_mono_type(fieldType);

			void* iter = nullptr;
			MonoClassField* enumField;
			std::vector<std::string> names;
			std::vector<int> values;
			int currentIndex = 0;
			int i = 0;

			MonoDomain* domain = mono_domain_get();
			MonoVTable* vtable = mono_class_vtable(domain, fieldClass);

			while((enumField = mono_class_get_fields(fieldClass, &iter)) != nullptr) {
				uint32_t flags = mono_field_get_flags(enumField);
				if(flags & MONO_FIELD_ATTR_STATIC) {
					names.push_back(mono_field_get_name(enumField));
					int val = 0;
					if(vtable) mono_field_static_get_value(vtable, enumField, &val);
					else mono_field_get_value(nullptr, enumField, &val);
					values.push_back(val);
					if(val == value) currentIndex = i;
					i++;
				}
			}

			if(!names.empty()) {
				std::vector<const char*> namePtrs;
				for(const auto& str : names) namePtrs.push_back(str.c_str());
				if(ImGui::Combo(_name, &currentIndex, namePtrs.data(), static_cast<int>(namePtrs.size()))) {
					group.Add(_name, values[currentIndex]);
				}
				break;
			}
		}

		if(ImGui::DragInt(_name, &value)) group.Add(_name, value);
		break;
	}
	case MONO_TYPE_R4:
	{
		if(!group.Has(_name)) group.Add(_name, 0.0f);
		float value = group.Get<float>(_name);
		if(ImGui::DragFloat(_name, &value)) group.Add(_name, value);
		break;
	}
	case MONO_TYPE_BOOLEAN:
	{
		if(!group.Has(_name)) group.Add(_name, false);
		bool value = group.Get<bool>(_name);
		if(ImGui::Checkbox(_name, &value)) group.Add(_name, value);
		break;
	}
	case MONO_TYPE_STRING:
	{
		if(!group.Has(_name)) group.Add(_name, std::string(""));
		std::string value = group.Get<std::string>(_name);
		if(ImGuiInputText(_name, &value)) group.Add(_name, value);
		break;
	}
	case MONO_TYPE_VALUETYPE:
	{
		MonoType* fieldType = mono_field_get_type(_field);
		MonoClass* fieldClass = mono_class_from_mono_type(fieldType);
		const char* className = mono_class_get_name(fieldClass);

		if(mono_class_is_enum(fieldClass)) {
			if(!group.Has(_name)) group.Add(_name, 0);
			int value = group.Get<int>(_name);

			void* iter = nullptr;
			MonoClassField* enumField;
			std::vector<std::string> names;
			std::vector<int> values;
			int currentIndex = 0;
			int i = 0;

			MonoDomain* domain = mono_domain_get();
			MonoVTable* vtable = mono_class_vtable(domain, fieldClass);

			while((enumField = mono_class_get_fields(fieldClass, &iter)) != nullptr) {
				uint32_t flags = mono_field_get_flags(enumField);
				if(flags & MONO_FIELD_ATTR_STATIC) {
					names.push_back(mono_field_get_name(enumField));
					int val = 0;
					if(vtable) mono_field_static_get_value(vtable, enumField, &val);
					else mono_field_get_value(nullptr, enumField, &val);
					values.push_back(val);
					if(val == value) currentIndex = i;
					i++;
				}
			}

			if(!names.empty()) {
				std::vector<const char*> namePtrs;
				for(const auto& str : names) namePtrs.push_back(str.c_str());
				if(ImGui::Combo(_name, &currentIndex, namePtrs.data(), static_cast<int>(namePtrs.size()))) {
					group.Add(_name, values[currentIndex]);
				}
				break;
			}
		}

		if(strcmp(className, "Vector2") == 0) {
			if(!group.Has(_name)) group.Add(_name, ONEngine::Vector2::Zero);
			ONEngine::Vector2 value = group.Get<ONEngine::Vector2>(_name);
			if(ImGui::DragFloat2(_name, &value.x)) group.Add(_name, value);
		} else if(strcmp(className, "Vector3") == 0) {
			if(!group.Has(_name)) group.Add(_name, ONEngine::Vector3::Zero);
			ONEngine::Vector3 value = group.Get<ONEngine::Vector3>(_name);
			if(ImGui::DragFloat3(_name, &value.x)) group.Add(_name, value);
		} else if(strcmp(className, "Vector4") == 0) {
			if(!group.Has(_name)) group.Add(_name, ONEngine::Vector4::Zero);
			ONEngine::Vector4 value = group.Get<ONEngine::Vector4>(_name);
			if(ImGui::DragFloat4(_name, &value.x)) group.Add(_name, value);
		}
		break;
	}
	case MONO_TYPE_GENERICINST:
	{
		ImGui::PushID(_name);

		MonoType* fieldType = mono_field_get_type(_field);
		MonoClass* fieldClass = mono_class_from_mono_type(fieldType);
		if (strcmp(mono_class_get_name(fieldClass), "List`1") != 0) {
			ImGui::PopID();
			break;
		}

		MonoMethod* getItemMethod = mono_class_get_method_from_name(fieldClass, "get_Item", 1);

		// 型引数の取得
		MonoMethodSignature* sig = mono_method_signature(getItemMethod);
		MonoType* elemType = mono_signature_get_return_type(sig);
		int elemTypeId = mono_type_get_type(elemType);

		if (ImGui::CollapsingHeader(_name)) {
			ImGui::Indent();
			if (elemTypeId == MONO_TYPE_I4) {
				if (!group.Has(_name)) group.Add(_name, std::vector<int>());
				auto& list = std::get<std::vector<int>>(const_cast<ONEngine::Variables::Var&>(group.Get(_name)));
				int size = static_cast<int>(list.size());
				if (ImGui::InputInt("Size", &size)) {
					if (size < 0) size = 0;
					list.resize(size);
				}
				for (int i = 0; i < static_cast<int>(list.size()); ++i) {
					ImGui::DragInt(std::format("[{}]", i).c_str(), &list[i]);
				}
			} else if (elemTypeId == MONO_TYPE_R4) {
				if (!group.Has(_name)) group.Add(_name, std::vector<float>());
				auto& list = std::get<std::vector<float>>(const_cast<ONEngine::Variables::Var&>(group.Get(_name)));
				int size = static_cast<int>(list.size());
				if (ImGui::InputInt("Size", &size)) {
					if (size < 0) size = 0;
					list.resize(size);
				}
				for (int i = 0; i < static_cast<int>(list.size()); ++i) {
					ImGui::DragFloat(std::format("[{}]", i).c_str(), &list[i]);
				}
			} else if (elemTypeId == MONO_TYPE_BOOLEAN) {
				if (!group.Has(_name)) group.Add(_name, std::vector<bool>());
				auto& list = std::get<std::vector<bool>>(const_cast<ONEngine::Variables::Var&>(group.Get(_name)));
				int size = static_cast<int>(list.size());
				if (ImGui::InputInt("Size", &size)) {
					if (size < 0) size = 0;
					list.resize(size);
				}
				for (int i = 0; i < static_cast<int>(list.size()); ++i) {
					bool b = list[i];
					if (ImGui::Checkbox(std::format("[{}]", i).c_str(), &b)) list[i] = b;
				}
			} else if (elemTypeId == MONO_TYPE_STRING) {
				if (!group.Has(_name)) group.Add(_name, std::vector<std::string>());
				auto& list = std::get<std::vector<std::string>>(const_cast<ONEngine::Variables::Var&>(group.Get(_name)));
				int size = static_cast<int>(list.size());
				if (ImGui::InputInt("Size", &size)) {
					if (size < 0) size = 0;
					list.resize(size);
				}
				for (int i = 0; i < static_cast<int>(list.size()); ++i) {
					ImGuiInputText(std::format("[{}]", i).c_str(), &list[i]);
				}
			} else if (elemTypeId == MONO_TYPE_VALUETYPE) {
				MonoClass* elemClass = mono_class_from_mono_type(elemType);
				if (strcmp(mono_class_get_name(elemClass), "Vector3") == 0) {
					if (!group.Has(_name)) group.Add(_name, std::vector<ONEngine::Vector3>());
					auto& list = std::get<std::vector<ONEngine::Vector3>>(const_cast<ONEngine::Variables::Var&>(group.Get(_name)));
					int size = static_cast<int>(list.size());
					if (ImGui::InputInt("Size", &size)) {
						if (size < 0) size = 0;
						list.resize(size);
					}
					for (int i = 0; i < static_cast<int>(list.size()); ++i) {
						ImGui::DragFloat3(std::format("[{}]", i).c_str(), &list[i].x);
					}
				}
			}
			ImGui::Unindent();
		}
		ImGui::PopID();
		break;
	}
	}
}



void CSGui::IntField::Draw(const std::string& _scriptName, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	int value = 0;
	mono_field_get_value(_obj, _field, &value);
	
	static int startValue = 0;
	if (ImGui::IsItemActivated()) {
		startValue = value;
		ONEngine::Console::Log(std::format("[UndoDebug] Int field '{}' activated. Start value: {}", _name, startValue));
	}

	if(ImGui::DragInt(_name, &value)) {
		mono_field_set_value(_obj, _field, &value);
	}

	if (ImGui::IsItemDeactivatedAfterEdit()) {
		ONEngine::Console::Log(std::format("[UndoDebug] Int field '{}' deactivated after edit. End value: {}", _name, value));
		ONEngine::GameEntity* entity = ONEngine::MonoScriptEngine::GetInstance().GetOwnerEntity(_obj);
		if (entity) {
			if (startValue != value) {
				ONEngine::Console::Log(std::format("[UndoDebug] Requesting ModifyScriptVariableCommand for '{}' on entity '{}'", _name, entity->GetName()));
				EditCommand::Execute<ModifyScriptVariableCommand>(entity, _scriptName, _name, MONO_TYPE_I4, startValue, value);
			} else {
				ONEngine::Console::Log("[UndoDebug] Value didn't change, skipping command.");
			}
		} else {
			ONEngine::Console::LogError("[UndoDebug] FAILED: Could not find owner entity for MonoObject.");
		}
	}
}

void CSGui::FloatField::Draw(const std::string& _scriptName, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	float value = 0.0f;
	mono_field_get_value(_obj, _field, &value);
	
	static float startValue = 0.0f;
	if (ImGui::IsItemActivated()) {
		startValue = value;
		ONEngine::Console::Log(std::format("[UndoDebug] Float field '{}' activated. Start value: {}", _name, startValue));
	}

	if(ImGui::DragFloat(_name, &value)) {
		mono_field_set_value(_obj, _field, &value);
	}

	if (ImGui::IsItemDeactivatedAfterEdit()) {
		ONEngine::Console::Log(std::format("[UndoDebug] Float field '{}' deactivated after edit. End value: {}", _name, value));
		ONEngine::GameEntity* entity = ONEngine::MonoScriptEngine::GetInstance().GetOwnerEntity(_obj);
		if (entity) {
			if (startValue != value) {
				ONEngine::Console::Log(std::format("[UndoDebug] Requesting ModifyScriptVariableCommand for '{}' on entity '{}'", _name, entity->GetName()));
				EditCommand::Execute<ModifyScriptVariableCommand>(entity, _scriptName, _name, MONO_TYPE_R4, startValue, value);
			} else {
				ONEngine::Console::Log("[UndoDebug] Value didn't change, skipping command.");
			}
		} else {
			ONEngine::Console::LogError("[UndoDebug] FAILED: Could not find owner entity for MonoObject.");
		}
	}
}


void CSGui::DoubleField::Draw(const std::string& _scriptName, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	double value = 0.0;
	mono_field_get_value(_obj, _field, &value);
	double oldValue = value;

	/// ImGuiはfloatしかサポートしていないので、floatにキャストして表示
	float floatValue = static_cast<float>(value);
	if(ImGui::DragFloat(_name, &floatValue)) {
		value = static_cast<double>(floatValue);
		ONEngine::GameEntity* entity = ONEngine::MonoScriptEngine::GetInstance().GetOwnerEntity(_obj);
		if (entity) {
			EditCommand::Execute<ModifyScriptVariableCommand>(entity, _scriptName, _name, MONO_TYPE_R8, oldValue, value);
		} else {
			mono_field_set_value(_obj, _field, &value);
		}
	}
}


void CSGui::BoolField::Draw(const std::string& _scriptName, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	bool value = false;
	mono_field_get_value(_obj, _field, &value);
	bool oldValue = value;
	if(ImGui::Checkbox(_name, &value)) {
		ONEngine::GameEntity* entity = ONEngine::MonoScriptEngine::GetInstance().GetOwnerEntity(_obj);
		if (entity) {
			EditCommand::Execute<ModifyScriptVariableCommand>(entity, _scriptName, _name, MONO_TYPE_BOOLEAN, oldValue, value);
		} else {
			mono_field_set_value(_obj, _field, &value);
		}
	}
}

void CSGui::StringField::Draw(const std::string& _scriptName, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	MonoString* monoStr = (MonoString*)mono_field_get_value_object(mono_domain_get(), _field, _obj);
	if(!monoStr) {
		return;
	}

	char* utf8 = mono_string_to_utf8(monoStr);
	std::string oldValue = utf8;
	std::string value = utf8;
	mono_free(utf8);

	if(ImGuiInputText(_name, &value, ImGuiInputTextFlags_EnterReturnsTrue)) {
		ONEngine::GameEntity* entity = ONEngine::MonoScriptEngine::GetInstance().GetOwnerEntity(_obj);
		if (entity) {
			EditCommand::Execute<ModifyScriptVariableCommand>(entity, _scriptName, _name, MONO_TYPE_STRING, oldValue, value);
		} else {
			MonoString* newStr = mono_string_new(mono_domain_get(), value.c_str());
			mono_field_set_value(_obj, _field, newStr);
		}
	}
}


void CSGui::ListField::Draw(const std::string& _scriptName, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	MonoDomain* domain = mono_domain_get();
	MonoObject* listObj = mono_field_get_value_object(domain, _field, _obj);
	if(!listObj) {
		ImGui::Text("%s: (null)", _name);
		return;
	}

	ImGui::PushID(_field);

	MonoClass* listClass = mono_object_get_class(listObj);
	MonoMethod* getCountMethod = mono_class_get_method_from_name(listClass, "get_Count", 0);
	MonoObject* countObj = mono_runtime_invoke(getCountMethod, listObj, nullptr, nullptr);
	int count = *(int*)mono_object_unbox(countObj);

	if(ImGui::CollapsingHeader(_name)) {
		ImGui::Indent();

		MonoMethod* getItemMethod = mono_class_get_method_from_name(listClass, "get_Item", 1);
		MonoMethod* setItemMethod = mono_class_get_method_from_name(listClass, "set_Item", 2);
		MonoMethod* addMethod = mono_class_get_method_from_name(listClass, "Add", 1);
		MonoMethod* removeAtMethod = mono_class_get_method_from_name(listClass, "RemoveAt", 1);

		// 型引数の取得
		MonoMethodSignature* sig = mono_method_signature(getItemMethod);
		MonoType* elemType = mono_signature_get_return_type(sig);
		int elemTypeId = mono_type_get_type(elemType);

		// Size操作
		int size = count;
		if(ImGui::InputInt("Size", &size)) {
			if(size < 0) size = 0;
			if(size > count) {
				for(int i = 0; i < size - count; ++i) {
					// Add default value
					if(elemTypeId == MONO_TYPE_I4) { int v = 0; void* args[1] = { &v }; mono_runtime_invoke(addMethod, listObj, args, nullptr); }
					else if(elemTypeId == MONO_TYPE_R4) { float v = 0.0f; void* args[1] = { &v }; mono_runtime_invoke(addMethod, listObj, args, nullptr); }
					else if(elemTypeId == MONO_TYPE_BOOLEAN) { bool v = false; void* args[1] = { &v }; mono_runtime_invoke(addMethod, listObj, args, nullptr); }
					else if(elemTypeId == MONO_TYPE_STRING) { MonoString* v = mono_string_new(domain, ""); void* args[1] = { v }; mono_runtime_invoke(addMethod, listObj, args, nullptr); }
					else if(elemTypeId == MONO_TYPE_VALUETYPE) { 
						MonoClass* elemClass = mono_class_from_mono_type(elemType);
						if(strcmp(mono_class_get_name(elemClass), "Vector3") == 0) { ONEngine::Vector3 v = ONEngine::Vector3::Zero; void* args[1] = { &v }; mono_runtime_invoke(addMethod, listObj, args, nullptr); }
					}
				}
			} else if(size < count) {
				for(int i = 0; i < count - size; ++i) {
					int idx = size;
					void* args[1] = { &idx };
					mono_runtime_invoke(removeAtMethod, listObj, args, nullptr);
				}
			}
			count = size;
		}

		for(int i = 0; i < count; ++i) {
			void* getArgs[1] = { &i };
			MonoObject* itemObj = mono_runtime_invoke(getItemMethod, listObj, getArgs, nullptr);
			std::string itemName = std::format("[{}]", i);

			if(elemTypeId == MONO_TYPE_I4) {
				int v = *(int*)mono_object_unbox(itemObj);
				if(ImGui::DragInt(itemName.c_str(), &v)) { void* setArgs[2] = { &i, &v }; mono_runtime_invoke(setItemMethod, listObj, setArgs, nullptr); }
			} else if(elemTypeId == MONO_TYPE_R4) {
				float v = *(float*)mono_object_unbox(itemObj);
				if(ImGui::DragFloat(itemName.c_str(), &v)) { void* setArgs[2] = { &i, &v }; mono_runtime_invoke(setItemMethod, listObj, setArgs, nullptr); }
			} else if(elemTypeId == MONO_TYPE_BOOLEAN) {
				bool v = *(bool*)mono_object_unbox(itemObj);
				if(ImGui::Checkbox(itemName.c_str(), &v)) { void* setArgs[2] = { &i, &v }; mono_runtime_invoke(setItemMethod, listObj, setArgs, nullptr); }
			} else if(elemTypeId == MONO_TYPE_STRING) {
				char* utf8 = mono_string_to_utf8((MonoString*)itemObj);
				std::string v = utf8;
				mono_free(utf8);
				if(ImGuiInputText(itemName.c_str(), &v)) { MonoString* ms = mono_string_new(domain, v.c_str()); void* setArgs[2] = { &i, ms }; mono_runtime_invoke(setItemMethod, listObj, setArgs, nullptr); }
			} else if(elemTypeId == MONO_TYPE_VALUETYPE) {
				MonoClass* elemClass = mono_class_from_mono_type(elemType);
				if(strcmp(mono_class_get_name(elemClass), "Vector3") == 0) {
					ONEngine::Vector3 v = *(ONEngine::Vector3*)mono_object_unbox(itemObj);
					if(ImGui::DragFloat3(itemName.c_str(), &v.x)) { void* setArgs[2] = { &i, &v }; mono_runtime_invoke(setItemMethod, listObj, setArgs, nullptr); }
				}
			}
		}

		ImGui::Unindent();
	}

	ImGui::PopID();
}


void CSGui::EnumField::Draw(const std::string& _scriptName, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	MonoType* fieldType = mono_field_get_type(_field);
	MonoClass* fieldClass = mono_class_from_mono_type(fieldType);

	/// 現在の値を取得
	int currentValue = 0;
	mono_field_get_value(_obj, _field, &currentValue);
	int oldValue = currentValue;

	void* iter = nullptr;
	MonoClassField* enumField;

	std::vector<std::string> names;
	std::vector<int> values;

	int currentIndex = 0;
	int i = 0;

	/// 【修正ポイント】Enumのvtableを取得する（Staticフィールドへの安全なアクセスのために必須）
	MonoDomain* domain = mono_object_get_domain(_obj);
	if(!domain) {
		domain = mono_domain_get();
	}
	MonoVTable* vtable = mono_class_vtable(domain, fieldClass);

	/// Enumのクラスから全てのフィールド（定数）を取得する
	while((enumField = mono_class_get_fields(fieldClass, &iter)) != nullptr) {
		uint32_t flags = mono_field_get_flags(enumField);

		/// Enumの要素（定数）は必ず static フラグを持っているため、それで判定
		if(flags & MONO_FIELD_ATTR_STATIC) {
			names.push_back(mono_field_get_name(enumField));

			int val = 0;
			if(vtable) {
				/// vtableを使って安全に静的(定数)フィールドの値を読み取る
				mono_field_static_get_value(vtable, enumField, &val);
			} else {
				/// 万が一vtableが取得できなかった場合のフォールバック
				mono_field_get_value(nullptr, enumField, &val);
			}
			values.push_back(val);

			if(val == currentValue) {
				currentIndex = i;
			}
			i++;
		}
	}

	/// もしEnumの中身が空だった場合のクラッシュ防止
	if(names.empty()) {
		return;
	}

	/// ImGui::Combo に渡すため const char* の配列を準備
	std::vector<const char*> namePtrs;
	namePtrs.reserve(names.size());
	for(const auto& str : names) {
		namePtrs.push_back(str.c_str());
	}

	/// プルダウンメニューを表示
	if(ImGui::Combo(_name, &currentIndex, namePtrs.data(), static_cast<int>(namePtrs.size()))) {
		/// 編集された場合、インデックスに対応する値をセットする
		int newValue = values[currentIndex];
		ONEngine::GameEntity* entity = ONEngine::MonoScriptEngine::GetInstance().GetOwnerEntity(_obj);
		if (entity) {
			EditCommand::Execute<ModifyScriptVariableCommand>(entity, _scriptName, _name, MONO_TYPE_ENUM, oldValue, newValue);
		} else {
			mono_field_set_value(_obj, _field, &newValue);
		}
	}
}


void CSGui::StructGui::Draw(const std::string& _scriptName, MonoObject* _obj, MonoClassField* _field, [[maybe_unused]] const char* _name) {
	MonoType* fieldType = mono_field_get_type(_field);
	MonoClass* fieldClass = mono_class_from_mono_type(fieldType);

	/// Enumの場合、MONO_TYPE_VALUETYPE として飛んでくることがあるため、安全のために判定
	if(mono_class_is_enum(fieldClass)) {
		static EnumField enumDrawer;
		enumDrawer.Draw(_scriptName, _obj, _field, _name);
		return;
	}

	if(fieldDrawers.empty()) {
		Register();  ///< 初回呼び出し時にフィールドドロワーを登録
	}

	const char* name = mono_class_get_name(fieldClass);

	auto itr = fieldDrawers.find(name);
	if(itr == fieldDrawers.end()) {
		ONEngine::Console::Log("[error] Unsupported struct type: " + std::string(name));
		return;
	}

	/// フィールドドロワーが登録されている場合はそれを使用
	itr->second->Draw(_scriptName, _obj, _field, _name);
}

void CSGui::StructGui::Register() {
	/// フィールドドロワーを登録する
	fieldDrawers["Vector2"] = std::make_unique<Vector2Field>();
	fieldDrawers["Vector3"] = std::make_unique<Vector3Field>();
	fieldDrawers["Vector4"] = std::make_unique<Vector4Field>();
}


void CSGui::Vector2Field::Draw(const std::string& _scriptName, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	ONEngine::Vector2 structData;
	mono_field_get_value(_obj, _field, &structData);
	
	static ONEngine::Vector2 startValue;
	if (ImGui::IsItemActivated()) {
		startValue = structData;
	}

	if(ImGui::DragFloat2(_name, &structData.x)) {
		mono_field_set_value(_obj, _field, &structData);
	}

	if (ImGui::IsItemDeactivatedAfterEdit()) {
		ONEngine::GameEntity* entity = ONEngine::MonoScriptEngine::GetInstance().GetOwnerEntity(_obj);
		if (entity && (startValue.x != structData.x || startValue.y != structData.y)) {
			EditCommand::Execute<ModifyScriptVariableCommand>(entity, _scriptName, _name, MONO_TYPE_VALUETYPE, startValue, structData);
		}
	}
}

void CSGui::Vector3Field::Draw(const std::string& _scriptName, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	ONEngine::Vector3 structData;
	mono_field_get_value(_obj, _field, &structData);
	
	static ONEngine::Vector3 startValue;
	if (ImGui::IsItemActivated()) {
		startValue = structData;
	}

	if(ImGui::DragFloat3(_name, &structData.x)) {
		mono_field_set_value(_obj, _field, &structData);
	}

	if (ImGui::IsItemDeactivatedAfterEdit()) {
		ONEngine::GameEntity* entity = ONEngine::MonoScriptEngine::GetInstance().GetOwnerEntity(_obj);
		if (entity && (startValue.x != structData.x || startValue.y != structData.y || startValue.z != structData.z)) {
			EditCommand::Execute<ModifyScriptVariableCommand>(entity, _scriptName, _name, MONO_TYPE_VALUETYPE, startValue, structData);
		}
	}
}

void CSGui::Vector4Field::Draw(const std::string& _scriptName, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	ONEngine::Vector4 structData;
	mono_field_get_value(_obj, _field, &structData);

	static ONEngine::Vector4 startValue;
	if (ImGui::IsItemActivated()) {
		startValue = structData;
	}

	if(ImGui::DragFloat4(_name, &structData.x)) {
		mono_field_set_value(_obj, _field, &structData);
	}

	if (ImGui::IsItemDeactivatedAfterEdit()) {
		ONEngine::GameEntity* entity = ONEngine::MonoScriptEngine::GetInstance().GetOwnerEntity(_obj);
		if (entity && (startValue.x != structData.x || startValue.y != structData.y || startValue.z != structData.z || startValue.w != structData.z)) {
			EditCommand::Execute<ModifyScriptVariableCommand>(entity, _scriptName, _name, MONO_TYPE_VALUETYPE, startValue, structData);
		}
	}
}
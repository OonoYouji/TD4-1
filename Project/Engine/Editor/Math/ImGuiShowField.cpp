#include "ImGuiShowField.h"

/// external
#include <mono/metadata/object.h>
#include <mono/metadata/attrdefs.h>
#include <mono/metadata/appdomain.h> ///< mono_domain_get のため追加
#include <mono/metadata/class.h>     ///< mono_class_vtable 等のため追加

/// std
#include <vector>

/// engine
#include "Engine/Core/Utility/Utility.h"

/// editor
#include "ImGuiMath.h"

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

	// 他の型も同様に登録
	ONEngine::Console::Log("Field drawers registered.");
}

} /// namespace


void CSGui::ShowFiled(int _type, MonoObject* _obj, MonoClassField* _field, const char* _name) {
	if(gFieldDrawers.empty()) {
		RegisterFieldDrawers();  ///< 初回呼び出し時にフィールドドロワーを登録
	}

	if(gFieldDrawers.find(_type) == gFieldDrawers.end()) {
		ONEngine::Console::Log("[error] Unsupported field type: " + std::to_string(_type));
		return;
	}

	/// Typeごとに登録されたフィールドドロワーを使用して描画
	gFieldDrawers[_type]->Draw(_obj, _field, _name);
}



void CSGui::IntField::Draw(MonoObject* _obj, MonoClassField* _field, const char* _name) {
	int value = 0;
	mono_field_get_value(_obj, _field, &value);
	if(ImGui::DragInt(_name, &value)) {
		/// 編集した値をセットする
		mono_field_set_value(_obj, _field, &value);
	}
}

void CSGui::FloatField::Draw(MonoObject* _obj, MonoClassField* _field, const char* _name) {
	float value = 0.0f;
	mono_field_get_value(_obj, _field, &value);
	if(ImGui::DragFloat(_name, &value)) {
		/// 編集した値をセットする
		mono_field_set_value(_obj, _field, &value);
	}
}


void CSGui::DoubleField::Draw(MonoObject* _obj, MonoClassField* _field, const char* _name) {
	double value = 0.0;
	mono_field_get_value(_obj, _field, &value);

	/// ImGuiはfloatしかサポートしていないので、floatにキャストして表示
	float floatValue = static_cast<float>(value);
	if(ImGui::DragFloat(_name, &floatValue)) {
		value = static_cast<double>(floatValue);
		/// 編集した値をセットする
		mono_field_set_value(_obj, _field, &value);
	}
}


void CSGui::BoolField::Draw(MonoObject* _obj, MonoClassField* _field, const char* _name) {
	bool value = false;
	mono_field_get_value(_obj, _field, &value);
	if(ImGui::Checkbox(_name, &value)) {
		/// 編集した値をセットする
		mono_field_set_value(_obj, _field, &value);
	}
}

void CSGui::StringField::Draw(MonoObject* _obj, MonoClassField* _field, const char* _name) {
	MonoString* monoStr = (MonoString*)mono_field_get_value_object(mono_domain_get(), _field, _obj);
	if(!monoStr) {
		return;
	}

	char* utf8 = mono_string_to_utf8(monoStr);
	std::string value = utf8;
	mono_free(utf8);

	if(ImGuiInputText(_name, &value, ImGuiInputTextFlags_EnterReturnsTrue)) {
		MonoString* newStr = mono_string_new(mono_domain_get(), value.c_str());
		mono_field_set_value(_obj, _field, newStr);
	}
}


void CSGui::EnumField::Draw(MonoObject* _obj, MonoClassField* _field, const char* _name) {
	MonoType* fieldType = mono_field_get_type(_field);
	MonoClass* fieldClass = mono_class_from_mono_type(fieldType);

	/// 現在の値を取得
	int currentValue = 0;
	mono_field_get_value(_obj, _field, &currentValue);

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
		mono_field_set_value(_obj, _field, &newValue);
	}
}


void CSGui::StructGui::Draw(MonoObject* _obj, MonoClassField* _field, [[maybe_unused]] const char* _name) {
	MonoType* fieldType = mono_field_get_type(_field);
	MonoClass* fieldClass = mono_class_from_mono_type(fieldType);

	/// Enumの場合、MONO_TYPE_VALUETYPE として飛んでくることがあるため、安全のために判定
	if(mono_class_is_enum(fieldClass)) {
		static EnumField enumDrawer;
		enumDrawer.Draw(_obj, _field, _name);
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
	itr->second->Draw(_obj, _field, _name);
}

void CSGui::StructGui::Register() {
	/// フィールドドロワーを登録する
	fieldDrawers["Vector2"] = std::make_unique<Vector2Field>();
	fieldDrawers["Vector3"] = std::make_unique<Vector3Field>();
	fieldDrawers["Vector4"] = std::make_unique<Vector4Field>();
}


void CSGui::Vector2Field::Draw(MonoObject* _obj, MonoClassField* _field, const char* _name) {
	ONEngine::Vector2 structData;
	mono_field_get_value(_obj, _field, &structData);

	if(ImGui::DragFloat2(_name, &structData.x)) {
		/// 編集した値をセットする
		mono_field_set_value(_obj, _field, &structData);
	}
}

void CSGui::Vector3Field::Draw(MonoObject* _obj, MonoClassField* _field, const char* _name) {
	ONEngine::Vector3 structData;
	mono_field_get_value(_obj, _field, &structData);

	if(ImGui::DragFloat3(_name, &structData.x)) {
		/// 編集した値をセットする
		mono_field_set_value(_obj, _field, &structData);
	}
}

void CSGui::Vector4Field::Draw(MonoObject* _obj, MonoClassField* _field, const char* _name) {
	ONEngine::Vector4 structData;
	mono_field_get_value(_obj, _field, &structData);

	if(ImGui::DragFloat4(_name, &structData.x)) {
		/// 編集した値をセットする
		mono_field_set_value(_obj, _field, &structData);
	}
}
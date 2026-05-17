#include "BehaviorTreeEditorWindow.h"
#include <imgui.h>
#include "Engine/Script/MonoScriptEngine.h"
#include "Engine/ECS/EntityComponentSystem/EntityComponentSystem.h"
#include "Engine/Core/Utility/Time/Time.h"
#include <algorithm>
#include <nlohmann/json.hpp>
#include <fstream>
#include <filesystem>

using json = nlohmann::json;

namespace Editor {

BehaviorTreeEditorWindow::BehaviorTreeEditorWindow(ONEngine::EntityComponentSystem* ecs)
    : pEcs_(ecs) {
    ed::Config config;
    config.SettingsFile = "BehaviorTreeEditor_UI.json";
    m_Editor = ed::CreateEditor(&config);

    availableNodeClasses_ = ONEngine::MonoScriptEngine::GetInstance().GetBehaviorNodeClasses();
}

BehaviorTreeEditorWindow::~BehaviorTreeEditorWindow() {
    if (m_Editor) {
        ed::DestroyEditor(m_Editor);
    }
}

void BehaviorTreeEditorWindow::ShowImGui() {
    if (!ImGui::Begin("Behavior Tree Editor", nullptr)) {
        ImGui::End();
        return;
    }

    // ツールバー
    if (ImGui::Button("Save")) {
        SaveTree(m_CurrentFilePath);
    }
    ImGui::SameLine();
    if (ImGui::Button("Load")) {
        LoadTree(m_CurrentFilePath);
    }
    ImGui::SameLine();
    ImGui::PushItemWidth(300);
    char pathBuf[256];
    strncpy_s(pathBuf, m_CurrentFilePath.c_str(), sizeof(pathBuf));
    if (ImGui::InputText("File Path", pathBuf, sizeof(pathBuf))) {
        m_CurrentFilePath = pathBuf;
    }
    ImGui::PopItemWidth();

    ImGui::SameLine();
    if (ImGui::Button("Refresh Nodes")) {
        availableNodeClasses_ = ONEngine::MonoScriptEngine::GetInstance().GetBehaviorNodeClasses();
    }
    ImGui::SameLine();
    ImGui::Text("Nodes: %d", (int)availableNodeClasses_.size());

    ImGui::Separator();

    // 左右の分割
    if (ImGui::BeginChild("LeftPanel", ImVec2(250, 0), true)) {
        DrawBlackboardEditor();
    }
    ImGui::EndChild();
    ImGui::SameLine();

    DrawGraphEditor();

    ImGui::End();
}

void BehaviorTreeEditorWindow::DrawBlackboardEditor() {
    ImGui::Text("Blackboard Variables");
    ImGui::Separator();

    if (ImGui::Button("Add Variable")) {
        m_BBVariables.push_back({ "NewVar", BBVarType::Float });
    }

    ImGui::BeginChild("VariablesList");
    for (size_t i = 0; i < m_BBVariables.size(); ++i) {
        auto& var = m_BBVariables[i];
        ImGui::PushID(static_cast<int>(i));

        if (ImGui::Button("X")) {
            m_BBVariables.erase(m_BBVariables.begin() + i);
            ImGui::PopID();
            break;
        }
        ImGui::SameLine();

        char keyBuf[64];
        strncpy_s(keyBuf, var.key.c_str(), sizeof(keyBuf));
        ImGui::PushItemWidth(100);
        if (ImGui::InputText("##Key", keyBuf, sizeof(keyBuf))) {
            var.key = keyBuf;
        }
        ImGui::PopItemWidth();
        ImGui::SameLine();

        const char* typeNames[] = { "Int", "Float", "Bool", "Vec3" };
        int typeIdx = static_cast<int>(var.type);
        ImGui::PushItemWidth(70);
        if (ImGui::Combo("##Type", &typeIdx, typeNames, IM_ARRAYSIZE(typeNames))) {
            var.type = static_cast<BBVarType>(typeIdx);
        }
        ImGui::PopItemWidth();

        // 値の編集
        ImGui::Indent(20.0f);
        switch (var.type) {
        case BBVarType::Int:    ImGui::InputInt("Value", &var.iVal); break;
        case BBVarType::Float:  ImGui::InputFloat("Value", &var.fVal); break;
        case BBVarType::Bool:   ImGui::Checkbox("Value", &var.bVal); break;
        case BBVarType::Vector3: ImGui::DragFloat3("Value", var.vVal, 0.1f); break;
        }
        ImGui::Unindent(20.0f);

        ImGui::Separator();
        ImGui::PopID();
    }
    ImGui::EndChild();
}

void BehaviorTreeEditorWindow::DrawGraphEditor() {
    ed::SetCurrentEditor(m_Editor);
    ed::Begin("BT_Graph_Editor", ImVec2(0, 0));

    // Entryノードがなければ作成
    bool hasEntry = false;
    for (const auto& n : m_Nodes) {
        if (n.className == "Entry") {
            hasEntry = true;
            break;
        }
    }
    if (!hasEntry) {
        Node* entry = CreateNode("Entry");
        entry->name = "ROOT ENTRY";
        ed::SetNodePosition(entry->id, ImVec2(50, 250));
    }

    // 1. ノードの描画
    for (auto& node : m_Nodes) {
        ed::BeginNode(node.id);
        
        ImGui::PushID(node.id.AsPointer());
        
        ImGui::BeginGroup();
        ImGui::TextColored(ImVec4(1.0f, 0.8f, 0.2f, 1.0f), "== %s ==", node.name.c_str());
        if (node.className != "Entry") {
            ImGui::TextDisabled("[%s]", node.className.c_str());
        }
        ImGui::EndGroup();
        
        ImGui::Spacing();

        ImGui::BeginGroup();
        
        // Input Pins
        ImGui::BeginGroup();
        for (auto& pin : node.inputs) {
            ed::BeginPin(pin.id, ed::PinKind::Input);
            ImGui::TextColored(ImVec4(0.4f, 1.0f, 0.4f, 1.0f), ">"); 
            ed::EndPin();
            ImGui::SameLine();
            ImGui::TextUnformatted(pin.name.c_str());
        }
        ImGui::EndGroup();

        ImGui::SameLine(100.0f);

        // Output Pins
        ImGui::BeginGroup();
        for (auto& pin : node.outputs) {
            ImGui::TextUnformatted(pin.name.c_str());
            ImGui::SameLine();
            ed::BeginPin(pin.id, ed::PinKind::Output);
            ImGui::TextColored(ImVec4(0.4f, 1.0f, 0.4f, 1.0f), ">");
            ed::EndPin();
        }
        ImGui::EndGroup();

        ImGui::EndGroup();
        
        ImGui::PopID();
        ed::EndNode();
    }

    // 2. リンクの描画
    for (auto& link : m_Links) {
        ed::Link(link.id, link.startPinId, link.endPinId, link.color, 2.0f);
    }

    // 3. インタラクション処理
    if (ed::BeginCreate()) {
        ed::PinId inputPinId, outputPinId;
        if (ed::QueryNewLink(&inputPinId, &outputPinId)) {
            if (inputPinId && outputPinId) {
                if (ed::AcceptNewItem()) {
                    CreateLink(inputPinId, outputPinId);
                }
            }
        }
    }
    ed::EndCreate();

    if (ed::BeginDelete()) {
        ed::LinkId linkId;
        while (ed::QueryDeletedLink(&linkId)) {
            if (ed::AcceptDeletedItem()) {
                auto it = std::remove_if(m_Links.begin(), m_Links.end(), 
                    [linkId](const Link& l) { return l.id == linkId; });
                m_Links.erase(it, m_Links.end());
            }
        }

        ed::NodeId nodeId;
        while (ed::QueryDeletedNode(&nodeId)) {
            auto it = std::find_if(m_Nodes.begin(), m_Nodes.end(), 
                [nodeId](const Node& n) { return n.id == nodeId; });
            
            if (it != m_Nodes.end()) {
                if (it->className == "Entry") {
                    ed::RejectDeletedItem();
                } else if (ed::AcceptDeletedItem()) {
                    m_Nodes.erase(it);
                }
            }
        }
    }
    ed::EndDelete();

    ed::Suspend();
    if (ed::ShowBackgroundContextMenu()) {
        m_ContextNodePos = ed::ScreenToCanvas(ImGui::GetMousePos());
        ImGui::OpenPopup("Create New Node");
    }
    
    if (ImGui::BeginPopup("Create New Node")) {
        for (const auto& className : availableNodeClasses_) {
            if (ImGui::MenuItem(className.c_str())) {
                Node* node = CreateNode(className);
                ed::SetNodePosition(node->id, m_ContextNodePos);
            }
        }
        ImGui::EndPopup();
    }
    ed::Resume();

    ed::End();
    ed::SetCurrentEditor(nullptr);
}

BehaviorTreeEditorWindow::Node* BehaviorTreeEditorWindow::CreateNode(const std::string& className) {
    m_Nodes.emplace_back(GetNextId(), className);
    Node* node = &m_Nodes.back();
    node->className = className;

    if (className == "Entry") {
        node->color = ImColor(255, 128, 0);
    } else if (className.find("Sequence") != std::string::npos || className.find("Selector") != std::string::npos) {
        node->color = ImColor(60, 60, 60);
    } else {
        node->color = ImColor(100, 40, 100);
    }

    if (className != "Entry") {
        node->inputs.emplace_back(GetNextId(), "In", PinKind::Input);
        node->inputs.back().node = node;
    }

    if (className.find("Sequence") != std::string::npos || 
        className.find("Selector") != std::string::npos || 
        className == "Entry") {
        node->outputs.emplace_back(GetNextId(), "Out", PinKind::Output);
        node->outputs.back().node = node;
    }

    return node;
}

void BehaviorTreeEditorWindow::CreateLink(ed::PinId startPin, ed::PinId endPin) {
    m_Links.emplace_back(GetNextId(), startPin, endPin);
}

void BehaviorTreeEditorWindow::SaveTree(const std::string& path) {
    ed::SetCurrentEditor(m_Editor);
    json data;

    // Blackboardの保存
    data["blackboard"] = json::array();
    for (const auto& var : m_BBVariables) {
        json v;
        v["key"] = var.key;
        v["type"] = static_cast<int>(var.type);
        v["iVal"] = var.iVal;
        v["fVal"] = var.fVal;
        v["bVal"] = var.bVal;
        v["vVal"] = { var.vVal[0], var.vVal[1], var.vVal[2] };
        data["blackboard"].push_back(v);
    }

    data["nodes"] = json::array();
    for (const auto& node : m_Nodes) {
        json n;
        n["id"] = (uintptr_t)node.id.AsPointer();
        n["className"] = node.className;
        n["name"] = node.name;
        ImVec2 pos = ed::GetNodePosition(node.id);
        n["pos"] = { pos.x, pos.y };
        n["inputs"] = json::array();
        for (const auto& pin : node.inputs) {
            n["inputs"].push_back({ {"id", (uintptr_t)pin.id.AsPointer()}, {"name", pin.name} });
        }
        n["outputs"] = json::array();
        for (const auto& pin : node.outputs) {
            n["outputs"].push_back({ {"id", (uintptr_t)pin.id.AsPointer()}, {"name", pin.name} });
        }
        data["nodes"].push_back(n);
    }

    data["links"] = json::array();
    for (const auto& link : m_Links) {
        json l;
        l["id"] = (uintptr_t)link.id.AsPointer();
        l["startPin"] = (uintptr_t)link.startPinId.AsPointer();
        l["endPin"] = (uintptr_t)link.endPinId.AsPointer();
        data["links"].push_back(l);
    }

    std::filesystem::path fsPath(path);
    if (!std::filesystem::exists(fsPath.parent_path())) {
        std::filesystem::create_directories(fsPath.parent_path());
    }

    std::ofstream file(path);
    if (file.is_open()) {
        file << data.dump(4);
        ONEngine::Console::Log("BehaviorTreeEditor: Saved tree with blackboard to " + path);
    } else {
        ONEngine::Console::LogError("BehaviorTreeEditor: Failed to save tree to " + path);
    }
    ed::SetCurrentEditor(nullptr);
}

void BehaviorTreeEditorWindow::LoadTree(const std::string& path) {
    std::ifstream file(path);
    if (!file.is_open()) return;

    ed::SetCurrentEditor(m_Editor);
    json data;
    try { file >> data; } catch (...) { 
        ed::SetCurrentEditor(nullptr);
        return; 
    }

    m_Nodes.clear();
    m_Links.clear();
    m_BBVariables.clear();
    m_NextId = 1;

    // Blackboardの復元
    if (data.contains("blackboard")) {
        for (const auto& v : data["blackboard"]) {
            BBVariable var;
            var.key = v["key"];
            var.type = static_cast<BBVarType>(v["type"].get<int>());
            var.iVal = v["iVal"];
            var.fVal = v["fVal"];
            var.bVal = v["bVal"];
            if (v.contains("vVal")) {
                var.vVal[0] = v["vVal"][0];
                var.vVal[1] = v["vVal"][1];
                var.vVal[2] = v["vVal"][2];
            }
            m_BBVariables.push_back(var);
        }
    }

    std::map<uintptr_t, ed::PinId> pinIdMap;

    for (const auto& n : data["nodes"]) {
        std::string className = n["className"];
        Node* node = CreateNode(className);
        node->name = n["name"];
        
        if (n.contains("inputs") && n["inputs"].size() == node->inputs.size()) {
            for (size_t i = 0; i < node->inputs.size(); ++i) {
                pinIdMap[(uintptr_t)n["inputs"][i]["id"]] = node->inputs[i].id;
            }
        }
        if (n.contains("outputs") && n["outputs"].size() == node->outputs.size()) {
            for (size_t i = 0; i < node->outputs.size(); ++i) {
                pinIdMap[(uintptr_t)n["outputs"][i]["id"]] = node->outputs[i].id;
            }
        }
        ed::SetNodePosition(node->id, ImVec2(n["pos"][0], n["pos"][1]));
    }

    for (const auto& l : data["links"]) {
        uintptr_t startId = l["startPin"];
        uintptr_t endId = l["endPin"];
        if (pinIdMap.count(startId) && pinIdMap.count(endId)) {
            CreateLink(pinIdMap[startId], pinIdMap[endId]);
        }
    }
    ONEngine::Console::Log("BehaviorTreeEditor: Loaded tree with blackboard from " + path);
    ed::SetCurrentEditor(nullptr);
}

}

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

BehaviorTreeEditorWindow* BehaviorTreeEditorWindow::s_Instance = nullptr;

BehaviorTreeEditorWindow::BehaviorTreeEditorWindow(ONEngine::EntityComponentSystem* ecs)
    : pEcs_(ecs) {
    s_Instance = this;
    ed::Config config;
    config.SettingsFile = "BehaviorTreeEditor_UI.json";
    m_Editor = ed::CreateEditor(&config);

    InitializeEditor();
}

BehaviorTreeEditorWindow::~BehaviorTreeEditorWindow() {
    if (s_Instance == this) s_Instance = nullptr;
    if (m_Editor) {
        ed::DestroyEditor(m_Editor);
    }
}

void BehaviorTreeEditorWindow::InitializeEditor() {
    availableNodeClasses_ = ONEngine::MonoScriptEngine::GetInstance().GetBehaviorNodeClasses();
    auto moduleClasses = ONEngine::MonoScriptEngine::GetInstance().GetBehaviorModuleClasses();
    availableModuleClasses_.clear();
    for (const auto& m : moduleClasses) {
        availableModuleClasses_.push_back({ m.fullName, !m.isDecorator }); // isDecorator=true means Decorator, else Service
    }
}

void BehaviorTreeEditorWindow::ShowImGui() {
    s_Instance = this;

    if (!ONEngine::DebugConfig::isDebugging) {
        m_RuntimeNodeStatuses.clear();
    }

    if (!ImGui::Begin("Behavior Tree Editor", nullptr)) {
        ImGui::End();
        return;
    }

    if (ImGui::Button("Save")) SaveTree(m_CurrentFilePath);
    ImGui::SameLine();
    if (ImGui::Button("Load")) LoadTree(m_CurrentFilePath);
    ImGui::SameLine();
    if (ImGui::Button("Refresh")) InitializeEditor();
    ImGui::SameLine();
    ImGui::Text("Nodes:%d Modules:%d", (int)availableNodeClasses_.size(), (int)availableModuleClasses_.size());

    ImGui::PushItemWidth(300);
    char pathBuf[256];
    strncpy_s(pathBuf, m_CurrentFilePath.c_str(), sizeof(pathBuf));
    if (ImGui::InputText("File Path", pathBuf, sizeof(pathBuf))) m_CurrentFilePath = pathBuf;
    ImGui::PopItemWidth();

    ImGui::Separator();

    float totalWidth = ImGui::GetContentRegionAvail().x;
    if (ImGui::BeginChild("LeftPanel", ImVec2(220, 0), true)) DrawBlackboardEditor();
    ImGui::EndChild();
    ImGui::SameLine();
    if (ImGui::BeginChild("CenterPanel", ImVec2(totalWidth - 440, 0), true)) DrawGraphEditor();
    ImGui::EndChild();
    ImGui::SameLine();
    if (ImGui::BeginChild("RightPanel", ImVec2(220, 0), true)) DrawNodeInspector();
    ImGui::EndChild();

    ImGui::End();
}

void BehaviorTreeEditorWindow::DrawBlackboardEditor() {
    ImGui::Text("Blackboard Variables");
    ImGui::Separator();
    if (ImGui::Button("Add Variable")) m_BBVariables.push_back({ "NewVar", BBVarType::Float });

    ImGui::BeginChild("VariablesList");
    for (size_t i = 0; i < m_BBVariables.size(); ++i) {
        auto& var = m_BBVariables[i];
        ImGui::PushID(static_cast<int>(i));
        if (ImGui::Button("X")) { m_BBVariables.erase(m_BBVariables.begin() + i); ImGui::PopID(); break; }
        ImGui::SameLine();
        char keyBuf[64];
        strncpy_s(keyBuf, var.key.c_str(), sizeof(keyBuf));
        ImGui::PushItemWidth(100);
        if (ImGui::InputText("##Key", keyBuf, sizeof(keyBuf))) var.key = keyBuf;
        ImGui::PopItemWidth();
        ImGui::SameLine();
        const char* typeNames[] = { "Int", "Float", "Bool", "Vec3", "String" };
        int typeIdx = static_cast<int>(var.type);
        ImGui::PushItemWidth(70);
        if (ImGui::Combo("##Type", &typeIdx, typeNames, IM_ARRAYSIZE(typeNames))) var.type = static_cast<BBVarType>(typeIdx);
        ImGui::PopItemWidth();
        ImGui::Indent(20.0f);
        switch (var.type) {
        case BBVarType::Int: ImGui::InputInt("Value", &var.iVal); break;
        case BBVarType::Float: ImGui::InputFloat("Value", &var.fVal); break;
        case BBVarType::Bool: ImGui::Checkbox("Value", &var.bVal); break;
        case BBVarType::Vector3: ImGui::DragFloat3("Value", var.vVal, 0.1f); break;
        case BBVarType::String: ImGui::InputText("Value", var.sVal, sizeof(var.sVal)); break;
        }
        ImGui::Unindent(20.0f);
        ImGui::Separator();
        ImGui::PopID();
    }
    ImGui::EndChild();
}

static int s_SelectedModuleId = -1; // -1: Node selected, >=0: Module index selected
static bool s_SelectedModuleIsService = false;

void BehaviorTreeEditorWindow::DrawNodeInspector() {
    ImGui::Text("Inspector");
    ImGui::Separator();
    Node* selectedNode = nullptr;
    if (m_SelectedNodeId) { for (auto& node : m_Nodes) { if (node.id == m_SelectedNodeId) { selectedNode = &node; break; } } }
    if (!selectedNode) { ImGui::TextDisabled("Select a node."); return; }

    ImGui::TextColored(ImVec4(1,1,0,1), "%s", selectedNode->name.c_str());
    ImGui::TextDisabled("Type: %s", selectedNode->className.c_str());
    ImGui::Separator();

    // ノード名の編集
    char nameBuf[64];
    strncpy_s(nameBuf, selectedNode->name.c_str(), sizeof(nameBuf));
    if (ImGui::InputText("Node Name", nameBuf, sizeof(nameBuf))) selectedNode->name = nameBuf;

    ImGui::Spacing();

    // モジュール（Decorator/Service）の選択UI
    if (ImGui::CollapsingHeader("Attached Modules", ImGuiTreeNodeFlags_DefaultOpen)) {
        // Decorators
        for (int i = 0; i < (int)selectedNode->decorators.size(); ++i) {
            bool isSelected = (s_SelectedModuleId == i && !s_SelectedModuleIsService);
            if (ImGui::Selectable(std::format("[D] {}", selectedNode->decorators[i].name).c_str(), isSelected)) {
                s_SelectedModuleId = i; s_SelectedModuleIsService = false;
            }
        }
        // Services
        for (int i = 0; i < (int)selectedNode->services.size(); ++i) {
            bool isSelected = (s_SelectedModuleId == i && s_SelectedModuleIsService);
            if (ImGui::Selectable(std::format("[S] {}", selectedNode->services[i].name).c_str(), isSelected)) {
                s_SelectedModuleId = i; s_SelectedModuleIsService = true;
            }
        }
    }

    ImGui::Separator();

    // プロパティ編集（選択されているものに応じて）
    std::string targetClassName;
    std::map<std::string, std::string>* targetProps = nullptr;

    if (s_SelectedModuleId == -1) {
        ImGui::Text("Node Properties:");
        targetClassName = selectedNode->className;
        targetProps = &selectedNode->properties;
    } else {
        auto& modules = s_SelectedModuleIsService ? selectedNode->services : selectedNode->decorators;
        if (s_SelectedModuleId < (int)modules.size()) {
            ImGui::Text("%s Properties:", s_SelectedModuleIsService ? "Service" : "Decorator");
            targetClassName = modules[s_SelectedModuleId].className;
            targetProps = &modules[s_SelectedModuleId].properties;
            if (ImGui::Button("Remove Module")) {
                modules.erase(modules.begin() + s_SelectedModuleId);
                s_SelectedModuleId = -1;
                return;
            }
        }
    }

    if (targetProps && !targetClassName.empty() && targetClassName != "Entry") {
        auto fields = ONEngine::MonoScriptEngine::GetInstance().GetClassFields(targetClassName);
        for (const auto& field : fields) {
            ImGui::PushID(field.name.c_str());
            std::string currentVal = (*targetProps)[field.name];
            if (field.isBBKey) {
                std::vector<const char*> bbKeys;
                int currentIdx = -1;
                for (size_t i = 0; i < m_BBVariables.size(); ++i) { bbKeys.push_back(m_BBVariables[i].key.c_str()); if (m_BBVariables[i].key == currentVal) currentIdx = (int)i; }
                if (ImGui::Combo(field.name.c_str(), &currentIdx, bbKeys.data(), (int)bbKeys.size())) (*targetProps)[field.name] = bbKeys[currentIdx];
            } else if (field.typeName == "System.Single" || field.typeName == "float") {
                float f = currentVal.empty() ? 0.0f : std::stof(currentVal);
                if (ImGui::DragFloat(field.name.c_str(), &f, 0.1f)) (*targetProps)[field.name] = std::to_string(f);
            } else if (field.typeName == "System.Int32" || field.typeName == "int") {
                int i = currentVal.empty() ? 0 : std::stoi(currentVal);
                if (ImGui::InputInt(field.name.c_str(), &i)) (*targetProps)[field.name] = std::to_string(i);
            } else if (field.typeName == "System.Boolean" || field.typeName == "bool") {
                bool b = (currentVal == "true");
                if (ImGui::Checkbox(field.name.c_str(), &b)) (*targetProps)[field.name] = b ? "true" : "false";
            } else if (field.typeName == "System.String" || field.typeName == "string") {
                char sBuf[128]; strncpy_s(sBuf, currentVal.c_str(), sizeof(sBuf));
                if (ImGui::InputText(field.name.c_str(), sBuf, sizeof(sBuf))) (*targetProps)[field.name] = sBuf;
            }
            ImGui::PopID();
        }
    }
}

void BehaviorTreeEditorWindow::DrawGraphEditor() {
    ed::SetCurrentEditor(m_Editor);
    ed::Begin("BT_Graph_Editor", ImVec2(0, 0));

    // 選択同期
    if (ed::GetSelectedObjectCount() > 0) {
        ed::NodeId selectedNodes[1];
        if (ed::GetSelectedNodes(selectedNodes, 1) > 0) {
            if (m_SelectedNodeId != selectedNodes[0]) {
                m_SelectedNodeId = selectedNodes[0];
                s_SelectedModuleId = -1; // ノード選択が変わったらモジュール選択をリセット
            }
        }
    } else { m_SelectedNodeId = 0; s_SelectedModuleId = -1; }

    bool hasEntry = false;
    for (const auto& n : m_Nodes) if (n.className == "Entry") { hasEntry = true; break; }
    if (!hasEntry) { Node* entry = CreateNode("Entry"); entry->name = "ROOT ENTRY"; ed::SetNodePosition(entry->id, ImVec2(50, 250)); }

    for (auto& node : m_Nodes) {
        bool isHighlighted = false;
        ImColor highlightColor = ImColor(255, 255, 0, 0);
        uint32_t idHash = (uint32_t)(uintptr_t)node.id.AsPointer();
        if (m_RuntimeNodeStatuses.count(idHash)) {
            isHighlighted = true;
            int status = m_RuntimeNodeStatuses[idHash];
            switch (status) {
            case 0: highlightColor = ImColor(255, 0, 0); break;
            case 1: highlightColor = ImColor(255, 255, 0); break;
            case 2: highlightColor = ImColor(0, 255, 0); break;
            }
        }

        if (isHighlighted) { ed::PushStyleColor(ed::StyleColor_NodeBorder, highlightColor); ed::PushStyleVar(ed::StyleVar_NodeBorderWidth, 4.0f); }

        ed::BeginNode(node.id);
        ImGui::PushID(node.id.AsPointer());
        
        // ヘッダー
        ImVec4 titleColor = node.isDecorator ? ImVec4(0.3f, 0.6f, 1.0f, 1.0f) : ImVec4(1.0f, 0.8f, 0.2f, 1.0f);
        ImGui::TextColored(titleColor, "== %s ==", node.name.c_str());
        
        // 付加モジュールの簡易表示
        for (const auto& d : node.decorators) ImGui::TextColored(ImVec4(0.4f, 0.7f, 1.0f, 1.0f), "[D] %s", d.name.c_str());
        for (const auto& s : node.services) ImGui::TextColored(ImVec4(0.4f, 1.0f, 0.4f, 1.0f), "[S] %s", s.name.c_str());

        ImGui::Spacing();
        ImGui::BeginGroup();
        for (auto& pin : node.inputs) { ed::BeginPin(pin.id, ed::PinKind::Input); ImGui::TextColored(ImVec4(0.4f, 1.0f, 0.4f, 1.0f), ">"); ed::EndPin(); ImGui::SameLine(); ImGui::TextUnformatted(pin.name.c_str()); }
        ImGui::SameLine(100.0f);
        for (auto& pin : node.outputs) { ImGui::TextUnformatted(pin.name.c_str()); ImGui::SameLine(); ed::BeginPin(pin.id, ed::PinKind::Output); ImGui::TextColored(ImVec4(0.4f, 1.0f, 0.4f, 1.0f), ">"); ed::EndPin(); }
        ImGui::EndGroup();
        
        ImGui::PopID();
        ed::EndNode();
        if (isHighlighted) { ed::PopStyleVar(); ed::PopStyleColor(); }
    }
    
    for (auto& link : m_Links) ed::Link(link.id, link.startPinId, link.endPinId, link.color, 2.0f);

    if (ed::BeginCreate()) {
        ed::PinId inputPinId, outputPinId;
        if (ed::QueryNewLink(&inputPinId, &outputPinId)) {
            if (inputPinId && outputPinId) { if (ed::AcceptNewItem()) CreateLink(inputPinId, outputPinId); }
        }
    }
    ed::EndCreate();

    if (ed::BeginDelete()) {
        ed::LinkId linkId;
        while (ed::QueryDeletedLink(&linkId)) if (ed::AcceptDeletedItem()) { auto it = std::remove_if(m_Links.begin(), m_Links.end(), [linkId](const Link& l) { return l.id == linkId; }); m_Links.erase(it, m_Links.end()); }
        ed::NodeId nodeId;
        while (ed::QueryDeletedNode(&nodeId)) {
            auto it = std::find_if(m_Nodes.begin(), m_Nodes.end(), [nodeId](const Node& n) { return n.id == nodeId; });
            if (it != m_Nodes.end()) { if (it->className == "Entry") ed::RejectDeletedItem(); else if (ed::AcceptDeletedItem()) m_Nodes.erase(it); }
        }
    }
    ed::EndDelete();

    // コンテキストメニュー
    ed::Suspend();
    // ノード上の右クリックメニュー
    ed::NodeId contextNodeId;
    if (ed::ShowNodeContextMenu(&contextNodeId)) {
        m_SelectedNodeId = contextNodeId;
        ImGui::OpenPopup("Node Context Menu");
    }

    if (ImGui::BeginPopup("Node Context Menu")) {
        if (ImGui::BeginMenu("Add Decorator")) {
            for (const auto& m : availableModuleClasses_) {
                if (!m.isService && ImGui::MenuItem(m.fullName.c_str())) {
                    for (auto& node : m_Nodes) if (node.id == m_SelectedNodeId) {
                        node.decorators.push_back({ (uint32_t)GetNextId(), m.fullName, m.fullName, {}, false });
                        break;
                    }
                }
            }
            ImGui::EndMenu();
        }
        if (ImGui::BeginMenu("Add Service")) {
            for (const auto& m : availableModuleClasses_) {
                if (m.isService && ImGui::MenuItem(m.fullName.c_str())) {
                    for (auto& node : m_Nodes) if (node.id == m_SelectedNodeId) {
                        node.services.push_back({ (uint32_t)GetNextId(), m.fullName, m.fullName, {}, true });
                        break;
                    }
                }
            }
            ImGui::EndMenu();
        }
        ImGui::EndPopup();
    }

    // キャンバス上の右クリックメニュー
    if (ed::ShowBackgroundContextMenu()) {
        m_ContextNodePos = ed::ScreenToCanvas(ImGui::GetMousePos());
        ImGui::OpenPopup("Create New Node");
    }
    if (ImGui::BeginPopup("Create New Node")) {
        for (const auto& classInfo : availableNodeClasses_) {
            if (ImGui::MenuItem(classInfo.fullName.c_str())) {
                Node* node = CreateNode(classInfo.fullName, classInfo.isDecorator);
                ed::SetNodePosition(node->id, m_ContextNodePos);
            }
        }
        ImGui::EndPopup();
    }
    ed::Resume();

    ed::End();
    ed::SetCurrentEditor(nullptr);
}

BehaviorTreeEditorWindow::Node* BehaviorTreeEditorWindow::CreateNode(const std::string& className, bool isDecorator) {
    m_Nodes.emplace_back(GetNextId(), className);
    Node* node = &m_Nodes.back();
    node->className = className;
    node->isDecorator = isDecorator;
    if (className == "Entry") node->color = ImColor(255, 128, 0);
    else if (className.find("Sequence") != std::string::npos || className.find("Selector") != std::string::npos) node->color = ImColor(60, 60, 60);
    else if (isDecorator) node->color = ImColor(20, 60, 100); 
    else node->color = ImColor(100, 40, 100);
    if (className != "Entry") { node->inputs.emplace_back(GetNextId(), "In", PinKind::Input); node->inputs.back().node = node; }
    if (className.find("Sequence") != std::string::npos || className.find("Selector") != std::string::npos || className == "Entry") {
        node->outputs.emplace_back(GetNextId(), "Out", PinKind::Output); node->outputs.back().node = node;
    }
    return node;
}

void BehaviorTreeEditorWindow::CreateLink(ed::PinId startPin, ed::PinId endPin) { m_Links.emplace_back(GetNextId(), startPin, endPin); }

void BehaviorTreeEditorWindow::UpdateNodeStatus(uint32_t nodeIdHash, int status) { m_RuntimeNodeStatuses[nodeIdHash] = status; }

void BehaviorTreeEditorWindow::SaveTree(const std::string& path) {
    ed::SetCurrentEditor(m_Editor);
    json data;
    data["blackboard"] = json::array();
    for (const auto& var : m_BBVariables) {
        json v; v["key"] = var.key; v["type"] = static_cast<int>(var.type); v["iVal"] = var.iVal; v["fVal"] = var.fVal; v["bVal"] = var.bVal; v["vVal"] = { var.vVal[0], var.vVal[1], var.vVal[2] }; v["sVal"] = var.sVal;
        data["blackboard"].push_back(v);
    }
    data["nodes"] = json::array();
    for (const auto& node : m_Nodes) {
        json n; n["id"] = (uintptr_t)node.id.AsPointer(); n["className"] = node.className; n["name"] = node.name; n["isDecorator"] = node.isDecorator;
        n["properties"] = json::object(); for (const auto& p : node.properties) n["properties"][p.first] = p.second;
        n["decorators"] = json::array();
        for (const auto& d : node.decorators) { json dj; dj["className"] = d.className; dj["name"] = d.name; dj["properties"] = d.properties; n["decorators"].push_back(dj); }
        n["services"] = json::array();
        for (const auto& s : node.services) { json sj; sj["className"] = s.className; sj["name"] = s.name; sj["properties"] = s.properties; n["services"].push_back(sj); }
        ImVec2 pos = ed::GetNodePosition(node.id); n["pos"] = { pos.x, pos.y };
        n["inputs"] = json::array(); for (const auto& pin : node.inputs) n["inputs"].push_back({ {"id", (uintptr_t)pin.id.AsPointer()}, {"name", pin.name} });
        n["outputs"] = json::array(); for (const auto& pin : node.outputs) n["outputs"].push_back({ {"id", (uintptr_t)pin.id.AsPointer()}, {"name", pin.name} });
        data["nodes"].push_back(n);
    }
    data["links"] = json::array();
    for (const auto& link : m_Links) {
        json l; l["id"] = (uintptr_t)link.id.AsPointer(); l["startPin"] = (uintptr_t)link.startPinId.AsPointer(); l["endPin"] = (uintptr_t)link.endPinId.AsPointer();
        data["links"].push_back(l);
    }
    std::filesystem::path fsPath(path); if (!std::filesystem::exists(fsPath.parent_path())) std::filesystem::create_directories(fsPath.parent_path());
    std::ofstream file(path); if (file.is_open()) file << data.dump(4);
    ed::SetCurrentEditor(nullptr);
}

void BehaviorTreeEditorWindow::LoadTree(const std::string& path) {
    std::ifstream file(path); if (!file.is_open()) return;
    ed::SetCurrentEditor(m_Editor);
    json data; try { file >> data; } catch (...) { ed::SetCurrentEditor(nullptr); return; }
    m_Nodes.clear(); m_Links.clear(); m_BBVariables.clear(); m_NextId = 1;
    if (data.contains("blackboard")) {
        for (const auto& v : data["blackboard"]) {
            BBVariable var; var.key = v["key"]; var.type = static_cast<BBVarType>(v["type"].get<int>()); var.iVal = v["iVal"]; var.fVal = v["fVal"]; var.bVal = v["bVal"];
            if (v.contains("vVal")) { var.vVal[0] = v["vVal"][0]; var.vVal[1] = v["vVal"][1]; var.vVal[2] = v["vVal"][2]; }
            if (v.contains("sVal")) strncpy_s(var.sVal, v["sVal"].get<std::string>().c_str(), sizeof(var.sVal));
            m_BBVariables.push_back(var);
        }
    }
    std::map<uintptr_t, ed::PinId> pinIdMap;
    for (const auto& n : data["nodes"]) {
        std::string className = n["className"]; bool isDecorator = n.value("isDecorator", false);
        Node* node = CreateNode(className, isDecorator);
        node->name = n["name"];
        if (n.contains("properties")) { for (auto it = n["properties"].begin(); it != n["properties"].end(); ++it) node->properties[it.key()] = it.value(); }
        if (n.contains("decorators")) {
            for (const auto& d : n["decorators"]) {
                node->decorators.push_back({ (uint32_t)GetNextId(), d["className"], d["name"], d["properties"].get<std::map<std::string, std::string>>(), false });
            }
        }
        if (n.contains("services")) {
            for (const auto& s : n["services"]) {
                node->services.push_back({ (uint32_t)GetNextId(), s["className"], s["name"], s["properties"].get<std::map<std::string, std::string>>(), true });
            }
        }
        if (n.contains("inputs") && n["inputs"].size() == node->inputs.size()) { for (size_t i = 0; i < node->inputs.size(); ++i) pinIdMap[(uintptr_t)n["inputs"][i]["id"]] = node->inputs[i].id; }
        if (n.contains("outputs") && n["outputs"].size() == node->outputs.size()) { for (size_t i = 0; i < node->outputs.size(); ++i) pinIdMap[(uintptr_t)n["outputs"][i]["id"]] = node->outputs[i].id; }
        ed::SetNodePosition(node->id, ImVec2(n["pos"][0], n["pos"][1]));
    }
    for (const auto& l : data["links"]) {
        uintptr_t startId = l["startPin"]; uintptr_t endId = l["endPin"];
        if (pinIdMap.count(startId) && pinIdMap.count(endId)) CreateLink(pinIdMap[startId], pinIdMap[endId]);
    }
    ed::SetCurrentEditor(nullptr);
}

}

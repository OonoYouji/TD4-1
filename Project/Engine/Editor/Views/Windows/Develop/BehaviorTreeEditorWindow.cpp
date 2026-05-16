#include "BehaviorTreeEditorWindow.h"
#include <imgui.h>
#include "Engine/Script/MonoScriptEngine.h"
#include "Engine/ECS/EntityComponentSystem/EntityComponentSystem.h"
#include "Engine/Core/Utility/Time/Time.h"
#include <algorithm>

namespace Editor {

BehaviorTreeEditorWindow::BehaviorTreeEditorWindow(ONEngine::EntityComponentSystem* ecs)
    : pEcs_(ecs) {
    ed::Config config;
    config.SettingsFile = "BehaviorTreeEditor.json";
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

    if (availableNodeClasses_.empty()) {
        availableNodeClasses_ = ONEngine::MonoScriptEngine::GetInstance().GetBehaviorNodeClasses();
    }

    if (ImGui::Button("Refresh Nodes")) {
        availableNodeClasses_ = ONEngine::MonoScriptEngine::GetInstance().GetBehaviorNodeClasses();
    }
    ImGui::SameLine();
    ImGui::Text("Available Nodes: %d", (int)availableNodeClasses_.size());

    ImGui::Separator();

    DrawGraphEditor();

    ImGui::End();
}

void BehaviorTreeEditorWindow::DrawGraphEditor() {
    ed::SetCurrentEditor(m_Editor);
    ed::Begin("BT_Graph_Editor", ImVec2(0, 0));

    // Entryノードがなければ作成 (安定化のためIDを固定)
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
        
        // ヘッダー部分
        ImGui::PushID(node.id.AsPointer());
        
        // タイトルの描画 (背景色は imgui-node-editor のスタイル設定に任せるか、簡易的に描画)
        ImGui::BeginGroup();
        ImGui::TextColored(ImVec4(1.0f, 0.8f, 0.2f, 1.0f), "== %s ==", node.name.c_str());
        if (node.className != "Entry") {
            ImGui::TextDisabled("[%s]", node.className.c_str());
        }
        ImGui::EndGroup();
        
        ImGui::Spacing();

        // ピンレイアウト
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

    // 3. インタラクション処理 (リンク作成)
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

    // 4. 削除処理
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

    // 5. コンテキストメニュー
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
    // 安定化のため直接構築
    m_Nodes.emplace_back(GetNextId(), className);
    Node* node = &m_Nodes.back();
    node->className = className;

    // クラス名に基づいた色設定 (UE風の簡易版)
    if (className == "Entry") {
        node->color = ImColor(255, 128, 0);
    } else if (className.find("Sequence") != std::string::npos || className.find("Selector") != std::string::npos) {
        node->color = ImColor(60, 60, 60);
    } else {
        node->color = ImColor(100, 40, 100);
    }

    // ピンの初期化
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

}

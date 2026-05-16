#include "BehaviorTreeEditorWindow.h"
#include <imgui.h>
#include "Engine/Script/MonoScriptEngine.h"
#include "Engine/ECS/EntityComponentSystem/EntityComponentSystem.h"

namespace Editor {

BehaviorTreeEditorWindow::BehaviorTreeEditorWindow(ONEngine::EntityComponentSystem* ecs)
    : pEcs_(ecs) {
    availableNodeClasses_ = ONEngine::MonoScriptEngine::GetInstance().GetBehaviorNodeClasses();
}

void BehaviorTreeEditorWindow::ShowImGui() {
    if (!ImGui::Begin("Behavior Tree Editor", nullptr)) {
        ImGui::End();
        return;
    }

    // 初回表示時または空の場合にリストを更新
    if (availableNodeClasses_.empty()) {
        availableNodeClasses_ = ONEngine::MonoScriptEngine::GetInstance().GetBehaviorNodeClasses();
    }

    if (ImGui::Button("Refresh Node List")) {
        availableNodeClasses_ = ONEngine::MonoScriptEngine::GetInstance().GetBehaviorNodeClasses();
        ONEngine::Console::Log("BehaviorTreeEditor: Refreshed node list. Found " + std::to_string(availableNodeClasses_.size()) + " classes.");
    }
    ImGui::SameLine();
    ImGui::Text("Nodes found: %d", (int)availableNodeClasses_.size());

    ImGui::Separator();

    if (ImGui::BeginChild("EditorArea", ImVec2(0, 0), true)) {
        DrawTreeEditor();

        // ポップアップをチャイルドウィンドウ内に移動
        if (ImGui::BeginPopupContextWindow("NodeEditorPopup", ImGuiPopupFlags_MouseButtonRight)) {
            if (ImGui::BeginMenu("Add Root Node")) {
                if (availableNodeClasses_.empty()) {
                    ImGui::MenuItem("(No nodes found)", nullptr, false, false);
                }

                for (const auto& className : availableNodeClasses_) {
                    if (ImGui::MenuItem(className.c_str())) {
                        if (!rootNode_) {
                            rootNode_ = std::make_unique<NodeInfo>();
                            rootNode_->className = className;
                            rootNode_->name = className;
                            rootNode_->idHash = static_cast<uint32_t>(std::hash<std::string>{}(className + std::to_string(ONEngine::Time::GetTime())));
                            ONEngine::Console::Log("BehaviorTreeEditor: Added root node: " + className);
                        }
                    }
                }
                ImGui::EndMenu();
            }
            ImGui::EndPopup();
        }
    }
    ImGui::EndChild();

    ImGui::End();
}

void BehaviorTreeEditorWindow::DrawTreeEditor() {
    if (!rootNode_) {
        ImGui::Text("Right click to add a root node.");
        return;
    }

    // 簡易的なツリー表示
    std::function<void(NodeInfo*)> drawNode = [&](NodeInfo* node) {
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags_OpenOnArrow | ImGuiTreeNodeFlags_DefaultOpen;
        if (node->children.empty()) flags |= ImGuiTreeNodeFlags_Leaf;

        bool open = ImGui::TreeNodeEx((void*)(intptr_t)node->idHash, flags, "%s", node->name.c_str());
        
        if (ImGui::BeginPopupContextItem()) {
            if (ImGui::BeginMenu("Add Child")) {
                for (const auto& className : availableNodeClasses_) {
                    if (ImGui::MenuItem(className.c_str())) {
                        auto child = std::make_unique<NodeInfo>();
                        child->className = className;
                        child->name = className;
                        child->idHash = static_cast<uint32_t>(std::hash<std::string>{}(className + std::to_string(node->children.size())));
                        node->children.push_back(std::move(child));
                    }
                }
                ImGui::EndMenu();
            }
            if (ImGui::MenuItem("Remove Node")) {
                // 親から自分を消すロジックが必要だが、ここでは簡略化
            }
            ImGui::EndPopup();
        }

        if (open) {
            for (auto& child : node->children) {
                drawNode(child.get());
            }
            ImGui::TreePop();
        }
    };

    drawNode(rootNode_.get());
}

}

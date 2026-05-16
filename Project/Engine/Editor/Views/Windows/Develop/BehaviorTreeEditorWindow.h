#pragma once

#include "Engine/Editor/Views/EditorViewCollection.h"
#include <string>
#include <vector>
#include <memory>

namespace ONEngine {
    class EntityComponentSystem;
}

namespace Editor {

class BehaviorTreeEditorWindow : public IEditorWindow {
public:
    BehaviorTreeEditorWindow(ONEngine::EntityComponentSystem* ecs);
    ~BehaviorTreeEditorWindow() override = default;

    void ShowImGui() override;

private:
    void DrawNodeList();
    void DrawTreeEditor();
    void DrawBlackboardViewer();

    struct NodeInfo {
        std::string className;
        std::string name;
        uint32_t idHash;
        std::vector<std::unique_ptr<NodeInfo>> children;
    };

    ONEngine::EntityComponentSystem* pEcs_;
    std::vector<std::string> availableNodeClasses_;
    std::unique_ptr<NodeInfo> rootNode_;
    
    int selectedEntityId_ = -1;
};

}

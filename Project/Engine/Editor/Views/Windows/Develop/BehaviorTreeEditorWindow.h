#pragma once

#include "Engine/Editor/Views/EditorViewCollection.h"
#include <string>
#include <vector>
#include <memory>
#include <map>

// imgui-node-editor
#include <imgui-node-editor/imgui_node_editor.h>

namespace ONEngine {
    class EntityComponentSystem;
}

namespace Editor {

namespace ed = ax::NodeEditor;

class BehaviorTreeEditorWindow : public IEditorWindow {
public:
    BehaviorTreeEditorWindow(ONEngine::EntityComponentSystem* ecs);
    ~BehaviorTreeEditorWindow() override;

    void ShowImGui() override;

private:
    enum class PinKind { Input, Output };

    struct Node;

    struct Pin {
        ed::PinId id;
        std::string name;
        PinKind kind;
        Node* node;

        Pin(int _id, const std::string& _name, PinKind _kind)
            : id(_id), name(_name), kind(_kind), node(nullptr) {}
    };

    struct Node {
        ed::NodeId id;
        std::string name;
        std::string className;
        std::vector<Pin> inputs;
        std::vector<Pin> outputs;
        ImColor color;
        ImVec2 size;

        Node(int _id, const std::string& _name, ImColor _color = ImColor(255, 255, 255))
            : id(_id), name(_name), color(_color) {}
    };

    struct Link {
        ed::LinkId id;
        ed::PinId startPinId;
        ed::PinId endPinId;
        ImColor color;

        Link(ed::LinkId _id, ed::PinId _start, ed::PinId _end)
            : id(_id), startPinId(_start), endPinId(_end), color(255, 255, 255) {}
    };

    enum class BBVarType { Int, Float, Bool, Vector3 };
    struct BBVariable {
        std::string key;
        BBVarType type = BBVarType::Float;
        int iVal = 0;
        float fVal = 0.0f;
        bool bVal = false;
        float vVal[3] = { 0,0,0 };
    };

    void InitializeEditor();
    void DrawNodeList();
    void DrawGraphEditor();
    void DrawBlackboardEditor();
    
    void SaveTree(const std::string& path);
    void LoadTree(const std::string& path);
    
    Node* CreateNode(const std::string& className);
    void CreateLink(ed::PinId startPin, ed::PinId endPin);

    ONEngine::EntityComponentSystem* pEcs_;
    std::vector<std::string> availableNodeClasses_;
    std::string m_CurrentFilePath = "Assets/AITrees/DefaultTree.json";
    
    ed::EditorContext* m_Editor = nullptr;
    std::vector<Node> m_Nodes;
    std::vector<Link> m_Links;
    std::vector<BBVariable> m_BBVariables;
    ImVec2 m_ContextNodePos;
    int m_NextId = 1;

    int GetNextId() { return m_NextId++; }
};

}

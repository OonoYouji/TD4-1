#pragma once

#include "Engine/Editor/Views/EditorViewCollection.h"
#include <string>
#include <vector>
#include <memory>
#include <map>

// imgui-node-editor
#include <imgui-node-editor/imgui_node_editor.h>

// engine
#include "Engine/Script/MonoScriptEngine.h"

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

    // 実行状態の更新（C#から呼ばれる）
    void UpdateNodeStatus(uint32_t nodeIdHash, int status);

    static BehaviorTreeEditorWindow* s_Instance;

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
        std::map<std::string, std::string> properties;
        bool isDecorator = false;

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

    enum class BBVarType { Int, Float, Bool, Vector3, String };
    struct BBVariable {
        std::string key;
        BBVarType type = BBVarType::Float;
        int iVal = 0;
        float fVal = 0.0f;
        bool bVal = false;
        float vVal[3] = { 0,0,0 };
        char sVal[128] = "";
    };

    void InitializeEditor();
    void DrawNodeList();
    void DrawGraphEditor();
    void DrawBlackboardEditor();
    void DrawNodeInspector();
    
    void SaveTree(const std::string& path);
    void LoadTree(const std::string& path);
    
    Node* CreateNode(const std::string& className, bool isDecorator = false);
    void CreateLink(ed::PinId startPin, ed::PinId endPin);

    ONEngine::EntityComponentSystem* pEcs_;
    std::vector<ONEngine::MonoScriptEngine::NodeClassInfo> availableNodeClasses_;
    std::string m_CurrentFilePath = "Assets/AITrees/DefaultTree.json";
    
    ed::EditorContext* m_Editor = nullptr;
    std::vector<Node> m_Nodes;
    std::vector<Link> m_Links;
    std::vector<BBVariable> m_BBVariables;
    std::map<uint32_t, int> m_RuntimeNodeStatuses;
    ed::NodeId m_SelectedNodeId = 0;
    ImVec2 m_ContextNodePos;
    int m_NextId = 1;

    int GetNextId() { return m_NextId++; }
};

}

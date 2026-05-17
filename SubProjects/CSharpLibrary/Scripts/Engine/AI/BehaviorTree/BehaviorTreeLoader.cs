using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

/// <summary>
/// エディタで作成したJSONファイルからビヘイビアツリーを構築するクラス
/// </summary>
public static class BehaviorTreeLoader
{
    public static BehaviorTree LoadFromFile(string filePath, Entity owner)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"BehaviorTreeLoader: File not found: {filePath}");
            return null;
        }

        string jsonContent = File.ReadAllText(filePath);
        return LoadFromJson(jsonContent, owner);
    }

    public static BehaviorTree LoadFromJson(string jsonContent, Entity owner)
    {
        JObject root = JObject.Parse(jsonContent);
        BehaviorTree tree = new BehaviorTree(owner);

        // 1. ノードのインスタンス化
        Dictionary<ulong, BehaviorNode> nodeInstances = new Dictionary<ulong, BehaviorNode>();
        Dictionary<ulong, ulong> pinToNodeMap = new Dictionary<ulong, ulong>();
        ulong entryNodeId = 0;

        foreach (var n in root["nodes"])
        {
            ulong id = (ulong)n["id"];
            string className = (string)n["className"];

            if (className == "Entry")
            {
                entryNodeId = id;
                // Entry自体のピンをマップに登録
                foreach (var pin in n["outputs"]) pinToNodeMap[(ulong)pin["id"]] = id;
                continue;
            }

            // リフレクションでクラスを生成
            Type type = Type.GetType(className);
            if (type == null)
            {
                // 名前空間なしで試行
                type = Type.GetType(className + ", CSharpLibrary");
            }

            if (type != null)
            {
                BehaviorNode node = (BehaviorNode)Activator.CreateInstance(type);
                node.NodeIdHash = (uint)id; // エディタのIDをハッシュとして使用
                nodeInstances[id] = node;

                // ピンのIDをノードIDに紐付け
                if (n["inputs"] != null) foreach (var pin in n["inputs"]) pinToNodeMap[(ulong)pin["id"]] = id;
                if (n["outputs"] != null) foreach (var pin in n["outputs"]) pinToNodeMap[(ulong)pin["id"]] = id;
            }
            else
            {
                Debug.LogError($"BehaviorTreeLoader: Could not find type {className}");
            }
        }

        // 2. リンクに基づいた親子関係の構築
        // C#側の実装では、Sequence/SelectorがBehaviorNodeのリストを持つ構造
        foreach (var l in root["links"])
        {
            ulong startPin = (ulong)l["startPin"];
            ulong endPin = (ulong)l["endPin"];

            if (pinToNodeMap.TryGetValue(startPin, out ulong parentId) &&
                pinToNodeMap.TryGetValue(endPin, out ulong childId))
            {
                // 親が Entry ノードの場合、その子がツリーのルート
                if (parentId == entryNodeId)
                {
                    if (nodeInstances.TryGetValue(childId, out var rootNode))
                    {
                        tree.RootNode = rootNode;
                    }
                }
                // 親が Composite ノードの場合
                else if (nodeInstances.TryGetValue(parentId, out var parentNode) && 
                         nodeInstances.TryGetValue(childId, out var childNode))
                {
                    if (parentNode is CompositeNode composite)
                    {
                        composite.AddChild(childNode);
                    }
                }
            }
        }

        if (tree.RootNode == null)
        {
            Debug.LogWarning("BehaviorTreeLoader: Loaded tree has no root connected to ENTRY.");
        }

        return tree;
    }
}

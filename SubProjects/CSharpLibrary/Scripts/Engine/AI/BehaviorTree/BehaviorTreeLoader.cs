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

        // 0. Blackboardの初期化
        if (root["blackboard"] != null)
        {
            foreach (var v in root["blackboard"])
            {
                string key = (string)v["key"];
                uint keyHash = HashString(key);
                int type = (int)v["type"];

                switch (type)
                {
                    case 0: // Int
                        tree.Blackboard.SetInt(keyHash, (int)v["iVal"]);
                        break;
                    case 1: // Float
                        tree.Blackboard.SetFloat(keyHash, (float)v["fVal"]);
                        break;
                    case 2: // Bool
                        tree.Blackboard.SetBool(keyHash, (bool)v["bVal"]);
                        break;
                    case 3: // Vector3
                        var va = v["vVal"];
                        tree.Blackboard.SetVector3(keyHash, new Vector3((float)va[0], (float)va[1], (float)va[2]));
                        break;
                    case 4: // String
                        tree.Blackboard.SetString(keyHash, (string)v["sVal"]);
                        break;
                }
            }
        }

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

                // エディタで設定したプロパティを反映
                if (n["properties"] is JObject props)
                {
                    foreach (var prop in props)
                    {
                        var field = type.GetField(prop.Key);
                        if (field != null)
                        {
                            try {
                                JToken val = prop.Value;
                                if (field.FieldType == typeof(float)) field.SetValue(node, val.Value<float>());
                                else if (field.FieldType == typeof(int)) field.SetValue(node, val.Value<int>());
                                else if (field.FieldType == typeof(bool)) field.SetValue(node, val.Value<bool>());
                                else if (field.FieldType == typeof(string)) field.SetValue(node, val.Value<string>());
                            } catch (Exception e) {
                                Debug.LogWarning($"BehaviorTreeLoader: Failed to set property {prop.Key} on {className}: {e.Message}");
                            }
                        }
                    }
                }

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
        foreach (var l in root["links"])
        {
            ulong startPin = (ulong)l["startPin"];
            ulong endPin = (ulong)l["endPin"];

            if (pinToNodeMap.TryGetValue(startPin, out ulong parentId) &&
                pinToNodeMap.TryGetValue(endPin, out ulong childId))
            {
                if (parentId == entryNodeId)
                {
                    if (nodeInstances.TryGetValue(childId, out var rootNode))
                    {
                        tree.RootNode = rootNode;
                    }
                }
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

    public static uint HashString(string str)
    {
        if (string.IsNullOrEmpty(str)) return 0;
        uint hash = 2166136261;
        foreach (char c in str)
        {
            hash = (hash ^ c) * 16777619;
        }
        return hash;
    }
}

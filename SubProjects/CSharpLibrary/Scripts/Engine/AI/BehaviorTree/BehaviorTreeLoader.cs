using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// JSONファイルからビヘイビアツリーを生成するクラス。
/// </summary>
public static class BehaviorTreeLoader
{
    public static BehaviorTree LoadFromFile(string path, Entity owner)
    {
        string jsonText = Mathf.LoadFile(path);
        if (string.IsNullOrEmpty(jsonText)) return null;

        var root = JObject.Parse(jsonText);
        BehaviorTree tree = new BehaviorTree(owner);

        // 0. Blackboard変数のロード
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
                foreach (var pin in n["outputs"]) pinToNodeMap[(ulong)pin["id"]] = id;
                continue;
            }

            Type type = Type.GetType(className) ?? Type.GetType(className + ", CSharpLibrary");

            if (type != null)
            {
                BehaviorNode node = (BehaviorNode)Activator.CreateInstance(type);
                node.NodeIdHash = (uint)id;
                
                // ブレークポイント
                if (n["hasBreakpoint"] != null) node.HasBreakpoint = (bool)n["hasBreakpoint"];

                nodeInstances[id] = node;

                // ノード本体のプロパティ反映
                ApplyProperties(type, node, n["properties"]);

                // Decorators のロード
                if (n["decorators"] is JArray decorators)
                {
                    foreach (var d in decorators)
                    {
                        string dClassName = (string)d["className"];
                        Type dType = Type.GetType(dClassName) ?? Type.GetType(dClassName + ", CSharpLibrary");
                        if (dType != null)
                        {
                            var decorator = (BehaviorDecorator)Activator.CreateInstance(dType);
                            ApplyProperties(dType, decorator, d["properties"]);
                            node.AddDecorator(decorator);
                        }
                    }
                }

                // Services のロード
                if (n["services"] is JArray services)
                {
                    foreach (var s in services)
                    {
                        string sClassName = (string)s["className"];
                        Type sType = Type.GetType(sClassName) ?? Type.GetType(sClassName + ", CSharpLibrary");
                        if (sType != null)
                        {
                            var service = (BehaviorService)Activator.CreateInstance(sType);
                            ApplyProperties(sType, service, s["properties"]);
                            node.AddService(service);
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

        // 監視ロジックの初期化
        tree.InitializeMonitoring();

        return tree;
    }

    private static void ApplyProperties(Type type, object instance, JToken props)
    {
        if (props == null) return;
        foreach (var p in props.Children<JProperty>())
        {
            FieldInfo field = type.GetField(p.Name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                try
                {
                    object val = ConvertValue(field.FieldType, p.Value.ToString());
                    field.SetValue(instance, val);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"BehaviorTreeLoader: Failed to set field {p.Name} on {type.Name}. {e.Message}");
                }
            }
        }
    }

    private static object ConvertValue(Type type, string value)
    {
        if (type == typeof(string)) return value;
        if (type == typeof(int)) return int.Parse(value);
        if (type == typeof(float)) return float.Parse(value);
        if (type == typeof(bool)) return bool.Parse(value);
        if (type.IsEnum) return Enum.Parse(type, value);
        return null;
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

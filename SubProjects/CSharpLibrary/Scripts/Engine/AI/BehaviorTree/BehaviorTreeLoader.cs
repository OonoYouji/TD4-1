using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// C++エディタで作成され JSON フォーマットで保存されたビヘイビアツリー資産を読み込み、
/// C#の実行用インスタンスとして展開（デシリアライズ）する静的ローダークラス。
/// </summary>
public static class BehaviorTreeLoader
{
    /// <summary>
    /// 指定されたファイルパスのJSONを読み込み、ビヘイビアツリーを構築する。
    /// </summary>
    /// <param name="path">JSONファイルのパス</param>
    /// <param name="owner">このツリーを実行するエンティティ（AI本体）</param>
    /// <returns>構築済みのBehaviorTreeインスタンス、失敗した場合はnull</returns>
    public static BehaviorTree LoadFromFile(string path, Entity owner)
    {
        // 1. ファイルの読み込みとパース
        string jsonText = Mathf.LoadFile(path);
        if (string.IsNullOrEmpty(jsonText)) return null;

        var root = JObject.Parse(jsonText);
        BehaviorTree tree = new BehaviorTree(owner);
        tree.SourcePath = path; // NEW: Set source path for filtering status updates in editor

        // 2. Blackboard（共有変数）のロード
        // JSON内の "blackboard" 配列から変数を読み取り、型に応じたディクショナリに格納する。
        if (root["blackboard"] != null)
        {
            foreach (var v in root["blackboard"])
            {
                string key = (string)v["key"];
                uint keyHash = HashString(key); // キーは高速化のために常にハッシュ値(uint)として扱う
                int type = (int)v["type"];
                
                // 型ごとに適切なメソッドを呼び出して初期値を設定
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

        // 3. ノード（タスク・コンポジット）とモジュール（デコレーター・サービス）のインスタンス化
        Dictionary<ulong, BehaviorNode> nodeInstances = new Dictionary<ulong, BehaviorNode>();
        Dictionary<ulong, ulong> pinToNodeMap = new Dictionary<ulong, ulong>();
        ulong entryNodeId = 0; // ツリーの開始点となる "Entry" ノードのID

        foreach (var n in root["nodes"])
        {
            ulong id = (ulong)n["id"];
            string className = (string)n["className"];

            // Entryノードは実際の実行ロジックを持たないため、ピンの接続関係のみを記録してスキップ
            if (className == "Entry")
            {
                entryNodeId = id;
                foreach (var pin in n["outputs"]) pinToNodeMap[(ulong)pin["id"]] = id;
                continue;
            }

            // クラス名からTypeを取得し、リフレクションでインスタンスを生成
            Type type = Type.GetType(className) ?? Type.GetType(className + ", CSharpLibrary");

            if (type != null)
            {
                BehaviorNode node = (BehaviorNode)Activator.CreateInstance(type);
                node.NodeIdHash = (uint)id;
                node.name = (string)n["name"] ?? className; // NEW: Set name
                node.Tree = tree; // NEW: Set tree instance
                
                // ブレークポイント設定の反映
                if (n["hasBreakpoint"] != null) node.HasBreakpoint = (bool)n["hasBreakpoint"];

                nodeInstances[id] = node;

                // ノード本体のプロパティ（インスペクターで設定した値）の反映
                ApplyProperties(type, node, n["properties"]);

                // 4. アタッチされている Decorator（条件）のロード
                if (n["decorators"] is JArray decorators)
                {
                    foreach (var d in decorators)
                    {
                        string dClassName = (string)d["className"];
                        Type dType = Type.GetType(dClassName) ?? Type.GetType(dClassName + ", CSharpLibrary");
                        if (dType != null)
                        {
                            var decorator = (BehaviorDecorator)Activator.CreateInstance(dType);
                            if (d["id"] != null) decorator.NodeIdHash = (uint)d["id"];
                            ApplyProperties(dType, decorator, d["properties"]);
                            node.AddDecorator(decorator);
                        }
                    }
                }

                // 5. アタッチされている Service（定期実行処理）のロード
                if (n["services"] is JArray services)
                {
                    foreach (var s in services)
                    {
                        string sClassName = (string)s["className"];
                        Type sType = Type.GetType(sClassName) ?? Type.GetType(sClassName + ", CSharpLibrary");
                        if (sType != null)
                        {
                            var service = (BehaviorService)Activator.CreateInstance(sType);
                            if (s["id"] != null) service.NodeIdHash = (uint)s["id"];
                            ApplyProperties(sType, service, s["properties"]);
                            node.AddService(service);
                        }
                    }
                }

                // 6. ピンのIDをノードIDに紐付けるマップを作成（後のリンク構築用）
                if (n["inputs"] != null) foreach (var pin in n["inputs"]) pinToNodeMap[(ulong)pin["id"]] = id;
                if (n["outputs"] != null) foreach (var pin in n["outputs"]) pinToNodeMap[(ulong)pin["id"]] = id;
            }
            else
            {
                Debug.LogError($"BehaviorTreeLoader: Could not find type {className}");
            }
        }

        // 7. リンク情報に基づいたツリー構造（親子関係）の構築
        // 実行順序を「高さ（Y座標）」で制御するため、リンクをターゲットノードのY座標でソートする
        var links = new List<JToken>(root["links"]);
        links.Sort((a, b) => {
            ulong childIdA = 0, childIdB = 0;
            pinToNodeMap.TryGetValue((ulong)a["endPin"], out childIdA);
            pinToNodeMap.TryGetValue((ulong)b["endPin"], out childIdB);

            float yA = 0, yB = 0;
            // JSONから座標を取得
            foreach (var n in root["nodes"]) {
                if ((ulong)n["id"] == childIdA) yA = (float)n["pos"][1];
                if ((ulong)n["id"] == childIdB) yB = (float)n["pos"][1];
            }
            return yA.CompareTo(yB);
        });

        foreach (var l in links)
        {
            ulong startPin = (ulong)l["startPin"];
            ulong endPin = (ulong)l["endPin"];

            // リンクの開始ピン・終了ピンがどのノードに属しているかを特定
            if (pinToNodeMap.TryGetValue(startPin, out ulong parentId) &&
                pinToNodeMap.TryGetValue(endPin, out ulong childId))
            {
                // 親が "Entry" ノードの場合は、その子ノードをこのツリーの「RootNode（最上位ノード）」として設定
                if (parentId == entryNodeId)
                {
                    if (nodeInstances.TryGetValue(childId, out var rootNode))
                    {
                        tree.RootNode = rootNode;
                    }
                }
                // それ以外の場合は、親コンポジットノードに子ノードを追加
                else if (nodeInstances.TryGetValue(parentId, out var parentNode) && 
                         nodeInstances.TryGetValue(childId, out var childNode))
                {
                    if (parentNode is CompositeNode composite)
                    {
                        composite.AddChild(childNode);
                        childNode.Parent = parentNode; // NEW: Set parent
                    }
                }
            }
        }

        if (tree.RootNode == null)
        {
            Debug.LogWarning("BehaviorTreeLoader: Loaded tree has no root connected to ENTRY.");
        }

        // 8. Observer Abort（監視による割り込み）のための初期化処理を実行
        tree.InitializeMonitoring();

        return tree;
    }

    /// <summary>
    /// リフレクションを使用して、JSON上のプロパティ値をC#インスタンスのフィールドに自動で代入する。
    /// </summary>
    /// <param name="type">対象クラスの型情報</param>
    /// <param name="instance">代入先のインスタンス</param>
    /// <param name="props">JSON内のプロパティオブジェクト</param>
    private static void ApplyProperties(Type type, object instance, JToken props)
    {
        if (props == null) return;
        foreach (var p in props.Children<JProperty>())
        {
            // まずはフィールドを探す
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
            else
            {
                // フィールドがなければプロパティを探す（AbortPolicyなどがこちらに該当する）
                PropertyInfo prop = type.GetProperty(p.Name, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        object val = ConvertValue(prop.PropertyType, p.Value.ToString());
                        prop.SetValue(instance, val);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"BehaviorTreeLoader: Failed to set property {p.Name} on {type.Name}. {e.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 文字列データを指定されたC#の型（int, float, bool, enumなど）に変換するユーティリティメソッド。
    /// </summary>
    private static object ConvertValue(Type type, string value)
    {
        if (type == typeof(string)) return value;
        if (type == typeof(int)) return int.Parse(value);
        if (type == typeof(float)) return float.Parse(value);
        if (type == typeof(bool)) return bool.Parse(value);
        if (type.IsEnum)
        {
            // 数値文字列（"0", "1" など）か、名前（"Success", "Failure" など）かを判定
            if (int.TryParse(value, out int intVal))
            {
                return Enum.ToObject(type, intVal);
            }
            return Enum.Parse(type, value, true);
        }
        return null;
    }

    /// <summary>
    /// 文字列を高速な32ビットハッシュ値（FNV-1aアルゴリズム）に変換する。
    /// BlackboardのキーやノードIDの管理に使用される。
    /// </summary>
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

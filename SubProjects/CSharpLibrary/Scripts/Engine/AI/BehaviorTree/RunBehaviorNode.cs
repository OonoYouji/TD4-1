using System;
using System.Collections.Generic;

/// <summary>
/// 別のビヘイビアツリー資産（JSONファイル）を動的に読み込み、
/// 一つのノードであるかのように実行する「サブツリー（Sub-tree）」ノード。
/// ロジックのモジュール化・再利用（例：索敵ロジックの共通化）に非常に有用。
/// UEの「Run Behavior」ノードに相当する。
/// </summary>
public class RunBehaviorNode : BehaviorNode
{
    /// <summary>
    /// 実行する対象のビヘイビアツリー資産のファイルパス。
    /// </summary>
    [SerializeField]
    public string treePath = "";

    // 読み込んだサブツリーの最上位（起点）となるノードのキャッシュ
    private BehaviorNode _rootOfSubtree = null;
    
    // 読み込みに失敗した場合に再試行を防ぐためのフラグ
    private bool _failedToLoad = false;

    /// <summary>
    /// ノードの実行処理。初回実行時にJSONからツリーを展開し、以後はそのルートに処理を委譲する。
    /// </summary>
    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        // 過去にロードに失敗している場合は即座にFailureを返す
        if (_failedToLoad) return NodeStatus.Failure;

        // 1. 初回実行時にサブツリーのロードを試みる
        if (_rootOfSubtree == null && !string.IsNullOrEmpty(treePath))
        {
            try
            {
                Debug.Log($"[RunBehavior] Attempting to load subtree: {treePath}");
                var tree = BehaviorTreeLoader.LoadFromFile(treePath, owner);
                if (tree != null)
                {
                    _rootOfSubtree = tree.RootNode;
                    Debug.Log($"[RunBehavior] Successfully loaded subtree: {treePath} (Root:{_rootOfSubtree.name})");
                }
                else {
                    Debug.LogWarning($"[RunBehavior] LoadFromFile returned null for: {treePath}");
                    _failedToLoad = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"RunBehaviorNode: Failed to load subtree {treePath}. Error: {e.Message}");
                _failedToLoad = true;
            }
        }

        // ロードできなかった場合（ファイルが存在しない、フォーマットエラー等）
        if (_rootOfSubtree == null) return NodeStatus.Failure;

        // 2. サブツリーの実行
        var status = _rootOfSubtree.Tick(blackboard, owner);
        return status;
    }
}

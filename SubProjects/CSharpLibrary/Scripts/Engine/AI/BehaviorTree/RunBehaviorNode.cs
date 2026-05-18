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
    public string subtreePath = "";

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
        if (_rootOfSubtree == null && !string.IsNullOrEmpty(subtreePath))
        {
            try
            {
                // BehaviorTreeLoaderを使用して指定パスからツリーを構築。
                // サブツリーのノードも同じowner（エンティティ）を対象として生成される。
                // 注意: 本来は無限ループ（AがBを呼び、BがAを呼ぶ）を防ぐための循環参照チェック機構が必要。
                var tree = BehaviorTreeLoader.LoadFromFile(subtreePath, owner);
                if (tree != null)
                {
                    _rootOfSubtree = tree.RootNode;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"RunBehaviorNode: Failed to load subtree {subtreePath}. Error: {e.Message}");
                _failedToLoad = true;
            }
        }

        // ロードできなかった場合（ファイルが存在しない、フォーマットエラー等）
        if (_rootOfSubtree == null) return NodeStatus.Failure;

        // 2. サブツリーの実行
        // 読み込んだサブツリーのRootNodeに対して、現在のBlackboardとOwnerを渡して実行（Tick）を委譲する。
        // サブツリーの内部でも同じBlackboardを共有するため、変数の受け渡しはシームレスに行われる。
        return _rootOfSubtree.Tick(blackboard, owner);
    }
}

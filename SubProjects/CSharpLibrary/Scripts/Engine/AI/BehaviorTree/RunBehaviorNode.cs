using System;
using System.Collections.Generic;

/// <summary>
/// 別のビヘイビアツリー資産を一つのノードとして実行するサブツリーノード。
/// </summary>
public class RunBehaviorNode : BehaviorNode
{
    [SerializeField]
    public string subtreePath = "";

    private BehaviorNode _rootOfSubtree = null;
    private bool _failedToLoad = false;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        if (_failedToLoad) return NodeStatus.Failure;

        // 初回実行時にロード
        if (_rootOfSubtree == null && !string.IsNullOrEmpty(subtreePath))
        {
            try
            {
                // 注意: 循環参照チェックは本来ローダー側で行うべきだが、
                // ここでは簡易的にロードを試みる
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

        if (_rootOfSubtree == null) return NodeStatus.Failure;

        // サブツリーのルートを実行
        return _rootOfSubtree.Tick(blackboard, owner);
    }
}

using System.Collections.Generic;

/// <summary>
/// 複数の子ノードを持つコンポジットノードの基底クラス。
/// </summary>
public abstract class CompositeNode : BehaviorNode
{
    protected readonly List<BehaviorNode> children = new List<BehaviorNode>();

    public CompositeNode() { }

    public CompositeNode(params BehaviorNode[] nodes)
    {
        children.AddRange(nodes);
    }

    public void AddChild(BehaviorNode node)
    {
        children.Add(node);
    }

    public List<BehaviorNode> GetChildren() => children;
}

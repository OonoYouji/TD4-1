using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 1つのエンティティに関連付けられたビヘイビアツリーの実行単位。
/// </summary>
public class BehaviorTree
{
    public BehaviorNode RootNode { get; set; }
    public Blackboard Blackboard { get; } = new Blackboard();
    public Entity Owner { get; }

    public BehaviorTree(Entity owner)
    {
        Owner = owner;
    }

    public void Tick()
    {
        if (RootNode == null || Owner == null) return;
        RootNode.Execute(Blackboard, Owner);
    }

    /// <summary>
    /// 全てのノードの実行状態を取得する（デバッグ用）
    /// </summary>
    public void GetAllNodeStatuses(Dictionary<uint, NodeStatus> outStatuses)
    {
        if (RootNode == null) return;
        CollectStatusRecursive(RootNode, outStatuses);
    }

    private void CollectStatusRecursive(BehaviorNode node, Dictionary<uint, NodeStatus> outStatuses)
    {
        if (node == null) return;
        outStatuses[node.NodeIdHash] = node.LastStatus;
        
        // C++エディタへ通知
        Internal_UpdateNodeStatus(node.NodeIdHash, (int)node.LastStatus);

        if (node is CompositeNode composite)
        {
            foreach (var child in composite.GetChildren())
            {
                CollectStatusRecursive(child, outStatuses);
            }
        }
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void Internal_UpdateNodeStatus(uint nodeIdHash, int status);
}

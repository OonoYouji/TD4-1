using System;
using System.Collections.Generic;

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
}

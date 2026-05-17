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

    private bool _reevaluateRequest = false;
    private readonly Dictionary<uint, List<BehaviorDecorator>> _monitoredDecorators = new Dictionary<uint, List<BehaviorDecorator>>();

    public BehaviorTree(Entity owner)
    {
        Owner = owner;
        Blackboard.OnValueChanged += HandleBlackboardChanged;
    }

    /// <summary>
    /// ツリー構造が決定した後に、Decoratorの監視を初期化する
    /// </summary>
    public void InitializeMonitoring()
    {
        _monitoredDecorators.Clear();
        if (RootNode == null) return;
        RegisterMonitoringRecursive(RootNode);
    }

    private void RegisterMonitoringRecursive(BehaviorNode node)
    {
        foreach (var decorator in node.Decorators)
        {
            if (decorator.AbortPolicy != ObserverAbortPolicy.None)
            {
                uint key = decorator.GetMonitoredKey();
                if (key != 0)
                {
                    if (!_monitoredDecorators.ContainsKey(key)) _monitoredDecorators[key] = new List<BehaviorDecorator>();
                    _monitoredDecorators[key].Add(decorator);
                }
            }
        }

        if (node is CompositeNode composite)
        {
            foreach (var child in composite.GetChildren()) RegisterMonitoringRecursive(child);
        }
    }

    private void HandleBlackboardChanged(uint keyHash)
    {
        // 監視中のキーが変わったかチェック
        if (_monitoredDecorators.ContainsKey(keyHash))
        {
            _reevaluateRequest = true;
        }
    }

    public void Tick()
    {
        if (RootNode == null || Owner == null) return;

        // 再評価リクエストがあれば、現在の実行状態を無視して（ステートレスなので自然に）最初から回す
        // 現在の Reactive な実装では毎フレームRootから回しているため、このフラグは将来的な最適化（イベント駆動）の布石となる
        RootNode.Tick(Blackboard, Owner);
        _reevaluateRequest = false;
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

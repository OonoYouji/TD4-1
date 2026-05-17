using System.Collections.Generic;

/// <summary>
/// ビヘイビアツリーの全てのノードの基底クラス。
/// </summary>
public abstract class BehaviorNode
{
    public uint NodeIdHash { get; set; }
    public NodeStatus LastStatus { get; protected set; } = NodeStatus.Failure;

    public List<BehaviorDecorator> Decorators { get; } = new List<BehaviorDecorator>();
    public List<BehaviorService> Services { get; } = new List<BehaviorService>();

    public void AddDecorator(BehaviorDecorator decorator) => Decorators.Add(decorator);
    public void AddService(BehaviorService service) => Services.Add(service);

    /// <summary>
    /// ノードを実行する（内部でDecorator/Serviceを処理する）
    /// </summary>
    public NodeStatus Tick(Blackboard blackboard, Entity owner)
    {
        // 1. Services の実行 (Interval管理)
        float currentTime = Time.time;
        foreach (var service in Services)
        {
            uint timeKey = BehaviorTreeLoader.HashString("LastSrvTick_" + service.NodeIdHash);
            float lastTick = blackboard.GetFloat(timeKey, -1.0f);
            if (currentTime - lastTick >= service.Interval)
            {
                service.OnTick(blackboard, owner);
                blackboard.SetFloat(timeKey, currentTime);
            }
        }

        // 2. Decorators の条件チェック (Pre-Condition)
        foreach (var decorator in Decorators)
        {
            if (!decorator.CalculateCondition(blackboard, owner))
            {
                return LastStatus = NodeStatus.Failure;
            }
        }

        // 3. 本体ロジックの実行
        NodeStatus result = Execute(blackboard, owner);

        // 4. Decorators による結果の加工 (Post-Process)
        foreach (var decorator in Decorators)
        {
            result = decorator.PostProcessStatus(result, blackboard);
        }

        return LastStatus = result;
    }

    /// <summary>
    /// ノード固有のロジック（継承先で実装）
    /// </summary>
    protected abstract NodeStatus Execute(Blackboard blackboard, Entity owner);
}

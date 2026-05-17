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
        // 1. Services の実行
        foreach (var service in Services)
        {
            service.OnTick(blackboard, owner);
        }

        // 2. Decorators の条件チェック
        foreach (var decorator in Decorators)
        {
            if (!decorator.CalculateCondition(blackboard, owner))
            {
                return LastStatus = NodeStatus.Failure;
            }
        }

        // 3. 本体ロジックの実行
        return LastStatus = Execute(blackboard, owner);
    }

    /// <summary>
    /// ノード固有のロジック（継承先で実装）
    /// </summary>
    protected abstract NodeStatus Execute(Blackboard blackboard, Entity owner);
}

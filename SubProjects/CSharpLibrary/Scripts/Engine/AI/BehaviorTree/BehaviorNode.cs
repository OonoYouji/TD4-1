/// <summary>
/// ビヘイビアツリーの全てのノードの基底クラス。
/// ステートレス（Flyweightパターン）でなければならない。
/// </summary>
public abstract class BehaviorNode
{
    /// <summary>
    /// ノードのロジックを実行する。
    /// </summary>
    public abstract NodeStatus Execute(Blackboard blackboard, Entity owner);
}

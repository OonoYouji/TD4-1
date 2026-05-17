/// <summary>
/// ビヘイビアツリーの全てのノードの基底クラス。
/// ステートレス（Flyweightパターン）でなければならない。
/// </summary>
public abstract class BehaviorNode
{
    /// <summary>
    /// エディター側で一意に割り振られるノードIDのハッシュ値。
    /// Blackboardへの状態保存キーとして使用される。
    /// </summary>
    public uint NodeIdHash { get; set; }

    /// <summary>
    /// 直近の実行状態。デバッグビジュアライザーで使用される。
    /// </summary>
    public NodeStatus LastStatus { get; protected set; } = NodeStatus.Failure;

    /// <summary>
    /// ノードのロジックを実行する。
    /// </summary>
    public abstract NodeStatus Execute(Blackboard blackboard, Entity owner);
}

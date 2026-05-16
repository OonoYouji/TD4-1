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
    /// ノードのロジックを実行する。
    /// </summary>
    public abstract NodeStatus Execute(Blackboard blackboard, Entity owner);
}

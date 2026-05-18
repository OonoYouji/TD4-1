using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 1つのエンティティ（AIキャラクターなど）に関連付けられるビヘイビアツリーの実行単位。
/// ツリー全体のルートノード、共有記憶（Blackboard）、および監視ロジックの管理を行う。
/// </summary>
public class BehaviorTree
{
    /// <summary>
    /// ツリーの最上位（起点）となるノード。
    /// </summary>
    public BehaviorNode RootNode { get; set; }

    /// <summary>
    /// このツリー内で共有される記憶領域。
    /// ターゲットの情報、フラグ、クールダウンのタイマーなどが保持される。
    /// </summary>
    public Blackboard Blackboard { get; } = new Blackboard();

    /// <summary>
    /// このビヘイビアツリーを実行している（アタッチされている）エンティティ。
    /// 移動処理やTransformの取得などに使用される。
    /// </summary>
    public Entity Owner { get; }

    // Blackboardの変数が書き換わった際、ツリーの再評価（割り込み処理）が必要かどうかを示すフラグ
    private bool _reevaluateRequest = false;

    // Blackboardの特定のキー（変数）を監視しているデコレーターのリスト。
    // キーのハッシュ値を元に、どのデコレーターが反応すべきかを管理する。
    private readonly Dictionary<uint, List<BehaviorDecorator>> _monitoredDecorators = new Dictionary<uint, List<BehaviorDecorator>>();

    // 現在実行中のノード（Running状態のノード）へのポインタ。
    // イベント駆動エンジンとして、次フレームはここから再開する。
    public BehaviorNode ActiveNode { get; set; }

    /// <summary>
    /// 新しいビヘイビアツリーのインスタンスを生成する。
    /// </summary>
    /// <param name="owner">ツリーを所有するエンティティ</param>
    public BehaviorTree(Entity owner)
    {
        Owner = owner;
        // Blackboardの値が変更されたイベントを購読し、変更時にツリーへ通知が行くように設定
        Blackboard.OnValueChanged += HandleBlackboardChanged;
    }

    /// <summary>
    /// ツリー構造が決定（ロード）された後に、Decoratorの監視登録を初期化する。
    /// ツリー全体を走査し、ObserverAbortが設定されているデコレーターを抽出する。
    /// </summary>
    public void InitializeMonitoring()
    {
        _monitoredDecorators.Clear();
        if (RootNode == null) return;
        RegisterMonitoringRecursive(RootNode);
    }

    /// <summary>
    /// 再帰的にノードを辿り、監視が必要なデコレーターを _monitoredDecorators に登録する内部メソッド。
    /// </summary>
    /// <param name="node">現在走査中のノード</param>
    private void RegisterMonitoringRecursive(BehaviorNode node)
    {
        // ノードにアタッチされているすべてのデコレーターをチェック
        foreach (var decorator in node.Decorators)
        {
            // AbortPolicy (中断設定) が None 以外（SelfやLowerPriorityなど）の場合のみ監視対象とする
            if (decorator.AbortPolicy != ObserverAbortPolicy.None)
            {
                uint key = decorator.GetMonitoredKey();
                if (key != 0)
                {
                    // 監視リストにキーが存在しなければ新しくリストを作成
                    if (!_monitoredDecorators.ContainsKey(key)) _monitoredDecorators[key] = new List<BehaviorDecorator>();
                    
                    // そのキーの監視リストにデコレーターを追加
                    _monitoredDecorators[key].Add(decorator);
                }
            }
        }

        // CompositeNode (SequenceやSelector等、子を持つノード) の場合は、さらに子ノードを再帰的に走査
        if (node is CompositeNode composite)
        {
            foreach (var child in composite.GetChildren()) RegisterMonitoringRecursive(child);
        }
    }

    /// <summary>
    /// Blackboard内の変数値が変更された際に呼び出されるコールバック。
    /// </summary>
    /// <param name="keyHash">変更された変数のキーハッシュ</param>
    private void HandleBlackboardChanged(uint keyHash)
    {
        // 変更されたキーを監視しているデコレーターが存在するかチェック
        if (_monitoredDecorators.ContainsKey(keyHash))
        {
            // 監視対象の変数が変わったため、現在の実行パスが不正になった可能性がある。
            // 次回のTickでツリー全体の再評価（Abort処理）を要求する。
            _reevaluateRequest = true;
        }
    }

    /// <summary>
    /// 指定したノードとそのすべての子孫ノードに対して Abort 処理を再帰的に実行する。
    /// </summary>
    private void AbortRecursive(BehaviorNode node)
    {
        if (node == null) return;

        // 子ノード（コンポジットの場合）を先に中断
        if (node is CompositeNode composite)
        {
            foreach (var child in composite.GetChildren()) AbortRecursive(child);
        }

        // ノード本体の中断処理を呼び出し
        node.OnAbort(Blackboard, Owner);
    }

    /// <summary>
    /// 毎フレーム呼び出される、ビヘイビアツリーの実行エントリーポイント。
    /// </summary>
    public void Tick()
    {
        if (RootNode == null || Owner == null) return;

        // 1. 再評価リクエスト（割り込み）のチェック
        if (_reevaluateRequest)
        {
            // 割り込みが発生した場合、現在の実行パスにあるすべてのノードに中断通知を送り、
            // 最初（Root）から評価し直す。
            if (ActiveNode != null)
            {
                // 現在のアクティブノードからRootまで辿ってすべて中断するか、
                // あるいは安全のためにRootから全走査して中断するか。
                // ここでは現在のアクティブノード周辺のみを確実に止めるため、
                // ルートから全子孫に対して再帰的にAbortを送る（ステートレスなので影響は軽微）。
                AbortRecursive(RootNode);
            }
            ActiveNode = null;
            _reevaluateRequest = false;
        }

        // 2. 実行の開始または再開
        // ActiveNode が保持されていればそこから、なければ Root から Tick を開始する。
        NodeStatus status;
        if (ActiveNode != null)
        {
            status = ActiveNode.Tick(Blackboard, Owner);
        }
        else
        {
            status = RootNode.Tick(Blackboard, Owner);
        }

        // 3. 親への結果伝播（バブリング）
        // ノードが Success または Failure を返した場合、親ノードの OnChildCompleted を呼び出し、
        // 実行ポインタを親へと戻しながら次のノードを決定する。
        while (status != NodeStatus.Running)
        {
            // Rootが完了した、または実行ポインタが未設定の場合はツリー全体が完了
            if (ActiveNode == null || ActiveNode.Parent == null)
            {
                ActiveNode = null;
                break;
            }

            var finishedNode = ActiveNode;
            ActiveNode = finishedNode.Parent; // 親へ戻る
            status = ActiveNode.OnChildCompleted(finishedNode, status, Blackboard, Owner);
        }

        // Tick終了時点で status が Running の場合、
        // 各ノードの Tick 内部で ActiveNode が更新されているため、次フレームはその地点から再開される。
    }

    /// <summary>
    /// 全てのノードの直近の実行状態（Success/Failure/Running）を取得する。
    /// 主にエディタでのデバッグ可視化（ハイライト表示）に使用される。
    /// </summary>
    /// <param name="outStatuses">結果を格納するディクショナリ</param>
    public void GetAllNodeStatuses(Dictionary<uint, NodeStatus> outStatuses)
    {
        if (RootNode == null) return;
        CollectStatusRecursive(RootNode, outStatuses);
    }

    /// <summary>
    /// ツリーを再帰的に辿り、実行状態を収集・C++エンジンへ通知する内部メソッド。
    /// </summary>
    /// <param name="node">現在走査中のノード</param>
    /// <param name="outStatuses">結果を格納するディクショナリ</param>
    private void CollectStatusRecursive(BehaviorNode node, Dictionary<uint, NodeStatus> outStatuses)
    {
        if (node == null) return;
        
        // C#側のディクショナリに状態を記録
        outStatuses[node.NodeIdHash] = node.LastStatus;
        
        // C++エディタへ状態を通知（グラフ上のノード枠色をリアルタイムに変更するため）
        Internal_UpdateNodeStatus(node.NodeIdHash, (int)node.LastStatus);

        // 子ノードも再帰的に収集
        if (node is CompositeNode composite)
        {
            foreach (var child in composite.GetChildren())
            {
                CollectStatusRecursive(child, outStatuses);
            }
        }
    }

    /// <summary>
    /// C++エディタへノードの状態を送信するための内部呼び出し。
    /// </summary>
    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void Internal_UpdateNodeStatus(uint nodeIdHash, int status);
}

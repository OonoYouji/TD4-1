using System;

/// <summary>
/// Blackboardの変数を監視した割り込み（Observer Aborts）のルールを定義する列挙型。
/// </summary>
public enum ObserverAbortPolicy
{
    /// <summary>監視による割り込みを行わない。</summary>
    None,
    /// <summary>実行中に自身の条件が満たされなくなった際、即座に自身（およびその子孫）を中断する。</summary>
    Self,
    /// <summary>他の優先度の低いタスクを実行中に、自身の条件が満たされた際、そのタスクを中断して自身に実行権を移す。</summary>
    LowerPriority,
    /// <summary>SelfとLowerPriorityの両方を適用する。</summary>
    Both
}

/// <summary>
/// Blackboardの特定の変数（キー）を監視し、その値に基づいて実行の可否を判定するデコレーター。
/// Unreal Engineの Blackboard Decorator に相当する。
/// </summary>
public class BlackboardDecorator : BehaviorDecorator
{
    /// <summary>
    /// 監視対象となるBlackboardのキー名。
    /// エディタ上では [BlackboardKey] 属性により、登録されている変数のドロップダウンとして表示される。
    /// </summary>
    [BlackboardKey]
    public string keyName = "";

    /// <summary>
    /// ツリー初期化時に呼ばれ、このデコレーターが監視すべきキーのハッシュ値を BehaviorTree に登録する。
    /// </summary>
    public override uint GetMonitoredKey()
    {
        return BehaviorTreeLoader.HashString(keyName);
    }

    /// <summary>
    /// ノード実行前に呼ばれ、Blackboardの値が条件を満たしているかを判定する。
    /// </summary>
    public override bool CalculateCondition(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString(keyName);
        
        // キーが存在しなければ無条件で失敗（False）とする
        if (!blackboard.HasKey(key)) return false;

        // 型が不定な場合でも汎用的に値を取得する
        object val = blackboard.GetValueAsObject(key);
        if (val == null) return false;

        // 取得した値の型に応じて「条件を満たしているか（True扱いか）」を判定する
        // 本来は列挙型で比較演算子（Equal, NotEqual, GreaterThan 等）を持たせるべきだが、
        // 現状は簡易的に「0ではない」「空ではない」「Falseではない」ことを成功条件としている。
        if (val is int i) return i != 0;
        if (val is float f) return f != 0.0f;
        if (val is bool b) return b;
        if (val is string s) return !string.IsNullOrEmpty(s);

        // その他の参照型などであれば、nullでなかった時点で成功とする
        return true;
    }
}

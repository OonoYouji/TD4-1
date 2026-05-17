using System;

public enum ObserverAbortPolicy
{
    None,
    Self,
    LowerPriority,
    Both
}

/// <summary>
/// Blackboardの変数を監視し、条件が満たされなくなった場合に実行を中断するデコレーター
/// </summary>
public class BlackboardDecorator : BehaviorDecorator
{
    [BlackboardKey]
    public string keyName = "";

    public ObserverAbortPolicy abortPolicy = ObserverAbortPolicy.None;

    public override bool CalculateCondition(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString(keyName);
        if (!blackboard.HasKey(key)) return false;

        // とりあえず「値が 0 または null/空 でなければ成功」という簡易的な判定
        // 本来は「Equal」「Not Equal」などの比較演算子をサポートすべき
        object val = GetValue(blackboard, key);
        if (val == null) return false;

        if (val is int i) return i != 0;
        if (val is float f) return f != 0.0f;
        if (val is bool b) return b;
        if (val is string s) return !string.IsNullOrEmpty(s);

        return true;
    }

    private object GetValue(Blackboard bb, uint key)
    {
        // 型が不明なので一旦全探索（本来は型情報も保存すべき）
        if (bb.HasKey(key)) {
            // 文字列
            string s = bb.GetString(key, null);
            if (s != null) return s;
            // 数値系はデフォルト値との兼ね合いで判定が難しいが、一旦objectで取得する仕組みが必要
        }
        return null;
    }
}

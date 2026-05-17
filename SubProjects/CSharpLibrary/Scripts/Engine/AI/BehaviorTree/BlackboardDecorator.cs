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

    public override uint GetMonitoredKey()
    {
        return BehaviorTreeLoader.HashString(keyName);
    }

    public override bool CalculateCondition(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString(keyName);
        if (!blackboard.HasKey(key)) return false;

        object val = blackboard.GetValueAsObject(key);
        if (val == null) return false;

        if (val is int i) return i != 0;
        if (val is float f) return f != 0.0f;
        if (val is bool b) return b;
        if (val is string s) return !string.IsNullOrEmpty(s);

        return true;
    }
}

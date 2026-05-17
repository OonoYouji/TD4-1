using System;

public enum LogicOp
{
    And,
    Or,
    Not
}

/// <summary>
/// 複数の条件や否定を組み合わせるための論理ゲートデコレーター。
/// </summary>
public class LogicDecorator : BehaviorDecorator
{
    public LogicOp operation = LogicOp.And;

    [BlackboardKey]
    public string keyA = "";

    [BlackboardKey]
    public string keyB = "";

    public override bool CalculateCondition(Blackboard blackboard, Entity owner)
    {
        uint hashA = BehaviorTreeLoader.HashString(keyA);
        uint hashB = BehaviorTreeLoader.HashString(keyB);

        bool valA = IsTrue(blackboard, hashA);
        bool valB = IsTrue(blackboard, hashB);

        switch (operation)
        {
            case LogicOp.And: return valA && valB;
            case LogicOp.Or:  return valA || valB;
            case LogicOp.Not: return !valA;
            default: return false;
        }
    }

    private bool IsTrue(Blackboard bb, uint key)
    {
        if (key == 0 || !bb.HasKey(key)) return false;
        object val = bb.GetValueAsObject(key);
        if (val == null) return false;

        if (val is bool b) return b;
        if (val is int i) return i != 0;
        if (val is float f) return f != 0.0f;
        if (val is string s) return !string.IsNullOrEmpty(s);
        
        return true;
    }
}

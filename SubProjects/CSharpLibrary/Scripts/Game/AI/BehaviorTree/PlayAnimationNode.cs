using System;

/// <summary>
/// アニメーションを切り替えるためのノード。
/// 内部的には InvokeEventNode を継承し、アニメーションシステム向けの命名規則に従ったイベントを発行する。
/// </summary>
public class PlayAnimationNode : InvokeEventNode
{
    public PlayAnimationNode() : base() { }

    public PlayAnimationNode(string animName, bool wait = true) 
        : base("PlayAnimation:" + animName, wait)
    {
    }

    // エディタからのプロパティ設定を容易にするため、InvokeEventNodeのeventNameを
    // アニメーション名として解釈するラッパーを提供しても良い。
}

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// C++側のイベントシステムへのインターフェース。
/// </summary>
public static class FrameEvent
{
    /// <summary>
    /// C++側のEventTypeと一致させる必要があります。
    /// </summary>
    public enum Type : byte
    {
        TestEvent = 0,
        NamedEvent = 1,
        Attack = 2,
    }

    /// <summary>
    /// エンティティに関連するイベントをキューに追加します。
    /// </summary>
    public static void EnqueueEntityEvent(Type eventType, int entityId)
    {
        Internal_EnqueueEntityEvent(eventType, entityId);
    }

    /// <summary>
    /// 名前付きイベントをキューに追加します。
    /// </summary>
    public static void EnqueueNamedEvent(string eventName, int entityId)
    {
        Internal_EnqueueNamedEvent(eventName, entityId);
    }

    /// <summary>
    /// 攻撃（当たり判定生成）イベントをキューに追加します。
    /// </summary>
    public static void EnqueueAttackEvent(int ownerId, float damage, float radius, float duration, float offsetForward, float offsetUp)
    {
        Internal_EnqueueAttackEvent(ownerId, damage, radius, duration, offsetForward, offsetUp);
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void Internal_EnqueueEntityEvent(Type eventType, int entityId);

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void Internal_EnqueueNamedEvent(string eventName, int entityId);

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void Internal_EnqueueAttackEvent(int ownerId, float damage, float radius, float duration, float offsetForward, float offsetUp);
}

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
        // ...
    }

    /// <summary>
    /// エンティティに関連するイベントをキューに追加します。
    /// </summary>
    public static void EnqueueEntityEvent(Type eventType, int entityId)
    {
        Internal_EnqueueEntityEvent(eventType, entityId);
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void Internal_EnqueueEntityEvent(Type eventType, int entityId);
}

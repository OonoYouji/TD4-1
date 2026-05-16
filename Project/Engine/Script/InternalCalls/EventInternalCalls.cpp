#include "EventInternalCalls.h"
#include "Engine/Core/Event/FrameEventQueue.h"
#include <mono/jit/jit.h>

namespace ONEngine {

    /// <summary>
    /// C#からイベントをC++のキューに追加するための内部呼び出し
    /// </summary>
    /// <param name="eventType">イベントの種類</param>
    /// <param name="entityId">関連するエンティティのID</param>
    static void Internal_EnqueueEntityEvent(EventType eventType, int32_t entityId)
    {
        Event e;
        e.type = eventType;
        e.payload = EntityEventPayload{ entityId };
        FrameEventQueue::GetInstance().Enqueue(e);
    }

    void AddEventInternalCalls()
    {
        mono_add_internal_call("FrameEvent::Internal_EnqueueEntityEvent", (void*)Internal_EnqueueEntityEvent);
    }
}

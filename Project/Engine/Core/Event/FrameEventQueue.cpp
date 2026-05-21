#include "FrameEventQueue.h"
#include "Engine/Core/Utility/Utility.h"
#include "Engine/Script/MonoScriptEngine.h"
#include <string>

namespace ONEngine {

    FrameEventQueue& FrameEventQueue::GetInstance() {
        static FrameEventQueue instance;
        return instance;
    }

    void FrameEventQueue::Enqueue(const Event& event) {
        std::lock_guard<std::mutex> lock(queueMutex_);
        queue_.push_back(event);
    }

    void FrameEventQueue::Flush() {
        // 現在のキューをローカルにスワップして、ロック時間を最小限に抑える
        std::vector<Event> processingQueue;
        {
            std::lock_guard<std::mutex> lock(queueMutex_);
            if (queue_.empty()) {
                return;
            }
            std::swap(processingQueue, queue_);
        }

        // スワップしたキューを処理する
        for (const auto& event : processingQueue) {
            if (event.type == EventType::NamedEvent)
            {
                const auto& payload = std::get<NamedEventPayload>(event.payload);
                Console::Log("[FrameEventQueue] Triggered Named Event: " + payload.eventName + " for Entity: " + std::to_string(payload.entityId), LogCategory::Engine);
                
                // 暫定：即座に完了を通知してAIを復帰させるテスト
                // 本来はアニメーションシステム等が完了時にこれを呼ぶ
                MonoScriptEngine::GetInstance().NotifyEventCompleted(payload.entityId, payload.eventName);
            }
            else if (event.type == EventType::Attack)
            {
                const auto& payload = std::get<AttackEventPayload>(event.payload);
                std::string msg = "[AttackEvent] Entity " + std::to_string(payload.ownerId) + 
                    " Spawn Attack: Damage=" + std::to_string(payload.damage) + 
                    ", Radius=" + std::to_string(payload.radius) + 
                    ", Offset=(" + std::to_string(payload.offsetForward) + ", " + std::to_string(payload.offsetUp) + ")";
                Console::Log(msg, LogCategory::Application);

                // ここで HitboxSystem->Create(payload) 等を呼び出す
            }
            else
            {   
                // 現在はデバッグ用にログ出力するだけ
                std::string logMessage = "Processing Event Type: " + std::to_string(static_cast<int>(event.type));
                
                if (std::holds_alternative<EntityEventPayload>(event.payload))
                {
                    logMessage += ", EntityID: " + std::to_string(std::get<EntityEventPayload>(event.payload).entityId);
                }

                Console::Log(logMessage, LogCategory::Engine);
            }
        }
    }
}

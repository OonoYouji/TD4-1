#include "FrameEventQueue.h"
#include "Engine/Core/Utility/Utility.h"
#include "Engine/Script/MonoScriptEngine.h"
#include "GameEventData.h"
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

    void FrameEventQueue::EnqueueAttackEvent(const std::string& attackName, int32_t ownerId, float damage, float radius, float duration, float offsetForward, float offsetUp) {
        Event event;
        event.type = EventType::Attack;
        AttackEventPayload payload;
        payload.attackName = attackName;
        payload.ownerId = ownerId;
        payload.damage = damage;
        payload.radius = radius;
        payload.duration = duration;
        payload.offsetForward = offsetForward;
        payload.offsetUp = offsetUp;
        event.payload = payload;
        GetInstance().Enqueue(event);
    }

    void FrameEventQueue::EnqueueEffectEvent(const std::string& effectName, int32_t entityId, float scale, float duration) {
        Event event;
        event.type = EventType::Effect;
        EffectEventPayload payload;
        payload.effectName = effectName;
        payload.entityId = entityId;
        payload.scale = scale;
        payload.duration = duration;
        event.payload = payload;
        GetInstance().Enqueue(event);
    }

    void FrameEventQueue::EnqueueNamedEvent(const std::string& eventName, int32_t entityId) {
        Event event;
        event.type = EventType::NamedEvent;
        NamedEventPayload payload;
        payload.eventName = eventName;
        payload.entityId = entityId;
        event.payload = payload;
        GetInstance().Enqueue(event);
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
                
                // --- 追加：特定のイベントに対する処理 ---
                if (payload.eventName == "ShowIndicator_Line") {
                    // C#側の InternalCreateEntity と同等の処理をここで行うのが理想的
                    // 現在はログ出力のみだが、ここにプレハブ生成コードを追加する
                    Console::Log("[Telegraph] Spawning TelegraphLine for Entity: " + std::to_string(payload.entityId));
                }

                // 暫定：即座に完了を通知してAIを復帰させるテスト
                // 本来はアニメーションシステム等が完了時にこれを呼ぶ
                MonoScriptEngine::GetInstance().NotifyEventCompleted(payload.entityId, payload.eventName);
            }
            else if (event.type == EventType::Attack)
            {
                auto payload = std::get<AttackEventPayload>(event.payload);
                
                // プリセット名が指定されていれば、マネージャーからデータを取得して上書きする
                if (!payload.attackName.empty()) {
                    if (auto* def = GameEventManager::GetInstance().GetAttack(payload.attackName)) {
                        payload.damage = def->damage;
                        payload.radius = def->radius;
                        payload.duration = def->duration;
                        payload.offsetForward = def->offsetForward.z; // TODO: マネージャー側も正規化が必要
                        payload.offsetUp = def->offsetUp.y;
                    }
                }

                std::string msg = "[AttackEvent] Entity " + std::to_string(payload.ownerId) + 
                    " (" + payload.attackName + ") Spawn Attack: Damage=" + std::to_string(payload.damage) + 
                    ", Radius=" + std::to_string(payload.radius);
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

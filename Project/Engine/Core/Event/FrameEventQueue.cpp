#include "FrameEventQueue.h"
#include "Engine/Core/Utility/Utility.h"
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
            // TODO: ここでイベントディスパッチャを介して、
            //       適切なリスナー/システムにイベントを配送する
            
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

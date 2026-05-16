#pragma once

#include <variant>
#include <cstdint>

namespace ONEngine
{
    // イベントのペイロード（具体的なデータ）を定義
    // 例として、エンティティIDを持つイベント
    struct EntityEventPayload
    {
        int32_t entityId;
    };

    // 必要に応じて他のペイロードをここに追加
    // struct AnotherEventPayload { ... };

    // イベントの種類を定義
    enum class EventType : uint8_t
    {
        TestEvent,
        // ...
    };

    // イベント本体
    // std::variantを使って、型安全なペイロードを持つ
    struct Event
    {
        EventType type;
        std::variant<
            EntityEventPayload
            //, AnotherEventPayload
        > payload;
    };
}

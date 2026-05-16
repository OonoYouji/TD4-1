using System;
using System.Runtime.InteropServices;

public static class AIUpdater {
    /// <summary>
    /// C++ から呼び出されるAI更新のメインエントリーポイント
    /// </summary>
    /// <param name="intentsDataPtr">AgentIntentComponentのネイティブ配列へのポインタ</param>
    /// <param name="entityCount">エンティティの数</param>
    /// <param name="deltaTime">フレーム時間</param>
    public unsafe static void UpdateIntents(AgentIntentComponent.BatchData* intentsDataPtr, int entityCount, float deltaTime) {
        if (intentsDataPtr == null) {
            return;
        }

        // Span<T> を使わずにポインタを直接操作する
        for (int i = 0; i < entityCount; i++) {
            AgentIntentComponent.BatchData* current = intentsDataPtr + i;

            // デバッグ: 受け取った全てのcompIdをログ出力
            Debug.Log($"C# AIUpdater: Processing component index {i}, compId: {current->compId}");

            // 全てのエンティティを右に動かす設定にする
            current->desiredMoveDirection = new Vector3(1, 0, 0);
            current->isAttacking = 1; // true
            
            // 最初の1つ目だけ詳細ログを出す（ログ過多防止）
            if (i == 0) {
                Debug.Log($"C# AIUpdater: Intent applied to compId {current->compId}. Dir: {current->desiredMoveDirection}");
            }
        }
    }
}

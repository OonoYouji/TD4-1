using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class AIUpdater {
    private static readonly Dictionary<uint, AgentIntentComponent> _componentCache = new Dictionary<uint, AgentIntentComponent>();

    /// <summary>
    /// C++ から呼び出されるAI更新のメインエントリーポイント
    /// </summary>
    /// <param name="intentsDataPtr">AgentIntentComponentのネイティブ配列へのポインタ</param>
    /// <param name="entityCount">エンティティの数</param>
    /// <param name="deltaTime">フレーム時間</param>
    /// <param name="groupName">対象のECSグループ名</param>
    public unsafe static void UpdateIntents(AgentIntentComponent.BatchData* intentsDataPtr, int entityCount, float deltaTime, string groupName) {
        if (intentsDataPtr == null) return;

        // キャッシュを更新
        RefreshCache(groupName);

        for (int i = 0; i < entityCount; i++) {
            AgentIntentComponent.BatchData* nativeData = intentsDataPtr + i;

            if (_componentCache.TryGetValue(nativeData->compId, out var component)) {
                // 安全策：Entityが未設定またはIDが無効な場合はスキップ
                if (component.entity == null || component.entity.Id == 0) {
                    continue;
                }

                // ビヘイビアツリーを実行
                if (component.behaviorTree != null) {
                    component.behaviorTree.Tick();

                    // エディタ用：実行状態を同期
                    component.behaviorTree.GetAllNodeStatuses(new Dictionary<uint, NodeStatus>());

                    // ツリーの実行結果（インテント）をネイティブデータに反映
                    nativeData->desiredMoveDirection = component.desiredMoveDirection;
                    nativeData->isAttacking = (byte)(component.isAttacking ? 1 : 0);
                    nativeData->targetEntityId = component.targetEntityId;
                }
 else {
                    // ツリーがない場合は停止を意図する
                    nativeData->desiredMoveDirection = Vector3.zero;
                    nativeData->isAttacking = 0;
                }
            } else {
                // コンポーネントが見つからない場合も停止
                nativeData->desiredMoveDirection = Vector3.zero;
            }
        }
    }

    private static void RefreshCache(string groupName) {
        var group = EntityComponentSystem.GetECSGroup(groupName);
        if (group == null) return;

        var array = group.componentCollection.GetArray<AgentIntentComponent>();
        if (array == null) return;

        _componentCache.Clear();
        foreach (var comp in array.components) {
            if (comp != null) {
                _componentCache[comp.compId] = comp;
            }
        }
    }
}

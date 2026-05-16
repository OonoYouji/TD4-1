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

                // サンプル：ツリーがなければ作成（検証用）
                if (component.behaviorTree == null) {
                    var root = new Sequence(
                        new MoveToPlayerNode(3.0f),
                        new InvokeEventNode(FrameEvent.Type.TestEvent, true, 2.0f)
                    );
                    // 簡易的なハッシュ割り当て
                    root.NodeIdHash = 1;
                    ((CompositeNode)root).GetChildren()[0].NodeIdHash = 2;
                    ((CompositeNode)root).GetChildren()[1].NodeIdHash = 3;

                    component.InitBehaviorTree(root);
                }

                // ビヘイビアツリーを実行
                if (component.behaviorTree != null) {
                    component.behaviorTree.Tick();
                }

                // ツリーの実行結果（インテント）をネイティブデータに反映
                nativeData->desiredMoveDirection = component.desiredMoveDirection;
                nativeData->isAttacking = (byte)(component.isAttacking ? 1 : 0);
                nativeData->targetEntityId = component.targetEntityId;
            }
        }
    }

    private static void RefreshCache(string groupName) {
        var group = EntityComponentSystem.GetECSGroup(groupName);
        if (group == null) return;

        var array = group.componentCollection.GetArray<AgentIntentComponent>();
        _componentCache.Clear();
        foreach (var comp in array.components) {
            _componentCache[comp.compId] = comp;
        }
    }
}

# エフェクトシステム連携仕様書

このドキュメントでは、AI（Behavior Tree）から発行されるエフェクトイベントと、スポーンされるプレハブ（演出実体）間の連携ルールを定義します。

## 1. 概要
AIは演出のトリガー（スポーン）のみを担当し、その後の「発生・追従・消滅・AIへの完了通知」はプレハブ側にアタッチされたスクリプトが担当します。

## 2. プレハブ側の責務
エフェクト用プレハブには `EffectLifecycleHandler.cs`（仮）をアタッチし、以下の制御を行います。

### 2.1 ライフサイクル管理
*   **自動消滅**: `TimedDestruction` またはパーティクル終了検知によるエンティティの破棄。
*   **完了通知**: 消滅の直前、または特定のタイミングで Blackboard に対して `EventComplete_{EventName}` フラグを true に設定し、AIの待機状態を解除します。

### 2.2 トランスフォーム同期
*   **初期化**: 生成時に発行元（ボス）の Position / Rotation を継承します。
*   **追従（Parenting）**: プレハブの設定により、ボスの移動に合わせて追従するか、その場に留まるかを決定します。

## 3. AI（C#）側の責務
*   `InvokeEventNode` または `FrameEvent.EnqueueEffectEvent` を使用して、指定したプレハブをスポーンさせます。
*   `waitUntilComplete` が true の場合、Blackboard のフラグが書き換わるまで Running 状態を維持します。

## 4. エンジン（C++）側の責務
*   `FrameEventQueue` は、エフェクト生成時に `owner` の Rotation を自動的にコピーして生成します（向きの同期不足を解消）。

## 5. 運用ルール
*   **名前付きイベントの命名規則**: `Effect_` で始めることを推奨（例: `Effect_BossRoar`）。
*   **完了通知が必要な場合**: `EffectLifecycleHandler` の `notifyAI` フラグを有効にし、`eventName` を AI 側と一致させます。

# Animation System 拡張タスクリスト (改訂版 v3)

## Phase 1: データ構造の分離と Animator コンポーネントの基礎
アニメーションデータを独立させ、単一アニメーションの切り替えができる土台を作ります。

- [x] **AnimationClip アセットの実装**
    - [x] C++: `NodeAnimation` を束ねた `AnimationClip` 構造体の定義 ( `Engine/Asset/Assets/Mesh/Skinning.h` 周辺)
    - [x] C++: `Model` ロード時にアニメーションデータを `AnimationClip` として抽出・保持するよう修正
    - [x] C++: `AnimationClip` 内へのイベントデータ保持構造の追加
- [x] **Animator コンポーネントの実装**
    - [x] C++: `Animator` コンポーネントクラスの新規作成 (`Engine/ECS/Component/Components/ComputeComponents/Animator/Animator.h`)
    - [x] C++: **重要：クラスメンバとしてフラットな固定長配列によるレイヤー/ステート管理を実装 (DoD最適化)**
    - [ ] C++: `Animator` コンポーネントのシリアライズ (json) 対応
    - [ ] C#: `Animator` クラスの新規作成 (`SubProjects/CSharpLibrary/Scripts/Engine/ECS/Components/Compute/Animator.cs`)
- [x] **AnimatorUpdateSystem の実装**
    - [x] C++: `AnimatorUpdateSystem` の新規作成。`SkinMeshRenderer` の `Skeleton` に対してジョイントトランスフォームを書き込む
- [ ] **基本 API とデータ連携**
    - [ ] C++: `Animator` 用の InternalCall 実装 (`Play(AnimationID)`, `Stop` 等)
    - [ ] C#: `StringHash` ユーティリティの実装（文字列からのハッシュ取得をキャッシュ化）

## Phase 2: クロスフェード (Animation Blending) と品質向上
アニメーション間の滑らかな遷移と、計算精度の担保を実装します。

- [x] **AnimationState の導入**
    - [x] C++: `Animator` 内部に固定長配列によるステート管理を実装
- [x] **ブレンディングロジックの実装**
    - [x] C++: `AnimatorUpdateSystem` で `AnimationState` をサンプリングし、補間する
    - [x] C++: **重要：ブレンド後の Quaternion 正規化 (Normalize) 処理の追加 (歪み防止)**
    - [ ] C++: SIMD (DirectXMath) を活用した補間計算の最適化検討
- [ ] **クロスフェード API の追加**
    - [ ] C++: `CrossFade(clipId, duration)` の内部ロジック実装
    - [ ] C#: `Animator.CrossFade` メソッドの追加

## Phase 3: レイヤー機能と BoneMask の実装
部位ごとのアニメーション合成（上半身だけ攻撃など）を可能にします。

- [x] **AnimationLayer の実装**
    - [x] C++: `Animator` が複数の再生レイヤーを持てるように拡張
- [ ] **BoneMask の実装**
    - [ ] C++: `BoneMask` データ構造と JSON ロード対応
- [x] **レイヤー合成ロジックの実装**
    - [x] C++: レイヤーごとの BoneMask 重みを適用した合成処理 (基礎実装済み)
    - [x] C++: **合成後の最終ポーズに対する Quaternion 正規化の徹底**


## Phase 4: ゲームプレイ連携 (Events & Root Motion)
ゲームロジックとの高度な連携機能を実装します。

- [ ] **アニメーションイベントシステム**
    - [ ] C++: **重要：ループ跨ぎ (ラッピング) を考慮したイベント検出ロジックの実装**
    - [ ] C++: `FrameEventQueue` へのイベント発行処理
- [ ] **ルートモーションの実装**
    - [ ] C++: ルートボーンの Delta トランスフォーム抽出ロジック
    - [ ] C++: `AgentIntentComponent` への移動量出力連携

## Phase 5: ワークフロー最適化
開発効率を向上させるツール・APIを整備します。

- [ ] **C# 定数生成ツールの検討**
    - [ ] Tool: アニメーション名から `AnimationIDs.cs` を自動生成するスクリプトまたはインポーター拡張
- [ ] **デバッグ表示の強化**
    - [ ] C++: ImGui 上での現在の再生レイヤー、ウェイト、イベント発火状況の可視化

# ONEngine ParticleSystem 開発タスクリスト

このタスクリストは、Unity Shuriken互換の新しいGPU駆動パーティクルシステムを実装するためのステップを定義します。

## Phase 1: データ構造と基本コンポーネント (C++)
- [x] `MinMaxFloat`, `MinMaxColor` 等の共通ユーティリティ構造体の作成。
- [x] `ParticleSystem` コンポーネントの基本クラスの作成（`IComponent` 継承）。
- [x] 各モジュールの構造体を定義 (`MainModule`, `EmissionModule`, `ShapeModule` 等)。
- [x] `ComponentJsonConverter` に JSON シリアライズ/デシリアライズ処理を追加。

## Phase 2: Inspector (エディタUI) の実装
- [x] `ImGuiMath` または `InspectorWindow` に `ParticleSystemDebug` 関数を作成。
- [x] Unityライクなトグル付きの CollapsingHeader を実現する ImGui カスタム関数の作成。
- [x] `MinMaxFloat` などを編集するための専用 ImGui ウィジェットの実装。
- [x] 各モジュールのパラメータ設定 UI の構築。

## Phase 3: CPU シミュレーションと描画の実装
- [x] CPU側のパーティクル状態管理 (struct Particle) の作成。
- [x] `ParticleSystemUpdateSystem` (CPU) の実装: 移動、寿命、発生ロジック。
- [x] `ParticleSystemRenderingPipeline` (CPUベース): 動的な頂点バッファ更新によるビルボード描画。
- [x] 基本的なモジュール（Emission, Shape）のCPU版実装。

## Phase 4: GPU シミュレーション基盤への移行 (HLSL & DirectX12)
- [ ] パーティクルデータ用の構造体定義 (`struct Particle`) のGPU版。
- [ ] `StructuredBuffer` (Pool), `Append/Consume Buffer` (Dead/Alive list) の生成・管理。
- [ ] `Emit.hlsl` / `Update.hlsl` への移植。

## Phase 5: 高度なモジュールの統合
- [ ] Color / Size over Lifetime の実装（CPU -> GPU）。
- [ ] 衝突判定やテクスチャアニメーションの追加。

## Phase 6: C# API と移行

- [ ] 古い `Effect` コンポーネントを使用しているシーンやプレハブを新しい `ParticleSystem` に置き換え。
- [ ] 古いコードの削除とクリーンアップ。

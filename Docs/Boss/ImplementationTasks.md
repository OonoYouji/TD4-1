# ボス実装タスクリスト

ボスキャラクター仕様書 (v1) に基づく実装ステップを整理しました。

---

## フェーズ 1: 基盤システムと共通機能
ボスの基本生命維持と、攻撃の「予兆」を表示するための共通機能を構築します。

- [x] **HP・フェーズ管理の実装**
- [x] **予兆・視覚フィードバックシステムの構築** (C++/C#連携)
- [x] **イベントシステムの拡張** (New!)
    - [x] `EventType::Effect` の追加（パーティクル・視覚演出用）。
    - [x] `GameEvents.json` でのダメージ・エフェクトパラメータの一括管理。

## フェーズ 3: 攻撃アクションの個別実装 (SubBTによるモジュール化)
各攻撃パターンを独立した SubBT (`Assets/AITrees/BossAttacks/*.json`) として実装し、プランナーが個別に調整・再利用できるようにします。

- [x] **攻撃 1: ビーム攻撃 (`Attack_Beam.json`)**
- [x] **攻撃 2: 詰まらせ攻撃 / 巨大化 (`Attack_Giant.json`)**
- [x] **攻撃 3: 爆弾攻撃 / 回転投擲 (`Attack_Bomb.json`)**
- [x] **攻撃 4: 岩持ち上げ攻撃 (`Attack_Rock.json`)**
- [x] **攻撃 5: 寄せ攻撃 / 吸引 (`Attack_Vortex.json`)**

## フェーズ 4: ボスの組み立てと調整
すべてのパーツを組み合せて、一つの強敵として仕上げます。

- [x] **BehaviorTree の構築**
    - [x] `BossMain.json` の作成。
    - [x] `Parallel` ノードを使用し、移動と攻撃が同時進行するツリーを構築。
    - [x] フェーズ遷移ロジックの接続。
- [ ] **パラメータの外部化と調整**
    - [ ] `Variables` コンポーネントを使用し、フェーズごとの速度、攻撃頻度、回復量を調整可能にする。
    - [ ] Ctrl+S による保存機能の動作確認。
- [ ] **最終ブラッシュアップ**
    - [ ] エフェクト、SE、カメラシェイクなどの演出追加。

---

## 完了済みの項目
- [x] ボス仕様書のドキュメント化 (`Docs/Boss/BossSpecification_v1.md`)
- [x] 援軍密集地索敵サービス (`ReinforcementDensitySensingService.cs`)
- [x] 巡回移動ノードの基礎 (`PatrolWaypointsNode.cs`)
- [x] HP自動回復サービス (`HPRegenService.cs`)
- [x] C# からのエンティティ全走査機能 (`ECSGroup::GetEntities`)
ties`)

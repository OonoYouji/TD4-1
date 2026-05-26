# ボスキャラクター仕様書 (v1)

このドキュメントは「ボス仕様書v1.pdf」の解析結果と、それを実現するためのビヘイビアツリー（BT）設計案をまとめたものです。

---

## 1. 概要
*   **当たり判定**: 正方形の衝突判定。
*   **HPシステム**: 常に回復し続ける。プレイヤーの攻撃頻度が回復量を上回る必要がある。
*   **フェーズ構成**: HP減少に伴い、第1〜第3フェーズへと遷移。パラメータと攻撃パターンが強化される。
*   **行動設計**: 「移動」と「攻撃」を独立したパターンとして定義し、並列実行を可能にする。

## 2. ビヘイビアツリー・ノード設計

仕様を実現するために必要となる、ボスの専用ノード群です。

### 2.1 サービス (常時実行・監視)
*   **`HPRegenService`**: 
    *   1秒あたりの回復量に基づき、ボスの `CurrentHP` を更新し続ける。
*   **`ReinforcementDensitySensingService`**: 
    *   フィールド上の援軍を走査し、最もユニットが密集している地点を特定。座標を Blackboard の `TargetPosition` に書き込む。
*   **`PhysicsPushService`**: 
    *   移動中に実行。進行方向の「岩」を検知し、ボスの速度を維持したまま押し退ける物理演算を適用する。

### 2.2 デコレーター (条件判定)
*   **`BossPhaseDecorator`**: 
    *   `CurrentHP` の割合を監視し、現在のフェーズが条件に合致するか判定する。

### 2.3 アクションノード (具体的行動)
*   **`PatrolWaypointsNode`**: 
    *   指定された座標リスト（1→2→3→4）を順番に巡回する。
*   **`UpdateTargetTrackerNode`**: 
    *   照射待機中などに実行。`TargetPosition` を最新の密集地に追い越し、ボスの向きを調整する。
*   **`ShowLaserPreviewNode`**: 
    *   ビーム攻撃の予測線（ラインレンダラー）を表示する。
*   **`ScaleUpAreaEntitiesNode`**: 
    *   ボス周辺の円範囲内にいる援軍の Scale を拡大し、渋滞を引き起こす。
*   **`RotateAndSpawnProjectileNode`**: 
    *   ボスを回転させながら、指定間隔・指定距離で爆弾を生成する。
*   **`PickRandomRockNode`**: 
    *   フィールド上の岩をランダムに1つ選び、持ち上げ状態に移行させる。
*   **`DropObjectAtTargetNode`**: 
    *   `TargetPosition` に向かって、持ち上げたオブジェクトを落下させる。
*   **`SpawnVortexFieldNode`**: 
    *   ボス周囲に等間隔で吸引フィールド（竜巻）を生成する。

---

## 3. ビヘイビアツリー構造案 (論理構成)

```text
Root
└── Parallel (ボスの基盤システム)
    ├── HPRegenService (常時回復)
    ├── ReinforcementDensitySensingService (常時ターゲット索敵)
    └── Selector (フェーズ分岐)
        ├── [Decorator: Phase 3 (HP < 30%)]
        │   └── Parallel (移動と激しい攻撃の並列)
        │       ├── PatrolWaypointsNode (高速巡回)
        │       └── Selector (高頻度攻撃シーケンス)
        ├── [Decorator: Phase 2 (HP < 70%)]
        │   └── ...
        └── [Decorator: Phase 1 (Default)]
            └── Parallel (移動と通常攻撃の並列)
                ├── PatrolWaypointsNode (通常巡回)
                └── Sequence (攻撃ループ)
                    ├── PickRandomRockNode (岩持ち上げ)
                    ├── ShowLaserPreviewNode (予兆)
                    ├── WaitNode (狙い定め)
                    ├── FireBeamNode (ビーム照射)
                    └── WaitNode (インターバル)
```

---

## 4. 実装のステップ
1.  **ターゲット索敵 (`ReinforcementDensitySensingService`)**: ボスの攻撃が正確に「密集地」を狙えるようにする。
2.  **巡回移動 (`PatrolWaypointsNode`)**: ボスの基本移動ロジックと、岩の押し退け物理を実装。
3.  **フェーズ管理 (`BossPhaseDecorator`)**: HPに応じた行動変化の基盤を作る。
4.  **各種攻撃ノードの個別実装**: ビーム、爆弾、岩落とし等を順次追加。

---

## 5. 補足：Eventシステムとの連携
*   `InvokeEventNode` を使用して、C++側の演出（カメラシェイク、特殊エフェクト、サウンド）をトリガーする。
*   ビーム発射や爆弾投擲の開始・終了タイミングをイベントで通知し、AIと同期させる。

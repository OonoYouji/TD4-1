# ONEngine ParticleSystem 仕様・使用ガイド

## 1. 概要
ONEngineのパーティクルシステムは、Unityの「Particle System (Shuriken)」と互換性のある使用感を目指して設計されています。現在は**CPUベース**でシミュレーションが行われており、数千〜数万程度のパーティクルを効率よく制御できます。

## 2. 現在の仕様 (v0.8)

### 出来ること (機能)
*   **メインモジュール (Main)**:
    *   寿命 (`Duration`), ループ (`Looping`), 開始遅延 (`Start Delay`).
    *   開始時のパラメータ: `Start Lifetime`, `Start Speed`, `Start Size`, `Start Rotation`, `Start Color`.
    *   重力影響 (`Gravity Modifier`).
    *   シミュレーション空間 (`Local` / `World`): 追従するか、空間に残るかを選択可能。
*   **放出 (Emission)**:
    *   時間経過による放出 (`Rate over Time`).
    *   バースト (`Bursts`): 特定のタイミングで大量に放出。確率指定も可能。
*   **形状 (Shape)**:
    *   Sphere, Hemisphere, Box, Cone, Circle, Edge をサポート。
    *   それぞれの形状に合わせたパラメータ (Radius, Angle, BoxScale等) の調整が可能。
*   **各寿命期間内モジュール (Over Lifetime)**:
    *   **Velocity over Lifetime**: 移動速度の変化 (Linear, Speed Modifier).
    *   **Color over Lifetime**: 時間経過による色の変化 (Gradient).
    *   **Size over Lifetime**: 時間経過によるサイズの変化 (Curve).
*   **レンダラー (Renderer)**:
    *   ビルボード (Billboard) および メッシュ (Mesh) 描画をサポート。
    *   ブレンドモード (Add, Normal等) の切り替えが可能 (現在は Add がデフォルト推奨)。
    *   テクスチャ/マテリアルの指定 (GUIDベース)。

### 出来ないこと (制限事項)
*   **GPUシミュレーション**: 現在はCPUで計算しているため、数十万単位のパーティクルは処理落ちの原因になります。
*   **高度な機能**: 衝突判定 (`Collision`), トレイル (`Trails`), サブエミッター (`Sub Emitters`) は未実装です。
*   **距離による放出 (`Rate over Distance`)**: 移動距離に応じた放出は現在機能しません。
*   **プリウォーム (`Prewarm`)**: 再生開始直後の状態（既に飛んでいる状態）からの開始は出来ません。

## 3. 使い方

### エディタでの設定
1.  GameEntity に `ParticleSystem` コンポーネントを追加します。
2.  Inspectorに Unity の Shuriken と同様の UI が表示されます。
3.  各モジュールのヘッダー左側にあるチェックボックスをオンにすることで、その機能を有効化できます。
4.  **MinMax設定**: パラメータ名の横の▼ボタンから `Constant` (固定値) や `Random Between Two Constants` (2つの値のランダム) を切り替えられます。

### スクリプトからの制御 (C++)
`ParticleSystem` コンポーネントを取得して、以下の関数で制御できます。

```cpp
auto* ps = entity->GetComponent<ParticleSystem>();
ps->Play();   // 再生開始
ps->Pause();  // 一時停止
ps->Stop();   // 停止（放出を止め、残っているパーティクルは消えない）
ps->Reset();  // リセット（全て消して最初から）
```

## 4. パラメータ詳細 (Inspector項目)

### Main モジュール (基本設定)
システム全体の根本的な挙動を設定します。
*   **Duration**: 1サイクルの長さ（秒）。
*   **Looping**: チェックを入れると、Durationが終了した後に最初から繰り返します。
*   **Start Delay**: 再生開始から実際に放出が始まるまでの待ち時間。
*   **Start Lifetime**: パーティクルが生成されてから消えるまでの時間。
*   **Start Speed**: 生成時の初速。
*   **Start Size**: 生成時の大きさ。
*   **Start Rotation**: 生成時の回転角（Z軸まわり）。
*   **Start Color**: 生成時の色。
*   **Gravity Modifier**: 重力の影響度。1.0で標準の重力、マイナス値で浮上します。
*   **Simulation Space**:
    *   `Local`: 親の移動・回転にパーティクルが追従します。
    *   `World`: 生成された瞬間に親から離れ、世界の座標軸で独立して動きます。
*   **Max Particles**: 同時に存在できるパーティクルの最大数。
*   **Play On Awake**: エンティティが生成された瞬間に自動再生するかどうか。

### Emission モジュール (放出)
「いつ・どれだけ」出すかを設定します。
*   **Rate over Time**: 1秒間に放出するパーティクル数。
*   **Bursts**: 特定の瞬間に「ドバッ」と出す設定。
    *   `Time`: Duration内のどのタイミングで出すか。
    *   `Count`: 放出する数。
    *   `Cycles`: 繰り返す回数。
    *   `Interval`: サイクル間の間隔。
    *   `Probability`: 放出される確率（0.0〜1.0）。

### Shape モジュール (形状)
「どこから・どの方向に」出すかを設定します。
*   **Shape**: 放出源の形（Sphere, Box, Coneなど）。
*   **Radius / Scale**: 形状の大きさ。
*   **Radius Thickness**: 表面から出すか(1.0)、内部からも出すか(0.0)の設定。
*   **Arc**: 生成する範囲の角度（360度で全周、180度で半円など）。
*   **Angle**: Cone（円錐）の広がりの角度。

### Velocity over Lifetime モジュール
生存期間中の速度変化を設定します。
*   **Linear X / Y / Z**: 各軸方向への追加速度。
*   **Speed Modifier**: 元の速度に対する倍率変化。
*   **Space**: この速度変化を `Local` か `World` どちらの軸で行うか。

### Color / Size over Lifetime モジュール
*   **Color**: 時間経過に伴う色の変化。グラデーション設定が可能です。
*   **Size**: 時間経過に伴うサイズの変化。カーブエディタで「最初は小さく、徐々に大きく」といった設定が可能です。

### Renderer モジュール (描画)
見た目の最終出力を設定します。
*   **Render Mode**:
    *   `Billboard`: 常にカメラを向く平面。
    *   `Mesh`: 指定した3Dモデルを表示。
*   **Material**: 使用するマテリアル（テクスチャ）の指定。
*   **MinMax指定について**:
    多くの項目（Speed, Size, Color等）の横にある▼をクリックすると、**Random Between Two Constants** を選択できます。これにより「サイズを1.0〜2.0の間でランダムにする」といった自然なゆらぎを簡単に作れます。

### ボスのエフェクト作成へのヒント
*   **予兆 (Telegraph)**: Shapeを `Circle` や `Cone` にし、`Rate over Time` を高く、`Start Speed` を 0 にすることで、攻撃範囲を示す予兆エフェクトとして利用できます。
*   **弾幕**: `Emission` の `Bursts` を使い、`Simulation Space` を `World` に設定することで、ボスの移動に影響されない弾幕を配置できます。
*   **溜めエフェクト**: `Shape` を `Sphere` にし、`Velocity over Lifetime` で中心に向かうような速度を与えることで、エネルギーの溜め表現が可能です。

---
*最終更新日: 2026年6月1日*

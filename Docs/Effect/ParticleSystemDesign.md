# ONEngine ParticleSystem (Unity Shuriken互換) 設計書

## 1. 概要
本設計は、現在のCPUベースの `Effect` コンポーネントを廃止し、Unityの「Particle System (Shuriken)」と**全く同じエディタ使用感（UX）**を持つ、GPU駆動の新しいパーティクルシステムを一から構築するためのものです。

**目標:**
1.  **UXの完全再現:** Unityと同じモジュール単位（Main, Emission, Shape, Color over Lifetime等）のパラメータ構成と、チェックボックスによる有効化/無効化UIを提供する。
2.  **パフォーマンス:** 内部的には Compute Shader を用いた GPU Driven アーキテクチャを採用し、数万～数十万のパーティクルを高速に処理する。
3.  **拡張性:** 各モジュールは独立しており、将来的に「Sub Emitters」や「Trails」などを追加しやすい構造にする。

---

## 2. データ構造設計 (C++ / ECS)

UnityのShurikenアーキテクチャに倣い、メイン設定と複数のオプショナルなモジュールで構成します。

### 2.1 乱数表現 (MinMax)
Unity特有の「定数」「2つの定数間のランダム」を表現するための共通構造体。

```cpp
enum class MinMaxState { Constant, RandomBetweenTwoConstants };

struct MinMaxFloat {
    MinMaxState state = MinMaxState::Constant;
    float constant = 0.0f;
    float constantMin = 0.0f;
    float constantMax = 1.0f;
};

struct MinMaxColor { /* ... */ };
struct MinMaxGradient { /* ... */ };
struct MinMaxCurve { /* ... */ };
```

### 2.2 モジュール構造体
各モジュールは独立した構造体として定義され、それぞれが `enabled` フラグを持ちます。

1.  **Main Module (必須)**
    *   `Duration`, `Looping`, `Prewarm`
    *   `Start Delay`, `Start Lifetime`, `Start Speed`, `Start Size`, `Start Rotation`, `Start Color`
    *   `Gravity Modifier`, `Simulation Space` (Local / World)
    *   `Max Particles`
2.  **Emission Module**
    *   `Rate over Time`, `Rate over Distance`
    *   `Bursts` (Time, Count, Cycles, Interval)
3.  **Shape Module**
    *   `Shape` (Sphere, Hemisphere, Cone, Box, Circle, Edge 等)
    *   `Radius`, `Angle`, `Scale`
4.  **Velocity over Lifetime Module**
    *   `Linear`, `Orbital` (X, Y, Z の MinMaxCurve)
5.  **Color over Lifetime Module**
    *   `Color` (MinMaxGradient)
6.  **Size over Lifetime Module**
    *   `Size` (MinMaxCurve)
7.  **Renderer Module (描画設定)**
    *   `Render Mode` (Billboard, Stretched Billboard, Mesh)
    *   `Material`, `Sort Mode`

---

## 3. エディタ(Inspector) UI 設計

ImGui を用いて、Unity の Inspector を忠実に再現します。

*   **ヘッダー:** 各モジュールのヘッダー左側にチェックボックスを配置し、モジュールの有効/無効を切り替える。
*   **展開:** ヘッダーをクリックして詳細パラメータを展開。
*   **MinMaxUI:** パラメータの右端にある▼ボタン（またはコンボボックス）から、「Constant」か「Random Between Two Constants」を切り替え、入力フィールドを動的に変化させる。

---

## 4. 内部アーキテクチャ (GPU Driven)

ユーザーに見える設定は Unity 互換ですが、内部はモダンな GPU シミュレーションを行います。

1.  **StructuredBuffer の確保:** `Max Particles` に基づき、GPU 上にパーティクルプールを確保。
2.  **Dead/Alive リスト:** `Append/Consume Buffer` を使用して、空きインデックスと生存インデックスを管理。
3.  **Emit (Compute Shader):** Emission モジュールの設定に基づき、CPUから生成要求（Emit Count）をGPUに送信。GPU側で `Dead List` からインデックスを取り出し、初期化。
4.  **Update (Compute Shader):** 生存パーティクルに対して、有効なモジュール（Velocity, Color, Size over Lifetime等）の計算を適用し、寿命を減らす。
5.  **Draw (Graphics):** `DrawInstancedIndirect` を使用し、`Alive List` にあるパーティクルだけを描画。

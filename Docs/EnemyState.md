# Enemy状態管理

このドキュメントはC#におけるEnemyの実装について記述したものです。

## 場所

中心となる実装は `SubProjects/CSharpLibrary/Scripts/Game/Enemy/` ディレクトリにあります。

## コンポーネント

Enemyは主に3つのスクリプトで構成されています:

### 1. `TestEnemy.cs`

このスクリプトはEnemyの基本的なAIと移動を扱います。

*   **状態:**
    *   **待機/索敵:** プレイヤーが `searchRange` の範囲外にいる時、Enemyは索敵状態になります。色は緑です。
    *   **追跡:** プレイヤーが `searchRange` の範囲内かつ `attackRange` の範囲外にいる時、Enemyはプレイヤーを追跡します。色はカーキ色です。
    *   **攻撃:** プレイヤーが `attackRange` の範囲内にいる時、Enemyは攻撃状態になります。色は赤です。
*   **シリアライズされたフィールド:**
    *   `ENTITY_NAME`: ターゲットとするエンティティの名前 (デフォルト: "Player").
    *   `searchRange`: Enemyがプレイヤーを検知し始める範囲。
    *   `rotateSpeed`: Enemyがプレイヤーの方向を向く回転速度。
    *   `attackRange`: Enemyがプレイヤーを攻撃し始める範囲。
    *   `speed`: Enemyの移動速度。

### 2. `EnemyCollisionHandler.cs`

このスクリプトはEnemyの体力、ダメージ、衝突を管理します。

*   **状態:**
    *   **生存:** Enemyは生きており、ダメージを受けることができます。
    *   **被ダメージ:** Enemyは攻撃を受け、一時的に無敵状態になります (`damageCooldown`)。
    *   **破壊:** `hitpoints` が0以下になると、 `isDestroy` フラグがtrueに設定され、次の `Update` サイクルでエンティティが破壊されます。
*   **シリアライズされたフィールド:**
    *   `MAX_HITPOINTS`: Enemyの最大体力。
    *   `DAMAGE_COOLDOWN_TIME`: ダメージを受けた後の無敵時間。

### 3. `EnemyUIHandler.cs`

このスクリプトはEnemyのUI、特に体力バーを制御します。

*   **機能:**
    *   "HP" という名前の子エンティティを見つけ、その `DissolveMeshRenderer` コンポーネントを取得します。
    *   `EnemyCollisionHandler` によって `OnDamaged` メソッドが呼び出され、 `DissolveMeshRenderer` の `threshold` を更新することで、Enemyの現在の体力を視覚的に表現します。
*   **シリアライズされたフィールド:**
    *   `HP_UI_NAME`: HPバーを表す子エンティティの名前 (デフォルト: "HP")。

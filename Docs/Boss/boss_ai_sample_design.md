# 実戦的ボスAI 構築ガイド：Behavior Tree 実装設計案

このドキュメントでは、ONEngine のビヘイビアツリー（BT）システムの各機能をフル活用した、アクションゲーム向けボスの行動設計案を解説します。

---

## 1. Blackboard (記憶) の定義
まず、AIが判断材料とする変数を定義します。

| 変数名 | 型 | 用途 |
| :--- | :--- | :--- |
| `TargetName` | String | ターゲット（例："Player"）の名前。 |
| `DistanceToPlayer` | Float | プレイヤーとの現在の距離（Serviceで更新）。 |
| `IsPlayerInSight` | Bool | プレイヤーが視界内にいるか（Serviceで判定）。 |
| `IsEnraged` | Bool | 怒り状態フラグ（HP低下などのイベントでON）。 |
| `SuperAttackCooldown` | Bool | 必殺技が使用可能か（Cooldownモジュールで管理）。 |

---

## 2. ビヘイビアツリー構造

**ROOT ENTRY**
└─ **Selector (メインブレイン)**  
　　※ **[Service] SensingService**: 毎フレーム索敵を行い `DistanceToPlayer` 等を自動更新
　　
### ① 必殺技ブランチ (最優先)
*   **Node:** Sequence
*   **[Decorator] LogicDecorator**: (AND: `IsPlayerInSight` && `IsEnraged`)
*   **[Decorator] CooldownDecorator**: (15.0s)
*   **Actions:** 
    1.  `InvokeEventNode` ("SuperAttackStart")
    2.  `WaitNode` (3.0s)

### ② 近接コンボブランチ (射程内)
*   **Node:** Sequence
*   **[Decorator] BlackboardDecorator**: (条件: `DistanceToPlayer` < 3.0, **Abort: Self**)
*   **[Service] DefaultFocusService**: 攻撃中、プレイヤーを自動で向き続ける
*   **[Decorator] LoopDecorator**: (3回ループ)
    *   **Action: `RunBehaviorNode`**: ("Assets/AITrees/SlashAttack.json")
    *   ※ 個別の攻撃モーションをサブツリー化して再利用。

### ③ 追跡ブランチ (発見時)
*   **Node:** Sequence
*   **[Decorator] BlackboardDecorator**: (条件: `IsPlayerInSight` == true, **Abort: Self**)
*   **[Service] DefaultFocusService**: 移動中、プレイヤーの方向へ体を向ける
*   **Action: `MoveToPlayerNode`**

### ④ 巡回ブランチ (非発見時・低優先度)
*   **Node:** Sequence
*   **Action: `RunBehaviorNode`**: ("Assets/AITrees/PatrolPattern.json")
*   ※ 複雑な巡回ルートを別ファイルに分離して管理。

---

## 3. この設計で使用している高度な機能

### Observer Aborts (監視による中断)
近接コンボ（②）や追跡（③）に `Abort: Self` を設定することで、**「追いかけている途中でプレイヤーが射程に入った瞬間に、追跡ノードを中断して即座にコンボへ移行する」** という、UEスタイルのキビキビした挙動を実現します。

### LogicDecorator (論理ゲート)
「怒り状態」かつ「視界内にいる」という、フラグが2つ揃った時だけ発動する必殺技を、コードを書かずにエディタ上の設定だけで構築しています。

### Sub-tree (Run Behavior)
「3連撃の各アクション」や「複雑な巡回AI」を別ツリー (`.json`) に分けることで、メインのツリーが巨大化して煩雑になるのを防ぎ、デバッグもしやすくしています。

### Runtime Watcher (実行時監視)
ゲームを動かしながら、Blackboard パネルの **「Runtime」タブ** を見ることで、「なぜ今ボスが必殺技を打たないのか（まだクールダウン中なのか？視界から外れたのか？）」をリアルタイムで特定できます。

---

## 4. 今後の拡張アドバイス
*   **Random Selector:** 攻撃の種類をランダムに変えたい場合は、優先順位ではなくランダムに子を選ぶ `RandomSelector` ノードを追加してください。
*   **Parallel:** 攻撃しながら別の特殊効果を発生させたい場合は、複数の子を同時に実行する `Parallel` ノードが有効です。

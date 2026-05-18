# 実戦的ボスAI 構築ガイド：Behavior Tree 実装設計案 (v2.0)

このドキュメントでは、ONEngine の最新ビヘイビアツリー（BT）機能をフル活用した、高度なアクションゲーム向けボスの行動設計案を解説します。

---

## 1. Blackboard (記憶) の定義
AIが判断材料とする変数を定義します。

| 変数名 | 型 | 用途 |
| :--- | :--- | :--- |
| `Target` | Entity | 追従・攻撃対象。 |
| `TargetPos` | Vector3 | Targetの現在の座標（SensingServiceで更新）。 |
| `MoveToPos` | Vector3 | 移動の目標地点（SimpleEQSで計算）。 |
| `IsAnger` | Bool | 怒り状態フラグ（HP低下でTrue）。 |
| `DistanceToTarget` | Float | ターゲットとの距離。 |
| `CombatPhase` | Int | 現在のフェーズ（0:巡回, 1:通常, 2:狂暴）。 |
| `SkillCooldown` | Bool | 必殺技のクールダウン中か。 |

---

## 2. 推奨ツリー構造 (フル機能活用版)

最新機能（Parallel, SimpleEQS, Advanced Decorator）を組み合わせた構造例です。

### Root (Selector)
*   **[Phase 2: 狂暴状態] (Sequence)**
    *   *Decorator: Blackboard (CombatPhase == 2, ObserverAborts: Both)*
    *   **狂暴化エフェクト再生 (Task)**
    *   **[並列行動] (Parallel: SuccessPolicy=All)**
        *   **高速追従 (SimpleEQS + MoveTo)**: `angleOffset=180` (常に背後を狙う)。
        *   **連続斬撃 (Task)**: 移動しながら攻撃を繰り出す。
*   **[Phase 1: 戦闘状態] (Selector)**
    *   *Decorator: Blackboard (Target != null, ObserverAborts: Both)*
    *   **[必殺技] (Sequence)**
        *   *Decorator: Blackboard (SkillCooldown == false && DistanceToTarget < 5.0)*
        *   **溜め動作 (WaitRandom: 0.5s~1.0s)**
        *   **回転斬り (Task)**
        *   **クールダウン開始 (Task)**
    *   **[回り込み移動] (Sequence)**
        *   *Decorator: Blackboard (DistanceToTarget > 10.0)*
        *   **目標地点計算 (SimpleEQS)**: ターゲットの横側 (`angleOffset=90`) を指定。
        *   **移動 (MoveToPosNode)**
    *   **[基本攻撃] (Task)**: 近接攻撃を実行。
*   **[Idle/Patrol] (WaitRandom: 2s~4s)**: ターゲットがいない時の待機。

---

## 3. 活用されている高度なテクニック

### ① Parallel による「動きながらの攻撃」
従来のツリーでは「移動」と「攻撃」を順に実行（Sequence）する必要がありましたが、**Parallel ノード**を使用することで、`SimpleEQS` で算出した最新の目標地点へ移動し続けながら、同時に攻撃モーションを再生することが可能になります。これにより「逃げるプレイヤーを追いかけながら切りつける」という執拗な挙動が実現します。

### ② SimpleEQS による「賢いポジショニング」
単にプレイヤーへ直進するのではなく、`SimpleEQSService` を使用して「プレイヤーの横 5m」や「プレイヤーの背後」といった動的な目標地点を Blackboard に書き込みます。`MoveTo` ノードはこの座標を参照するだけでよいため、タスクの責務が分離され、再利用性が高まります。

### ③ Advanced Decorator による「フェーズ遷移」
`BlackboardQuery` の拡張（Equal, GreaterThan等）により、「HPが50%以下ならフェーズ1へ」「怒りフラグが立っているならフェーズ2へ」といった複雑な条件分岐を、C#コードを書かずにエディタ上で完結できます。

### ④ Observer Aborts (Lower Priority) による「割り込み」
プレイヤーが遠くに逃げた際、現在実行中の「近接攻撃」を即座に中断（**OnAbort**）し、上位にある「追いかけ（EQS + MoveTo）」に即座に評価を飛ばすことで、AIの反応速度を飛躍的に向上させています。

---

## 4. デバッグのポイント
*   **Validator の活用**: 
    エディタ上で赤い **[!] アイコン**が出ていないか確認してください。特に Subtree (`RunBehaviorNode`) のパスが正しいか、EQS の出力キーと移動ノードの入力キーが一致しているかをチェックします。
*   **Runtime ハイライト**: 
    Parallel ノード内では複数の子が同時に黄色（Running）になります。どのポリシー（One/All）で完了しようとしているか、各ノードの状態を注視してください。

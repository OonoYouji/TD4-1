# ONEngine AI・イベント連動システム アーキテクチャ設計書 (v2.0)

本ドキュメントは、C++（コア/ECS）とC#（ロジック）のハイブリッド環境における、ボスの自律AI（Behavior Tree）およびステージ連動ギミック（イベント駆動）の統合設計を定義します。実運用における安全性、デバッグ性、パフォーマンス（GC・キャッシュ最適化）を重視した商用基準のアーキテクチャです。

---

## 1. システム全体アーキテクチャと責務（Ownership）

システムは大きく分けて「C++エンジンコア層」「C#アプリケーション層」「イベントバス層」の3つで構成され、データ指向設計と厳密な所有権の分離を前提とします。

*   **C++ エンジンコア層:** ECSメインループ、ImGuiエディター、Monoホスティング、**最終的なTransform（座標・回転）の更新処理**を担当。
*   **C# アプリケーション層:** Behavior Treeの論理処理、Intent（意図）データの出力、ゲーム固有のイベント発行を担当。
*   **通信境界 (Interop):** 毎フレーム1回のC#バッチ呼び出し。エンティティごとのManaged呼び出しは行わない。

---

## 2. C++ / C# 相互運用 (Interop) と Intentの分離

パフォーマンスの維持とECSの原則を守るため、AI（C#）は直接オブジェクトを動かさず、「意図」のみを出力します。

### 2.1 バッチ処理によるオーバーヘッド削減
*   毎フレーム1回、C++からC#の全体更新関数（UpdateAI等）を呼び出す。
*   C++からC#へは、コンポーネントの種類ごとに連続配列（またはポインタ）として一括で渡す。C#側は Span<T> や unsafe を用いて直接メモリを参照し、コピー・ボクシングコストをゼロにする。

### 2.2 Intent（意図）コンポーネントによる所有権の分離
*   **禁止事項:** C#のAI処理内で直接 Transform を書き換えてはいけない。
*   **解決策:** C#のAIは、対象エンティティの AgentIntentComponent（例: DesiredMoveDirection, IsAttacking フラグ等）のみを書き換える。
*   **実行フェーズ:** C#側のAI更新が完了した後、C++側の MovementSystem や AttackSystem がそのIntentを読み取り、実際のTransform更新や物理演算、アニメーション制御を行う。

---

## 3. Behavior Tree (BT) ランタイム設計

大量のエンティティを高速に処理するため、BT構造をフライウェイトパターンで実装し、GCと検索コストを極限まで削減します。

### 3.1 ステートレス設計と高速Blackboard
*   BehaviorNode を継承する全ノードは、内部に状態（変数）を持たない。
*   **Stringキーの廃止:** GC発生と検索遅延を防ぐため、Blackboardのキーには文字列(string)を使用せず、初期化時に計算済みのハッシュ値（uint32 等）、または列挙型（Enum）を使用する。

### 3.2 実行状態（スタック）の保存
*   Sequence 等の制御ノードが Running を返した場合の再開インデックス保存設計。

    // コンパイル時/初期化時に計算済みのハッシュキーを使用（GCゼロ）
    // uint stateKeyHash = this.NodeIdHash; 
    
    int currentIndex = context.Blackboard.GetInt(stateKeyHash, 0);
    
    // ... 子ノードの実行処理 ...
    
    // Running時に状態を保存
    context.Blackboard.SetInt(stateKeyHash, currentIndex); 

---

## 4. イベント駆動型ステージ連携 (Event System)

AIのロジックと、ステージの変化・演出（カメラ揺れ、エフェクト等）を完全にデカップリングします。

### 4.1 Frame Event Queue（フレーム境界での遅延実行）
*   **キューイング:** 即時発火による処理順序のバグ（AI実行中のステージ変化など）を防ぐため、AIノードはイベントをグローバルな EventQueue に積む（Push）だけにする。
*   **フラッシュ:** C#のAIバッチ処理がすべて完了した直後（またはフレーム終端）でキューを一斉に消化し、StageSystemやEffectSystem等の購読者が処理を行う。

### 4.2 フェイルセーフを備えた InvokeEventNode
*   エディターから配置可能な専用のイベント発行ノード。
*   **演出待ち (WaitUntilComplete):** イベントの演出完了（NotifyComplete）までAIを Running 状態で待機させる。
*   **タイムアウト (TimeoutSec):** 演出側の不具合による「AI永久停止」を防ぐため、**必ずタイムアウト秒数を設定する**。超過した場合は強制的に Failure としてAIを復帰させる。

    public string EventId { get; set; }
    public bool WaitUntilComplete { get; set; }
    public float TimeoutSec { get; set; } // 必須：永久停止防止のフェイルセーフ

---

## 5. ノードエディター (ImGui) ＆ 開発ワークフロー

エディターのGUI描画はC++（ImGui）で行い、AIロジックとノードの定義はC#を正（Source of Truth）とします。

### 5.1 ノード定義の自動収集 (Discovery)
*   エディター起動時（または安全なタイミング）に、C++側からMono C APIを用いてC#アセンブリを解析し、BehaviorNode を継承する全クラスを抽出し、ImGuiの右クリックメニューに自動追加する。

### 5.2 デバッグ可視化（最優先実装要件）
AI開発がブラックボックス化するのを防ぐため、実装の初期段階から以下のデバッグ機能をC++(ImGui)側に構築する。
1.  **BTの視覚化:** 現在 Running になっているノード、および直前に Success/Failure になったノードの色を変えて表示する。
2.  **Blackboard Viewer:** 選択したエンティティのBlackboardの現在の値（HP、ターゲット状態、フラグ等）をリアルタイムに一覧表示する。
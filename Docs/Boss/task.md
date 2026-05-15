# ONEngine AI・イベント連動システム 実装仕様書

## 1. データ構造とInterop仕様
*   **AgentIntentComponent (C++ / C# 共通定義)**
    *   **仕様:** Transformを直接操作せず、AIの「意図」を格納するコンポーネント。C#側ではクラス（`class`）として定義する。
    *   **フィールド例:** `Vector3 DesiredMoveDirection`, `bool IsAttacking`, `uint TargetEntityId`
    *   **メモリ・同期レイアウト:** C++側（ネイティブ構造体の連続配列）とC#側（コンポーネントクラスのプールされたインスタンス）でデータ構造を一致させ、GCアロケーションを抑えつつシームレスに値を同期・反映できるようにする。
*   **バッチ更新インターフェース**
    *   **仕様:** C++側から毎フレーム1回のみ呼び出されるC#の静的メソッドを定義。
    *   **引数:** `UpdateAI(IntPtr intentsDataPtr, int entityCount, float deltaTime)` のように、連続したメモリブロックのポインタを渡す。C#側ではアンマネージドメモリからデータを読み取り、対応する `AgentIntentComponent` クラスのインスタンス群へ値をバルク更新（または状態の同期）を行う。

## 2. Behavior Tree (BT) ランタイム仕様
*   **Blackboardの実装**
    *   **仕様:** GCアロケーションをゼロにするため、キーは `uint` (ハッシュ値) を使用する。
    *   **データ格納:** ValueType（int, float, bool, Vector3）用の配列と、参照型（必要な場合）のプールを分離した構造にする。
*   **ステートレス BehaviorNode**
    *   **仕様:** ノード自体は状態を持たないシングルトンまたはフライウェイトとして振る舞う。
    *   **状態保存:** Sequence等の実行インデックスは、初期化時に計算された自身の `NodeIdHash` をキーとして、引数で渡される `Blackboard` (または `ExecutionContext`) に保存・復帰する。

## 3. イベントシステム仕様
*   **Frame Event Queue**
    *   **仕様:** C#側で発行されたイベントを一時的に保持するキュー。AIの評価ループ中は即時実行せず、キューに積むのみ。
    *   **フラッシュ機構:** C#のバッチ処理完了直後にキューのイベントを抽出し、C++のStageSystem等へディスパッチする。
*   **InvokeEventNode のフェイルセーフ**
    *   **仕様:** 演出待ちイベントが完了しない場合のデッドロックを回避するタイマー機構。
    *   **処理:** ノード実行開始時にBlackboardへタイムスタンプを記録。毎フレーム `TimeoutSec` と比較し、超過した場合は強制的に `Failure` を返してノードを終了する。

## 4. エディター (ImGui) 仕様
*   **ノード自動収集 (Reflection)**
    *   **仕様:** Mono C APIを使用して、ロードされたC#アセンブリから `BehaviorNode` を継承するクラス一覧と、公開プロパティ（Serialize対象）を抽出し、C++側のImGuiメニューツリーを構築する。
*   **デバッグ可視化**
    *   **ノード状態:** C#側からデバッグ用に「現在RunningのノードID」「最後にSuccess/Failureを返したノードID」のリストを毎フレーム受け取り、ImGuiのノードグラフ上で色分け描画する。
    *   **Blackboard Viewer:** 選択中エンティティのBlackboard内メモリを読み取り、ImGuiのテーブル（またはKey-Valueリスト）としてリアルタイム表示する。

---

# GeminiCLI向け 実装タスクリスト

### フェーズ1: コア通信とIntentの分離 (C++ / C# Interop)
*   [ ] **Task 1-1:** C++側でECSに組み込むネイティブデータとしての `AgentIntentComponent` 構造体を定義し、同時にC#側でもコンポーネントとして扱うためのクラス（`class`）を定義する。
*   [ ] **Task 1-2:** C++のメインループからC#のAI更新関数をバッチで呼び出すブリッジ処理を実装する。C#側では渡されたポインタ（ネイティブ配列）からデータを一括で読み取り、事前に用意された `AgentIntentComponent` クラスのインスタンス群に対して状態を同期する処理を構築する。
*   [ ] **Task 1-3:** C++側の `MovementSystem` を修正し、Transformを直接変更するのではなく、同期された `AgentIntentComponent` の値を読み取ってTransformを更新するように処理を書き換える。

### フェーズ2: Behavior Tree ランタイムの構築
*   [ ] **Task 2-1:** 文字列キーを排除した、ハッシュ値（`uint`）ベースの高速な `Blackboard` クラスをC#で実装する。
*   [ ] **Task 2-2:** 状態を持たない（ステートレスな） `BehaviorNode` の基底クラスと、実行コンテキスト（Blackboardと対象Entityへの参照を含む）クラスをC#で実装する。
*   [ ] **Task 2-3:** `Sequence` ノードおよび `Selector` ノードを実装する。実行状態（現在実行中の子ノードのインデックスなど）は、自身のハッシュキーを使ってBlackboardに保存・復元するロジックを組み込む。

### フェーズ3: イベント駆動システムとフェイルセーフ
*   [ ] **Task 3-1:** グローバルな `FrameEventQueue` をC#で実装する。イベントをPushする機能と、フレームの終端で全イベントを配列として取り出してクリアするFlush機能を実装する。
*   [ ] **Task 3-2:** AIからイベントを発行する `InvokeEventNode` をC#で実装する。
*   [ ] **Task 3-3:** `InvokeEventNode` にフェイルセーフ処理を追加する。`WaitUntilComplete` が有効な場合、Blackboardに開始時刻を記録し、`TimeoutSec` を経過しても完了通知が来ない場合は `Failure` を返すロジックを実装する。

### フェーズ4: ツール開発・Python/PowerShell連携とImGui統合
*   [ ] **Task 4-1:** Mono C APIを利用して、C++側からC#の `BehaviorNode` 継承クラス一覧を動的に収集するリフレクション処理を実装する。（※必要に応じて、PythonやPowerShellを使ったスクリプトでビルド前処理として登録コードを自動生成する手法も検討）
*   [ ] **Task 4-2:** 収集したノード情報をもとに、ImGui上で右クリックメニューからノードを配置できるベースUIをC++で実装する。
*   [ ] **Task 4-3:** ImGuiのデバッグビューアを実装する。実行中のエンティティを選択した際、C#側から取得した「現在RunningのノードID」をハイライト表示し、Blackboardの中身をリスト表示する機能を追加する。

### フェーズ5: 動作検証とワークフロー自動化
*   [ ] **Task 5-1:** テスト用のGameScene（必要であればDebugSceneと同時起動）を作成し、単純なBehavior Tree（例: プレイヤーに近づき、距離が近ければ攻撃Intentを出し、エフェクトイベントを発行する）を構成して動作を確認する。
*   [ ] **Task 5-2:** 上記の一連の実装が完了し、ビルドエラーおよびメモリリーク（GCの意図しない発生）がないことを確認する。
*   [ ] **Task 5-3:** 各実装ごとにGithubのブランチを作成し、タスク完了とエラーなしを確認した上でマージする。マージ後、DiscordのWebhook経由で通知を送信するスクリプト（Python/PowerShell）を自動実行する。
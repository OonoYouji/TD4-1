using System;

/// <summary>
/// ボスAIを制御するサンプルスクリプト
/// </summary>
public class BossAI : MonoScript {
	[SerializeField]
	public string treePath = "Assets/AITrees/DefaultTree.json";

	private AgentIntentComponent _intent;
	private float _combatTimer = 0.0f;
	private bool _isEnraged = false;

	public override void Initialize() {
		Debug.Log($"BossAI: Initializing for entity {entity.name} (ID:{entity.Id})");
		_intent = entity.GetComponent<AgentIntentComponent>();
		if (_intent == null) {
			_intent = entity.AddComponent<AgentIntentComponent>();
		}

		// エディタで作成したツリーをロード
		Debug.Log($"BossAI: Loading tree from {treePath} for {entity.name}");
		_intent.LoadBehaviorTree(treePath);

		if (_intent.behaviorTree != null && _intent.behaviorTree.RootNode != null) {
			Debug.Log($"BossAI: Successfully loaded tree. Root Node: {_intent.behaviorTree.RootNode.name}");
		} else {
			Debug.LogError($"BossAI: Failed to load tree or RootNode is null! Path: {treePath}");
		}
	}

public override void Update()
{
    // --- 憤怒（Enrage）タイマーの更新 ---
    if (_intent != null && _intent.behaviorTree != null)
    {
        _combatTimer += Time.deltaTime;
        if (!_isEnraged && _combatTimer >= 300.0f)
        {
            _isEnraged = true;
            _intent.behaviorTree.Blackboard.SetBool(BehaviorTreeLoader.HashString("IsEnraged"), true);
            Debug.Log("<color=red>[BossAI] ENRAGED!</color> 300 seconds passed. Boss speed and attack rate increased!");
            
            // 視覚的フィードバック（赤いオーラ等）のイベント発行
            FrameEvent.EnqueueNamedEvent("Effect_BossEnrage", entity.Id);
        }
    }

    // 実際の更新ロジックは AISystem (C++) -> AIUpdater (C#) -> BehaviorTree.Tick() 
    // の流れで一括処理されるため、ここでは何もしなくてよい

    // --- デバッグ用：視線の表示 ---
    GizmoBatch.DrawRay(transform.position + Vector3.up * 2.0f, transform.forward * 5.0f, new Vector4(0, 1, 0, 1));
}}

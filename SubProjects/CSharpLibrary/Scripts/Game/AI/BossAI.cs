using System;

/// <summary>
/// ボスAIを制御するサンプルスクリプト
/// </summary>
public class BossAI : MonoScript {
	[SerializeField]
	public string treePath = "Assets/AITrees/DefaultTree.json";

	private AgentIntentComponent _intent;

	public override void Initialize() {
// 		Debug.Log($"BossAI: Initializing for entity {entity.name} (ID:{entity.Id})");
		_intent = entity.GetComponent<AgentIntentComponent>();
		if (_intent == null) {
			_intent = entity.AddComponent<AgentIntentComponent>();
		}

		// エディタで作成したツリーをロード
// 		Debug.Log($"BossAI: Loading tree from {treePath} for {entity.name}");
		_intent.LoadBehaviorTree(treePath);

		if (_intent.behaviorTree != null && _intent.behaviorTree.RootNode != null) {
// 			Debug.Log($"BossAI: Successfully loaded tree. Root Node: {_intent.behaviorTree.RootNode.name}");
		} else {
// 			Debug.LogError($"BossAI: Failed to load tree or RootNode is null! Path: {treePath}");
		}
	}

	public override void Update() {
		// --- デバッグ用：HキーでHPを10%減らす ---
		if (Input.TriggerKey(KeyCode.H)) {
			var hp = entity.GetScript<HP>();
			if (hp != null) {
				hp.TakeDamage(120); // 1200の10%
// 				Debug.Log($"<color=orange>[Debug]</color> Boss HP reduced. Current: {hp.currentHp}/{hp.MAX_HP} ({hp.CurrentHpRatio() * 100:F1}%)");
			}
		}

		// 実際の更新ロジックは AISystem (C++) -> AIUpdater (C#) -> BehaviorTree.Tick() 
		// の流れで一括処理されるため、ここでは何もしなくてよい

		// --- デバッグ用：視線の表示 ---
		GizmoBatch.DrawRay(transform.position + Vector3.up * 2.0f, transform.forward * 5.0f, new Vector4(0, 1, 0, 1));
	}
}

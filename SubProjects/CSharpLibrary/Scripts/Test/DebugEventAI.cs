using System;

public class DebugEventAI : MonoScript {
	private AgentIntentComponent _intent;

	public override void Initialize() {
		_intent = entity.GetComponent<AgentIntentComponent>();
		if (_intent == null) {
			_intent = entity.AddComponent<AgentIntentComponent>();
		}

		// 検証用のシンプルなツリーを構築
		// 1. Log: Start
		// 2. InvokeEvent: "TestDebugEvent" (WaitUntilComplete = true)
		// 3. Log: Finished

		var root = new Sequence();
		root.AddChild(new LogNode("AI: Starting event test..."));
		root.AddChild(new InvokeEventNode("TestDebugEvent", true, 5.0f));
		root.AddChild(new LogNode("AI: Event test finished successfully!"));

		_intent.InitBehaviorTree(root);

		Debug.Log($"[DebugEventAI] Initialized on {entity.name}. BT is ready.");
	}
}

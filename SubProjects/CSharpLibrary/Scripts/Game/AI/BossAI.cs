using System;

/// <summary>
/// ボスAIを制御するサンプルスクリプト
/// </summary>
public class BossAI : MonoScript
{
    [SerializeField]
    public string treePath = "Assets/AITrees/DefaultTree.json";

    private AgentIntentComponent _intent;

    public override void Initialize()
    {
        Debug.Log($"BossAI: Initializing for entity {entity.name} (ID:{entity.Id})");
        _intent = entity.GetComponent<AgentIntentComponent>();
        if (_intent == null)
        {
            _intent = entity.AddComponent<AgentIntentComponent>();
        }

        // エディタで作成したツリーをロード
        Debug.Log($"BossAI: Loading tree from {treePath}");
        _intent.LoadBehaviorTree(treePath);
    }


    public override void Update()
    {
        // 実際の更新ロジックは AISystem (C++) -> AIUpdater (C#) -> BehaviorTree.Tick() 
        // の流れで一括処理されるため、ここでは何もしなくてよい
    }
}

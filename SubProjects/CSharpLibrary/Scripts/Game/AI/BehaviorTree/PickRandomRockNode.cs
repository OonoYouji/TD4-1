using System;
using System.Collections.Generic;

/// <summary>
/// ステージ上にある「岩（Rock）」タグの付いたエンティティをランダムに一つ選択し、
/// そのエンティティ（Entityオブジェクト）をBlackboardに保存するノード。
/// </summary>
public class PickRandomRockNode : BehaviorNode
{
    [BlackboardKey]
    public string outputRockKey = "TargetRock";

    public string rockTag = "Rock";
    public float searchRadius = 30.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        var entities = owner.Group.GetEntities();
        List<Entity> candidateRocks = new List<Entity>();

        foreach (var entity in entities)
        {
            if (entity.name.Contains(rockTag))
            {
                float dist = Vector3.Distance(owner.transform.position, entity.transform.position);
                if (dist <= searchRadius)
                {
                    candidateRocks.Add(entity);
                }
            }
        }

        if (candidateRocks.Count == 0)
        {
            Debug.LogWarning("[PickRock] No rocks found within radius.");
            return NodeStatus.Failure;
        }

        // ランダムに一つ選択
        Random rand = new Random();
        Entity selectedRock = candidateRocks[rand.Next(candidateRocks.Count)];

        blackboard.SetObject(BehaviorTreeLoader.HashString(outputRockKey), selectedRock);
        Debug.Log($"<color=brown>[PickRock]</color> Selected rock: {selectedRock.name} at {selectedRock.transform.position}");

        return NodeStatus.Success;
    }
}

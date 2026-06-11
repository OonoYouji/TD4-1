using System;
using System.Collections.Generic;

/// <summary>
/// 指定された範囲内にいる援軍などのエンティティを一斉に巨大化させるノード。
/// </summary>
public class ScaleUpAreaEntitiesNode : BehaviorNode {
	[BlackboardKey]
	public string targetPosKey = "TargetPosition";

	public float effectRadius = 15.0f;
	[BlackboardKey]
	public string effectRadiusKey = "";

	public float scaleMultiplier = 1.2f;
	public float duration = 5.0f;

	public float delay = 0.0f;
	[BlackboardKey]
	public string delayKey = "";

	public string targetNameFilter = "";

	protected override NodeStatus Execute(Blackboard blackboard, Entity owner) {
		uint startTimeKey = BehaviorTreeLoader.HashString("ScaleUpStart_" + NodeIdHash);
		uint doneKey = BehaviorTreeLoader.HashString("ScaleUpDone_" + NodeIdHash);
		float currentTime = Time.time;

		float finalRadius = effectRadius;
		if (!string.IsNullOrEmpty(effectRadiusKey)) {
			uint keyHash = BehaviorTreeLoader.HashString(effectRadiusKey);
			if (blackboard.HasKey(keyHash)) finalRadius = blackboard.GetFloat(keyHash, effectRadius);
		}

		float finalDelay = delay;
		if (!string.IsNullOrEmpty(delayKey)) {
			uint keyHash = BehaviorTreeLoader.HashString(delayKey);
			if (blackboard.HasKey(keyHash)) finalDelay = blackboard.GetFloat(keyHash, delay);
		}

		if (!blackboard.HasKey(startTimeKey)) {
			blackboard.SetFloat(startTimeKey, currentTime);
			return NodeStatus.Running;
		}

		float startTime = blackboard.GetFloat(startTimeKey);
		float elapsed = currentTime - startTime;

		// 条件を満たせば拡大実行
		if (elapsed >= finalDelay && !blackboard.HasKey(doneKey)) {
			blackboard.SetBool(doneKey, true);

			// --- 音声の再生 (ワンショット) ---
			var audio = owner.GetComponent<AudioSource>();
			if (audio == null) audio = owner.AddComponent<AudioSource>();
			if (audio != null) {
				audio.OneShotPlay(0.5f, 1.0f, "./Assets/Sounds/MainGameSounds/se/boss/clog.mp3");
			}

			// 範囲内のエンティティを走査
			Vector3 center = blackboard.GetVector3(BehaviorTreeLoader.HashString(targetPosKey));
			var entities = owner.Group.GetEntities();

			foreach (var e in entities) {
				if (e == null || e.Id == owner.Id) continue;

				// --- 修正：吸引と同様、対象を「援軍」に厳格に制限する ---
				// 名前フィルターが空の場合でも、プレイヤーや地面タイルを巨大化させないようにガード
				string ename = e.name;
				bool isReinforcement = ename.Contains("Reinforcement");

				// カスタムフィルターがあればそれも考慮するが、基本は援軍のみ
				if (!string.IsNullOrEmpty(targetNameFilter)) {
					if (!ename.Contains(targetNameFilter)) continue;
				} else {
					if (!isReinforcement) continue;
				}

				float dist = Vector3.Distance(center, e.transform.position);
				if (dist <= finalRadius) {
					// 巨大化処理 (一時的なスケール変更など)
					// ここでは簡易的に Transform を直接操作するか、バフスクリプトを付与する
					//e.transform.scale *= scaleMultiplier;
					e.transform.scale *= 1.2f;

					// エフェクト
					FrameEvent.EnqueueNamedEvent("Effect_GiantScale", e.Id);
				}
			}
		}

		if (elapsed >= finalDelay + 0.1f) {
			blackboard.Remove(startTimeKey);
			blackboard.Remove(doneKey);
			return NodeStatus.Success;
		}

		return NodeStatus.Running;
	}

	public override void OnAbort(Blackboard blackboard, Entity owner) {
		blackboard.Remove(BehaviorTreeLoader.HashString("ScaleUpStart_" + NodeIdHash));
		blackboard.Remove(BehaviorTreeLoader.HashString("ScaleUpDone_" + NodeIdHash));
	}
}

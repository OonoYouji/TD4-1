using System;
using System.Collections.Generic;

/// <summary>
/// 指定された座標に吸引力を発生させるフィールドを生成するノード。
/// 範囲内のエンティティを中心に引き寄せる。
/// 
/// 仕様書v2対応：距離の2乗に反比例する吸引力と、中心部でのダメージ処理を追加。
/// </summary>
public class SpawnVortexFieldNode : BehaviorNode {
	[BlackboardKey]
	public string targetPosKey = "TargetPosition";

	[BlackboardKey]
	public string suctionRadiusKey = "";

	public string vortexPrefab = "VortexField";
	public float suctionRadius = 15.0f;
	public float suctionForce = 100.0f; // 大幅に強化
	public float duration = 5.0f;

	public int centerDamage = 50;
	public float centerDamageRadius = 3.0f;
	public float damageInterval = 0.5f;

	protected override NodeStatus Execute(Blackboard blackboard, Entity owner) {
		uint startTimeKey = BehaviorTreeLoader.HashString("VortexStart_" + NodeIdHash);
		uint entityIdKey = BehaviorTreeLoader.HashString("VortexEntityID_" + NodeIdHash);
		uint damageTimerKey = BehaviorTreeLoader.HashString("VortexDamageTimer_" + NodeIdHash);
		float currentTime = Time.time;

		// 半径の決定
		float finalSuctionRadius = suctionRadius;
		if (!string.IsNullOrEmpty(suctionRadiusKey)) {
			uint keyHash = BehaviorTreeLoader.HashString(suctionRadiusKey);
			if (blackboard.HasKey(keyHash)) {
				finalSuctionRadius = blackboard.GetFloat(keyHash, suctionRadius);
				if (finalSuctionRadius == suctionRadius) finalSuctionRadius = (float)blackboard.GetInt(keyHash, (int)suctionRadius);
			}
		}

		// 1. 開始処理: プレハブの生成
		if (!blackboard.HasKey(startTimeKey)) {
			// ボスの現在位置を吸引の中心点として固定する
			Vector3 spawnPos = owner.transform.position;
			blackboard.SetVector3(BehaviorTreeLoader.HashString("VortexFixedPos_" + NodeIdHash), spawnPos);

			blackboard.SetFloat(startTimeKey, currentTime);
			blackboard.SetFloat(damageTimerKey, 0f);

			Entity vortex = owner.Group.CreateEntity(vortexPrefab);
			if (vortex != null) {
				vortex.parent = null;
				vortex.transform.position = spawnPos;

				// --- 修正：生成したプレハブのパラメータと見た目のサイズを同期する ---
				var script = vortex.GetScript<VortexField>();
				if (script != null) {
					script.suctionRadius = finalSuctionRadius;
					script.suctionForce = suctionForce;
					script.duration = duration;
					script.centerDamage = centerDamage;
					script.centerDamageRadius = centerDamageRadius;
					script.damageInterval = damageInterval;
				}

				// 見た目のスケールを半径に合わせる
				vortex.transform.scale = new Vector3(finalSuctionRadius, 0.1f, finalSuctionRadius);


				blackboard.SetInt(entityIdKey, vortex.Id);
			}

			FrameEvent.EnqueueEffectEvent("Vortex_Activate", owner.Id, finalSuctionRadius, duration);
			return NodeStatus.Running;
		}

		float startTime = blackboard.GetFloat(startTimeKey);
		float elapsed = currentTime - startTime;

		// 2. 終了判定
		if (elapsed >= duration) {
			if (blackboard.HasKey(entityIdKey)) {
				owner.Group.DestroyEntity(blackboard.GetInt(entityIdKey));
				blackboard.Remove(entityIdKey);
			}
			blackboard.Remove(startTimeKey);
			blackboard.Remove(damageTimerKey);
			blackboard.Remove(BehaviorTreeLoader.HashString("VortexFixedPos_" + NodeIdHash));
			FrameEvent.EnqueueNamedEvent("Effect_Vortex_End", owner.Id);
			return NodeStatus.Success;
		}

		// 3. 吸引・ダメージロジック
		// 開始時に保存した固定座標を使用する（ボスが動いても吸引地点は動かない）
		Vector3 center = blackboard.GetVector3(BehaviorTreeLoader.HashString("VortexFixedPos_" + NodeIdHash));

		// デバッグ表示 (吸引範囲を紫、ダメージ範囲を赤で表示、太さ12.0)
		GizmoBatch.DrawWireCircle(center + Vector3.up * 0.1f, finalSuctionRadius, new Vector4(0.5f, 0, 1, 1), 32, 12.0f);
		GizmoBatch.DrawWireCircle(center + Vector3.up * 0.2f, centerDamageRadius, new Vector4(1, 0, 0, 1), 24, 12.0f);

		float dTimer = blackboard.GetFloat(damageTimerKey);
		bool applyDamage = false;

		if (currentTime >= dTimer) {
			applyDamage = true;
			blackboard.SetFloat(damageTimerKey, currentTime + damageInterval);
		}

		var entities = owner.Group.GetEntities();
		List<Entity> objectsToExplode = new List<Entity>();

		foreach (var e in entities) {
			if (e == null || e.Id == owner.Id) continue;
			if (blackboard.HasKey(entityIdKey) && e.Id == blackboard.GetInt(entityIdKey)) continue;

			// --- 修正：吸引対象をホワイトリスト形式で厳格に制限する ---
			string ename = e.name;
			bool isObject = ename.Contains("Rock") || ename.Contains("Pillar") || ename.Contains("TargetRock");
			bool isValidTarget = ename.Contains("Player") || ename.Contains("Reinforcement") || isObject;

			if (!isValidTarget) continue;

			float dist = Vector3.Distance(center, e.transform.position);
			if (dist <= finalSuctionRadius && dist > 0.1f) {
				// 質量（Mass）を取得して吸引力に反映させる (a = F / m)
				float mass = 1.0f;
				var sphere = e.GetComponent<SphereCollider>();
				if (sphere != null) mass = sphere.mass;
				else {
					var box = e.GetComponent<BoxCollider>();
					if (box != null) mass = box.mass;
				}
				mass = Math.Max(0.1f, mass); // 0除算防止

				// 距離に応じて減衰する吸引力（線形減衰）
				float power = (suctionForce * (1.0f - (dist / finalSuctionRadius))) / mass;

				Vector3 pullDir = (center - e.transform.position).Normalized();
				
				// デバッグ：吸引前のスケールを記録
				// Vector3 oldScale = e.transform.scale;
				
				e.transform.position += pullDir * power * Time.deltaTime;

				// デバッグ：もしスケールが変わっていたら警告を出す
				// if (e.transform.scale != oldScale) {
				//     Debug.LogWarning($"[VortexScaleBug] Entity {e.name} scale CHANGED from {oldScale} to {e.transform.scale} during position update!");
				// }

				// 中心部ダメージ
				if (applyDamage && dist <= centerDamageRadius) {
					// 共通ユーティリティを使用してダメージとスロウを適用（Player, Reinforcement 両対応）
					BossDamageUtil.ApplyDamage(e, centerDamage, center);
					BossDamageUtil.ApplySlow(e, 0.5f, damageInterval); // 吸引中は移動速度を50%低下

					if (e.name.Contains("Player")) {
						//                         Debug.Log("<color=red>[Vortex]</color> Player caught in center!");
					}

					// 吸い込まれた物体の爆発（後で一括処理）
					if (isObject && dist <= 1.5f) {
						objectsToExplode.Add(e);
					}
				}
			}
		}

		// 破壊・爆発処理を一括実行
		foreach (var obj in objectsToExplode) {
			if (obj == null || obj.Id == 0) continue;
			//             Debug.Log($"<color=orange>[Vortex]</color> {obj.name} sucked in and exploded!");
			FrameEvent.EnqueueNamedEvent("Effect_Explosion", obj.Id);
			owner.Group.DestroyEntity(obj.Id);

			// 周囲のプレイヤーや援軍に爆発ダメージ
			BossDamageUtil.ApplyAreaDamage(owner.Group, center, 5.0f, 30);
		}

		return NodeStatus.Running;
	}

	public override void OnAbort(Blackboard blackboard, Entity owner) {
		uint entityIdKey = BehaviorTreeLoader.HashString("VortexEntityID_" + NodeIdHash);
		if (blackboard.HasKey(entityIdKey)) {
			owner.Group.DestroyEntity(blackboard.GetInt(entityIdKey));
			blackboard.Remove(entityIdKey);
		}
		blackboard.Remove(BehaviorTreeLoader.HashString("VortexStart_" + NodeIdHash));
		blackboard.Remove(BehaviorTreeLoader.HashString("VortexDamageTimer_" + NodeIdHash));
	}
}

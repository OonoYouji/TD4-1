using System;
using ONEngine;

/// <summary>
/// ターゲット座標（Blackboard）に向かってダメージ判定のあるビームを照射するノード。
/// C++側のエンジンに対し、ビームの開始位置、方向、長さをイベントとして送信する。
/// 
/// 仕様書v2対応：ヒット時のスロウ効果設定を追加。
/// </summary>
public class FireBeamNode : BehaviorNode {
	[BlackboardKey]
	public string targetPosKey = "TargetPosition";

	[BlackboardKey]
	public string beamLengthKey = "";

	[BlackboardKey]
	public string beamRadiusKey = "";

	[BlackboardKey]
	public string fireDirectionKey = "";

	public float damage = 50.0f;

	public float beamLength = 100.0f; // 長さを十分に確保
	public float beamRadius = 1.0f;   // 当たり判定の太さ
	public float duration = 2.0f;
	public float trackingRotationSpeed = 1.0f; // 照射中の追従速度

	// --- 追加の調整用プロパティ ---
	public float beamHeight = 8.0f;       // 発射の高さ
	public float beamOffsetForward = 3.0f; // ボスの中心からの前方オフセット

	public float slowMultiplier = 0.8f; // 20%低下
	public float slowDuration = 1.0f;

	protected override NodeStatus Execute(Blackboard blackboard, Entity owner) {
		uint startTimeKey = BehaviorTreeLoader.HashString("BeamStart_" + NodeIdHash);
		float currentTime = Time.time;

		if (!blackboard.HasKey(startTimeKey)) {
			blackboard.SetFloat(startTimeKey, currentTime);

			// --- 音声の再生 (持続音) ---
			var audio = owner.GetComponent<AudioSource>();
			if (audio == null) audio = owner.AddComponent<AudioSource>();
			if (audio != null) {
				audio.OneShotPlay(0.5f, 1.0f, "./Assets/Sounds/MainGameSounds/se/boss/beam.mp3");
			}

			// --- 予測線の削除 ---
			uint telegraphKey = BehaviorTreeLoader.HashString("TelegraphEntityID_" + NodeIdHash);
			if (blackboard.HasKey(telegraphKey)) {
				int telegraphId = blackboard.GetInt(telegraphKey);
				if (telegraphId != 0) {
					owner.Group.DestroyEntity(telegraphId);
				}
				blackboard.Remove(telegraphKey);
			}

			// --- ビームメッシュの生成 ---
			Entity beamEntity = owner.Group.CreateEntity("BossBeam");
			if (beamEntity != null) {
				beamEntity.parent = null; // 独立させる
				blackboard.SetInt(BehaviorTreeLoader.HashString("BeamEntityID_" + NodeIdHash), beamEntity.Id);

				// --- 子オブジェクトを含めた初期化の強制 ---
				FetchAllChildren(beamEntity);

				// DamageTriggerのパラメータをノードの設定で上書き
				var trigger = beamEntity.GetScript<DamageTrigger>();
				if (trigger != null) {
					trigger.damage = (int)damage;
					trigger.interval = 0.1f; // 連続ヒット判定
					trigger.slowMultiplier = slowMultiplier;
					trigger.slowDuration = slowDuration;
				}

				// トリガー設定を適用（押し戻し防止）
				var collider = beamEntity.GetComponent<BoxCollider>();
				if (collider != null) {
					collider.isTrigger = true;
				}

				// スポーン直後にトランスフォームを一度更新
				UpdateBeamTransform(beamEntity, owner, blackboard);
			}

			return NodeStatus.Running;
		}
		float startTime = blackboard.GetFloat(startTimeKey);
		float elapsed = currentTime - startTime;

		if (elapsed >= duration) {
			// --- 音声の停止 ---
			var audio = owner.GetComponent<AudioSource>();
			if (audio != null) audio.Stop();

			// --- ビームメッシュの削除 ---
			uint beamKey = BehaviorTreeLoader.HashString("BeamEntityID_" + NodeIdHash);
			if (blackboard.HasKey(beamKey)) {
				int beamId = blackboard.GetInt(beamKey);
				if (beamId != 0) owner.Group.DestroyEntity(beamId);
				blackboard.Remove(beamKey);
			}

			blackboard.Remove(startTimeKey);
			return NodeStatus.Success;
		}

		// 毎フレーム更新
		uint currentBeamKey = BehaviorTreeLoader.HashString("BeamEntityID_" + NodeIdHash);
		if (blackboard.HasKey(currentBeamKey)) {
			Entity beamEntity = owner.Group.GetEntity(blackboard.GetInt(currentBeamKey));
			if (beamEntity != null) {
				UpdateBeamTransform(beamEntity, owner, blackboard);
			}
		}

		return NodeStatus.Running;
	}

	private void UpdateBeamTransform(Entity beamEntity, Entity owner, Blackboard blackboard) {
		float finalLength = beamLength;
		if (!string.IsNullOrEmpty(beamLengthKey)) {
			uint keyHash = BehaviorTreeLoader.HashString(beamLengthKey);
			if (blackboard.HasKey(keyHash)) {
				finalLength = blackboard.GetFloat(keyHash, beamLength);
				if (finalLength == beamLength) finalLength = (float)blackboard.GetInt(keyHash, (int)beamLength);
			}
		}

		float finalRadius = beamRadius;
		if (!string.IsNullOrEmpty(beamRadiusKey)) {
			uint keyHash = BehaviorTreeLoader.HashString(beamRadiusKey);
			if (blackboard.HasKey(keyHash)) {
				finalRadius = blackboard.GetFloat(keyHash, beamRadius);
				if (finalRadius == beamRadius) finalRadius = (float)blackboard.GetInt(keyHash, (int)beamRadius) * 0.5f;
				else finalRadius *= 0.5f; // size(直径)からradius(半径)への変換
			}
		}

		Vector3 bossPosition = owner.transform.position;
		Vector3 emissionPos = new Vector3(bossPosition.x, bossPosition.y + beamHeight, bossPosition.z);

		Vector3 direction = owner.transform.forward;
		if (!string.IsNullOrEmpty(fireDirectionKey)) {
			uint keyHash = BehaviorTreeLoader.HashString(fireDirectionKey);
			if (blackboard.HasKey(keyHash)) {
				direction = blackboard.GetVector3(keyHash);
				// 修正：念のため、ブラックボードから取得した方向も水平に補正
				direction.y = 0.0f;
				if (direction.sqrMagnitude > 0.001f) direction = direction.Normalized();
			}
		} else {
			Vector3 targetPos = blackboard.GetVector3(BehaviorTreeLoader.HashString(targetPosKey));
			// 修正：水平方向のみを考慮（Y軸の変化を無視）
			Vector3 diff = targetPos - emissionPos;
			diff.y = 0.0f;
			if (diff.sqrMagnitude > 0.001f) direction = diff.Normalized();
		}

		// ボスの向きを更新（水平方向）
		var intent = owner.GetComponent<AgentIntentComponent>();
		if (intent != null) {
			Vector3 horizontalDir = new Vector3(direction.x, 0, direction.z).Normalized();
			intent.desiredRotation = Quaternion.LookRotation(horizontalDir, Vector3.up).Conjugate();
			intent.rotationSpeed = trackingRotationSpeed;
			intent.useDesiredRotation = true;
		}

		Vector3 visualStartPos = emissionPos + direction * beamOffsetForward;
		beamEntity.transform.position = visualStartPos + direction * (finalLength * 0.5f);

		// 修正：LookRotation(ワールド回転) + X90補正。
		// Y-upのメッシュを水平方向に寝かせて正面（Z方向）へ向ける
		Quaternion baseRot = Quaternion.LookRotation(direction, Vector3.up).Conjugate();
		Quaternion x90 = Quaternion.MakeFromAxis(new Vector3(1, 0, 0), 90.0f * Mathf.Deg2Rad);
		beamEntity.transform.rotation = baseRot * x90;

		// 太さと長さを適用 (Y軸が長さ)
		beamEntity.transform.scale = new Vector3(finalRadius * 2.0f, finalLength * 0.5f, finalRadius * 2.0f);

		// 子オブジェクトの補正
		uint childCount = beamEntity.GetChildCount();
		for (uint i = 0; i < childCount; i++) {
			Entity child = beamEntity.GetChild(i);
			if (child != null && child.name.Contains("out")) {
				var t = child.GetComponent<Transform>();
				if (t != null) {
					t.position = new Vector3(0, -1.0f, 0);
					if (child.name == "out_1") t.scale = new Vector3(1.4f, 1.0f, 1.4f);
					else t.scale = new Vector3(1.22f, 1.0f, 1.22f);
				}
			}
		}

		// コライダー調整
		var boxCollider = beamEntity.GetComponent<BoxCollider>();
		if (boxCollider != null) boxCollider.size = new Vector3(1.0f, 1.0f, 1.0f);

		// 可視化デバッグ
		GizmoBatch.DrawWireCube(beamEntity.transform.position, new Vector3(finalRadius * 2, finalRadius * 2, finalLength), Quaternion.LookRotation(direction, Vector3.up).Conjugate(), new Vector4(1, 0, 1, 1), 16.0f);
	}

	private Quaternion CalcFromToRotation(Vector3 from, Vector3 to) {
		from = from.Normalized();
		to = to.Normalized();
		float dot = Vector3.Dot(from, to);
		if (dot > 0.999999f) return Quaternion.identity;
		if (dot < -0.999999f) {
			Vector3 axis = Vector3.Cross(Vector3.right, from);
			if (axis.sqrMagnitude < 0.0001f) axis = Vector3.Cross(Vector3.up, from);
			return Quaternion.MakeFromAxis(axis.Normalized(), (float)Math.PI);
		}
		Vector3 cross = Vector3.Cross(from, to);
		float s = (float)Math.Sqrt((1.0f + dot) * 2.0f);
		float invS = 1.0f / s;
		return new Quaternion(cross.x * invS, cross.y * invS, cross.z * invS, s * 0.5f);
	}

	private void FetchAllChildren(Entity entity) {
		if (entity == null) return;
		var anim = entity.GetComponent<ONEngine.AnimationPlayer>();
		if (anim != null) anim.Play();
		uint childCount = entity.GetChildCount();
		for (uint i = 0; i < childCount; i++) FetchAllChildren(entity.GetChild(i));
	}

	public override void OnAbort(Blackboard blackboard, Entity owner) {
		blackboard.Remove(BehaviorTreeLoader.HashString("BeamStart_" + NodeIdHash));

		// --- 音声の停止 ---
		var audio = owner.GetComponent<AudioSource>();
		if (audio != null) audio.Stop();

		uint beamKey = BehaviorTreeLoader.HashString("BeamEntityID_" + NodeIdHash);
		if (blackboard.HasKey(beamKey)) {
			int beamId = blackboard.GetInt(beamKey);
			if (beamId != 0) owner.Group.DestroyEntity(beamId);
			blackboard.Remove(beamKey);
		}
	}
}

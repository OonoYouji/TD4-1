using System;
using System.Collections.Generic;

/// <summary>
/// ボスの「詰まらせ攻撃」を制御するクラス。
/// 範囲内の援軍を巨大化させ、物理的な渋滞を引き起こす。
/// </summary>
public class BossClogAttack : MonoScript {
	[SerializeField] public float radius = 500.0f;
	[SerializeField] public float duration = 5.0f;
	[SerializeField] public float scaleMultiplier = 2.5f;
	[SerializeField] public float preparationTime = 1.0f;
	[SerializeField] public string targetTag = "Reinforcement";

	private enum State { Idle, Preparation, Active }
	private State currentState = State.Idle;
	private float stateTimer = 0.0f;
	private Animator animator;
	private string currentAnim = "";

	public bool IsActive => currentState != State.Idle;

	private class AffectedReinforcement {
		public Entity entity;
		public Vector3 originalScale;
		public float remainingTime;
	}
	private List<AffectedReinforcement> affectedList = new List<AffectedReinforcement>();

	public override void Initialize()
	{
		animator = entity.GetComponent<Animator>();
	}

	private void PlayAnimation(string clipName)
	{
		if (animator == null || currentAnim == clipName) return;
		Debug.Log($"[BossAnimation] Changing to: {clipName} (from: {currentAnim})");
		animator.CrossFade(clipName, 0.15f);
		currentAnim = clipName;
	}

	public override void Update() {
		switch (currentState) {
		case State.Idle:
			if (Input.TriggerKey(KeyCode.C)) {
				StartAttack();
			}
			break;

		case State.Preparation:
			if (currentAnim != "clog_start") {
				animator.CrossFadeWithDuration("clog_start", preparationTime);
				currentAnim = "clog_start";
			}
			stateTimer -= Time.deltaTime;
			GizmoBatch.DrawWireCircle(transform.position, radius, new Vector4(1, 0.5f, 0, 1));

			if (stateTimer <= 0) {
				ExecuteAttack();
			}
			break;

		case State.Active:
			if (currentAnim != "clog") {
				animator.CrossFadeWithDuration("clog", duration);
				currentAnim = "clog";
			}
			stateTimer -= Time.deltaTime;
			if (stateTimer <= 0) {
				PlayAnimation("clog_end");
				currentState = State.Idle;
			}
			break;
		}

		UpdateAffectedReinforcements();
	}

	public void StartAttack() {
		if (currentState != State.Idle) return;
		currentState = State.Preparation;
		stateTimer = preparationTime;
		Debug.Log("[BossClogAttack] Preparation started...");
	}

	private void ExecuteAttack() {
		currentState = State.Active;
		stateTimer = duration;
		Debug.Log("[BossClogAttack] Clog Attack Active!");

		foreach (var entity in ecsGroup.GetEntities()) {
			if (!entity.name.Contains(targetTag)) continue;

			float dist = Vector3.Distance(transform.position, entity.transform.position);
			if (dist <= radius) {
				ApplyEnlargement(entity);
			}
		}
	}

	private void ApplyEnlargement(Entity target) {
		// 既に巨大化している場合はタイマーをリセット
		var existing = affectedList.Find(a => a.entity.Id == target.Id);
		if (existing != null) {
			existing.remainingTime = duration;
			return;
		}

		// 新規適用
		var info = new AffectedReinforcement {
			entity = target,
			originalScale = target.transform.scale,
			remainingTime = duration
		};

		target.transform.scale = info.originalScale * scaleMultiplier;
		affectedList.Add(info);
	}

	private void UpdateAffectedReinforcements() {
		for (int i = affectedList.Count - 1; i >= 0; i--) {
			var info = affectedList[i];

			// エンティティが消滅している場合
			if (info.entity == null || info.entity.Id == 0) {
				affectedList.RemoveAt(i);
				continue;
			}

			info.remainingTime -= Time.deltaTime;
			if (info.remainingTime <= 0) {
				// 元のサイズに戻す
				info.entity.transform.scale = info.originalScale;
				affectedList.RemoveAt(i);
			}
		}
	}
}

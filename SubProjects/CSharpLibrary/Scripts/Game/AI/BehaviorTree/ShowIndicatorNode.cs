using System;

public enum IndicatorShape {
	Line,   // ビーム用予測線
	Circle  // 落下・範囲用
}

public enum IndicatorCenterType {
	Target, // ターゲット地点
	Boss,   // ボスの位置
	Other   // 自由指定
}

/// <summary>
/// 特定の形状の「予兆（Indicator）」を生成または制御するアクションノード。
/// ビームの予測線や、岩落としの落下地点円などを AI からトリガーするために使用。
/// </summary>
public class ShowIndicatorNode : BehaviorNode {
	/// <summary>
	/// 表示する形状。
	/// </summary>
	public IndicatorShape shape = IndicatorShape.Line;

	/// <summary>
	/// 中心位置の基準。
	/// </summary>
	public IndicatorCenterType centerType = IndicatorCenterType.Target;

	/// <summary>
	/// 表示時間（秒）。0以下の場合は明示的に非表示にするまで継続。
	/// </summary>
	public float duration = 2.0f;

	/// <summary>
	/// Blackboardから表示時間を取得する場合のキー名。
	/// </summary>
	[BlackboardKey]
	public string durationKey = "";

	/// <summary>
	/// 線の太さや円の半径。
	/// </summary>
	public float size = 1.0f;

	/// <summary>
	/// 線の長さ（Lineの場合）。
	/// </summary>
	public float length = 20.0f;

	/// <summary>
	/// Blackboardから長さを取得する場合のキー名。
	/// </summary>
	[BlackboardKey]
	public string lengthKey = "";

	/// <summary>
	/// 予兆の色。
	/// </summary>
	public Vector4 color = new Vector4(1.0f, 0.0f, 0.0f, 0.5f);

	/// <summary>
	/// Blackboardからサイズを取得する場合のキー名。
	/// </summary>
	[BlackboardKey]
	public string sizeKey = "";

	/// <summary>
	/// Blackboard上のターゲット座標キー（そこに向かって線を描く、またはそこに円を描く）。
	/// </summary>
	[BlackboardKey]
	public string targetPosKey = "TargetPosition";

	/// <summary>
	/// 決定された発射方向（Vector3）を書き込むBlackboardのキー。
	/// </summary>
	[BlackboardKey]
	public string fireDirectionKey = "";

	/// <summary>
	/// 生成時に位置を固定するかどうか。trueの場合、ターゲットが動いても予兆は追従しません。
	/// </summary>
	public bool lockPosition = false;

	/// <summary>
	/// ボスの中心からの前方オフセット（Lineの場合）。
	/// </summary>
	public float offsetForward = 3.0f;

	/// <summary>
	/// 地面からの高さオフセット。
	/// </summary>
	public float offsetHeight = 0.1f;

	/// <summary>
	/// 指定した時間をかけて0から目標の大きさまで拡大する演出を行う場合の時間（秒）。
	/// </summary>
	public float expandDuration = 0.0f;

	/// <summary>
	/// Blackboardから拡大時間を取得する場合のキー名。
	/// </summary>
	[BlackboardKey]
	public string expandDurationKey = "";

	protected override NodeStatus Execute(Blackboard blackboard, Entity owner) {
		uint startTimeKey = BehaviorTreeLoader.HashString("IndicatorStart_" + NodeIdHash);
		uint doneKey = BehaviorTreeLoader.HashString("IndicatorDone_" + NodeIdHash);
		float currentTime = Time.time;

		// すでにこのアクティベーションで完了している場合は再実行（再生成）しない
		if (blackboard.HasKey(doneKey)) return NodeStatus.Success;

		float finalDuration = duration;
		if (!string.IsNullOrEmpty(durationKey)) {
			uint keyHash = BehaviorTreeLoader.HashString(durationKey);
			if (blackboard.HasKey(keyHash)) {
				finalDuration = blackboard.GetFloat(keyHash, duration);
				if (finalDuration == duration) {
					finalDuration = (float)blackboard.GetInt(keyHash, (int)duration);
				}
			}
		}

		float finalSize = size;
		if (!string.IsNullOrEmpty(sizeKey)) {
			uint keyHash = BehaviorTreeLoader.HashString(sizeKey);
			if (blackboard.HasKey(keyHash)) {
				finalSize = blackboard.GetFloat(keyHash, size);
				if (finalSize == size) {
					finalSize = (float)blackboard.GetInt(keyHash, (int)size);
				}
			}
		}

		float finalLength = length;
		if (!string.IsNullOrEmpty(lengthKey)) {
			uint keyHash = BehaviorTreeLoader.HashString(lengthKey);
			if (blackboard.HasKey(keyHash)) {
				finalLength = blackboard.GetFloat(keyHash, length);
				if (finalLength == length) {
					finalLength = (float)blackboard.GetInt(keyHash, (int)length);
				}
			}
		}

		float finalExpandDuration = expandDuration;
		if (!string.IsNullOrEmpty(expandDurationKey)) {
			uint keyHash = BehaviorTreeLoader.HashString(expandDurationKey);
			if (blackboard.HasKey(keyHash)) {
				finalExpandDuration = blackboard.GetFloat(keyHash, expandDuration);
				if (finalExpandDuration == expandDuration) {
					finalExpandDuration = (float)blackboard.GetInt(keyHash, (int)expandDuration);
				}
			}
		}

		float elapsed = 0.0f;
		if (blackboard.HasKey(startTimeKey)) {
			elapsed = currentTime - blackboard.GetFloat(startTimeKey);
		}

		float progress = 1.0f;
		if (finalExpandDuration > 0.001f) {
			progress = Math.Min(1.0f, elapsed / finalExpandDuration);
		}

		float currentSize = finalSize * progress;
		float currentLength = finalLength * progress;

		if (!blackboard.HasKey(startTimeKey)) {
			// 初回実行
			Vector3 originPos = owner.transform.position;
			if (centerType == IndicatorCenterType.Target) {
				uint keyHash = BehaviorTreeLoader.HashString(targetPosKey);
				if (blackboard.HasKey(keyHash)) originPos = blackboard.GetVector3(keyHash);
			}

			// --- プレハブを使用してメッシュベースの予測線を生成 ---
			string prefabName = (shape == IndicatorShape.Line) ? "TelegraphLine" : "TelegraphCircle";
			Entity telegraph = owner.Group.CreateEntity(prefabName);
			if (telegraph != null) {
				// スクリプトの追加と初期設定
				if (shape == IndicatorShape.Line) {
					var script = telegraph.GetScript<TelegraphLine>();
					if (script == null) script = telegraph.AddScript<TelegraphLine>();
					script.thickness = currentSize;
					script.length = currentLength;
					script.offsetForward = offsetForward;
					script.offsetHeight = offsetHeight;
					script.color = color;
				} else {
					var script = telegraph.GetScript<TelegraphCircle>();
					if (script == null) script = telegraph.AddScript<TelegraphCircle>();
					script.size = currentSize;
					script.offsetHeight = offsetHeight;
					script.color = color;
				}

				// 色の設定
				var renderer = telegraph.GetComponent<MeshRenderer>();
				if (renderer == null) {
					for (uint i = 0; i < telegraph.GetChildCount(); i++) {
						var child = telegraph.GetChild(i);
						if (child != null) {
							renderer = child.GetComponent<MeshRenderer>();
							if (renderer != null) break;
						}
					}
				}
				if (renderer != null) {
					renderer.color = color;
					renderer.renderQueue = RenderQueue.Telegraph;
				}

				uint targetKeyHash = BehaviorTreeLoader.HashString(targetPosKey);
				Vector3 currentTarget = blackboard.HasKey(targetKeyHash) ? blackboard.GetVector3(targetKeyHash) : owner.transform.position;

				if (currentTarget.sqrMagnitude < 0.0001f) {
					currentTarget = owner.transform.position + owner.transform.rotate * Vector3.forward * 10.0f;
				}

				UpdateTelegraphTransform(telegraph, owner.transform.position, originPos, currentTarget, currentSize, currentLength);

				if (!string.IsNullOrEmpty(fireDirectionKey)) {
					Vector3 diff = new Vector3(currentTarget.x - owner.transform.position.x, 0.0f, currentTarget.z - owner.transform.position.z);
					Vector3 direction = (diff.sqrMagnitude > 0.001f) ? diff.Normalized() : owner.transform.forward;
					blackboard.SetVector3(BehaviorTreeLoader.HashString(fireDirectionKey), direction);
				}

				blackboard.SetInt(BehaviorTreeLoader.HashString("TelegraphEntityID_" + NodeIdHash), telegraph.Id);
			}

			blackboard.SetFloat(startTimeKey, currentTime);
			return NodeStatus.Running;
		}

		float startTime = blackboard.GetFloat(startTimeKey);

		// 毎フレーム更新
		uint telegraphKey = BehaviorTreeLoader.HashString("TelegraphEntityID_" + NodeIdHash);
		if (blackboard.HasKey(telegraphKey)) {
			int telegraphId = blackboard.GetInt(telegraphKey);
			if (telegraphId != 0) {
				Entity telegraph = owner.Group.GetEntity(telegraphId);
				if (telegraph != null) {
					uint targetKeyHash = BehaviorTreeLoader.HashString(targetPosKey);
					Vector3 currentTarget = blackboard.HasKey(targetKeyHash) ? blackboard.GetVector3(targetKeyHash) : owner.transform.position;

					Vector3 originPos = owner.transform.position;
					if (centerType == IndicatorCenterType.Target) originPos = currentTarget;

					if (!lockPosition) {
						UpdateTelegraphTransform(telegraph, owner.transform.position, originPos, currentTarget, currentSize, currentLength);

						if (!string.IsNullOrEmpty(fireDirectionKey)) {
							Vector3 diff = new Vector3(currentTarget.x - owner.transform.position.x, 0.0f, currentTarget.z - owner.transform.position.z);
							Vector3 direction = (diff.sqrMagnitude > 0.001f) ? diff.Normalized() : owner.transform.forward;
							blackboard.SetVector3(BehaviorTreeLoader.HashString(fireDirectionKey), direction);
						}
					} else {
						UpdateTelegraphSizeOnly(telegraph, currentSize, currentLength);
					}
				}
			}
		}

		// 完了判定
		if (finalDuration > 0.001f && currentTime - startTime >= finalDuration) {
			if (blackboard.HasKey(telegraphKey)) {
				int telegraphId = blackboard.GetInt(telegraphKey);
				if (telegraphId != 0) owner.Group.DestroyEntity(telegraphId);
				blackboard.Remove(telegraphKey);
			}

			blackboard.Remove(startTimeKey);
			blackboard.SetBool(doneKey, true);
			return NodeStatus.Success;
		}

		return NodeStatus.Running;
	}

	private void UpdateTelegraphTransform(Entity telegraph, Vector3 bossPos, Vector3 originPos, Vector3 targetPos, float finalSize, float finalLength) {
		if (shape == IndicatorShape.Line) {
			var script = telegraph.GetScript<TelegraphLine>();
			if (script != null) {
				script.bossPosition = bossPos;
				script.targetPosition = targetPos;
				script.thickness = finalSize;
				script.length = finalLength;
			}
		} else if (shape == IndicatorShape.Circle) {
			var script = telegraph.GetScript<TelegraphCircle>();
			if (script != null) {
				script.centerPosition = originPos;
				script.size = finalSize;
			}
		}
	}

	private void UpdateTelegraphSizeOnly(Entity telegraph, float finalSize, float finalLength) {
		if (shape == IndicatorShape.Line) {
			var script = telegraph.GetScript<TelegraphLine>();
			if (script != null) {
				script.thickness = finalSize;
				script.length = finalLength;
			}
		} else if (shape == IndicatorShape.Circle) {
			var script = telegraph.GetScript<TelegraphCircle>();
			if (script != null) {
				script.size = finalSize;
			}
		}
	}

	public override void OnAbort(Blackboard blackboard, Entity owner) {
		blackboard.Remove(BehaviorTreeLoader.HashString("IndicatorStart_" + NodeIdHash));
		blackboard.Remove(BehaviorTreeLoader.HashString("IndicatorDone_" + NodeIdHash));

		uint telegraphKey = BehaviorTreeLoader.HashString("TelegraphEntityID_" + NodeIdHash);
		if (blackboard.HasKey(telegraphKey)) {
			int telegraphId = blackboard.GetInt(telegraphKey);
			if (telegraphId != 0) owner.Group.DestroyEntity(telegraphId);
			blackboard.Remove(telegraphKey);
		}
	}
}

using System;

/// <summary>
/// 固定のオフセット位置を TargetPosition として Blackboard に書き込むサービス。
/// テスト用エンティティで特定方向への攻撃を固定するために使用。
/// </summary>
public class FixedTargetService : BehaviorService {
	[BlackboardKey]
	public string targetPosKey = "TargetPosition";

	public Vector3 offset = new Vector3(0, 0, 10);

	public override void OnTick(Blackboard blackboard, Entity owner) {
		Vector3 targetPos = owner.transform.position + owner.transform.rotation * offset;
		blackboard.SetVector3(BehaviorTreeLoader.HashString(targetPosKey), targetPos);

		// --- デバッグ用：ターゲット地点の表示 ---
		GizmoBatch.DrawLine(owner.transform.position + Vector3.up * 2.0f, targetPos, new Vector4(1, 0, 0, 1));
	}
}

using System;
using ONEngine;

/// <summary>
/// ボスの直線状攻撃の予測表示を制御するスクリプト。
/// ボスからターゲット方向へ伸びる長方形の表示を担当する。
/// </summary>
public class TelegraphLine : MonoScript {
	public Vector3 targetPosition;
	public Vector3 bossPosition;
	public bool useFixedDirection = false;
	public Vector3 fixedDirection = Vector3.forward;
	public float thickness = 1.0f;

	public float length = 20.0f;
	public float offsetForward = 3.0f;
	public float offsetHeight = 0.1f;
	public Vector4 color = new Vector4(1.0f, 0.0f, 0.0f, 0.5f);

	public override void Update() {
		// ターゲット方向の計算
		Vector3 direction;
		// 修正：予測線は地面（Y=0付近）に表示させる
		Vector3 startPos = new Vector3(bossPosition.x, 0.1f, bossPosition.z);

		if (useFixedDirection && fixedDirection.sqrMagnitude > 0.001f) {
			direction = fixedDirection.Normalized();
		} else {
			// 水平方向のみを考慮（Y軸の変化を無視）
			Vector3 diff = new Vector3(targetPosition.x - startPos.x, 0.0f, targetPosition.z - startPos.z);
			direction = (diff.sqrMagnitude > 0.001f) ? diff.Normalized() : Vector3.forward;
		}

		// 1. 起点（スタート地点）の計算: 
		// ボスの位置から前方のオフセット分進めた位置を起点にする
		Vector3 visualStartPos = startPos + direction * offsetForward;

		// 2. 長さの決定
		float finalLength = Math.Max(0.001f, length);
		float safeThickness = Math.Max(0.001f, thickness);

		// 3. 配置座標の制御: 
		transform.position = visualStartPos;

		// 4. 回転の制御
		// LookRotationの結果（ビュー空間形式）にConjugateを適用して水平方向を向かせる
		transform.rotation = Quaternion.LookRotation(direction, Vector3.up).Conjugate();

		// 5. スケールの制御:
		// Z は予測線の長さ(finalLength)の半分とする（エンジンのCube仕様）。
		transform.scale = new Vector3(safeThickness, 0.1f, finalLength * 0.5f);


		// レイヤーと色の適用
		var renderer = entity.GetComponent<MeshRenderer>();
		if (renderer == null) {
			// 子要素にある場合を考慮
			for (uint i = 0; i < entity.GetChildCount(); i++) {
				var child = entity.GetChild(i);
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

		// デバッグ用描画
		GizmoBatch.DrawRay(visualStartPos, direction * finalLength, color);
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
}

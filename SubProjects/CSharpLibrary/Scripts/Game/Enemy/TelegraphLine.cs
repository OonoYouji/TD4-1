using System;

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
		// 高度差を無視した水平方向のベクトルを計算
		Vector3 direction;
		if (useFixedDirection && fixedDirection.sqrMagnitude > 0.001f) {
			direction = fixedDirection.Normalized();
		} else {
			Vector3 diff = new Vector3(targetPosition.x - bossPosition.x, 0.0f, targetPosition.z - bossPosition.z);
			direction = (diff.sqrMagnitude > 0.001f) ? diff.Normalized() : Vector3.forward;
		}

		// 1. 起点（スタート地点）の計算: 
		// ボスの中心(horizontalBoss)から、ターゲット方向(direction)へオフセット分進めた位置を起点にする。
		Vector3 horizontalBoss = new Vector3(bossPosition.x, offsetHeight, bossPosition.z);
		Vector3 visualStartPos = horizontalBoss + direction * offsetForward;

		// 2. 長さの決定
		// ターゲットまでの距離に関わらず、指定された length の値を使用する
		float finalLength = length;

		// 3. 配置座標の制御: 
		// キューブの原点は中心にあるため、端を起点(visualStartPos)に合わせるには
		// 進行方向に「長さの半分」だけ座標をずらして配置する。
		transform.position = visualStartPos;

		// 4. 回転の制御
		transform.rotation = Quaternion.LookRotation(direction).Conjugate();

		// 5. スケールの制御:
		// Z は予測線の長さ(finalLength)の半分とする（モデルの原点が中心にあるため、エンジン側の仕様に合わせる）。
		transform.scale = new Vector3(thickness, 0.1f, finalLength * 0.5f);

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
}

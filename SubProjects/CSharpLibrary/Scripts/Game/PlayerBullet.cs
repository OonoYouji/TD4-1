using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PlayerBullet : MonoScript {

	enum UTurnState {
		Straight,   /// 直進中
		UTurn       /// Uターン中
	}

	/// 基盤
	public Vector3 velocity = new Vector3(0, 0, 1);
	float lifeTime = 20.0f;

	/// 進行・Uターン用パラメータ
	// ↓ ここで指定した方向（外部から変更可能）でずっとUターンし続けます
	public UTurnType uTurnType = UTurnType.Left;

	UTurnState uTurnState = UTurnState.Straight;

	float straightDuration = 2.0f;  // 直進する時間（秒）
	float straightTimer = 0.0f;     // 直進の経過時間

	float uTurnDuration = 1.0f;     // Uターンにかける時間（秒）
	float uTurnTimer = 0.0f;        // Uターンの経過時間

	public override void Initialize() {
		uTurnState = UTurnState.Straight;
		straightTimer = 0.0f;
		uTurnTimer = 0.0f;
	}

	public override void Update() {

		switch (uTurnState) {
		case UTurnState.Straight:
			straightTimer += Time.deltaTime;

			if (straightTimer >= straightDuration) {
				uTurnState = UTurnState.UTurn;
				uTurnTimer = 0.0f;
			} else {
				MoveStraight();
			}
			break;

		case UTurnState.UTurn:
			MoveUTurn();
			break;
		}

		CheckLifeTime();
	}

	void MoveStraight() {
		transform.position += velocity * Time.deltaTime;
	}

	void MoveUTurn() {
		uTurnTimer += Time.deltaTime;

		float turnSpeedDeg = 180.0f / uTurnDuration;

		// 設定されている uTurnType に従って回転方向を決める
		float directionSign = (uTurnType == UTurnType.Left) ? -1.0f : 1.0f;
		float angleThisFrame = turnSpeedDeg * directionSign * Time.deltaTime;

		float rad = angleThisFrame * (float)(Math.PI / 180.0);
		float cos = (float)Math.Cos(rad);
		float sin = (float)Math.Sin(rad);

		float newX = velocity.x * cos - velocity.z * sin;
		float newZ = velocity.x * sin + velocity.z * cos;
		velocity = new Vector3(newX, velocity.y, newZ);

		transform.position += velocity * Time.deltaTime;

		// 180度回りきったときの処理
		if (uTurnTimer >= uTurnDuration) {
			uTurnState = UTurnState.Straight;
			straightTimer = 0.0f;
			// ※方向を反転させるコードを削除したため、次回も同じ方向に曲がります
		}
	}

	void CheckLifeTime() {
		lifeTime -= Time.deltaTime;
		if (lifeTime <= 0) {
			entity.Destroy();
		}
	}
}
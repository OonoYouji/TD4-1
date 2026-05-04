using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PlayerBullet : MonoScript {

	public enum UTurnType {
		Left, Right
	}

	/// 状態を2つ（直進とUターン）に絞り、これをループさせます
	enum UTurnState {
		Straight,   /// 直進中
		UTurn       /// Uターン中
	}

	/// 基盤
	public Vector3 velocity = new Vector3(0, 0, 1);
	float lifeTime = 20.0f;

	/// 進行・Uターン用パラメータ
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

		/// 状態のループ処理
		switch (uTurnState) {

		case UTurnState.Straight:
			straightTimer += Time.deltaTime;

			// 指定時間直進したら、Uターン状態へ切り替え
			if (straightTimer >= straightDuration) {
				uTurnState = UTurnState.UTurn;
				uTurnTimer = 0.0f; // Uターンのタイマーをリセット（重要）
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
		float directionSign = (uTurnType == UTurnType.Left) ? -1.0f : 1.0f;
		float angleThisFrame = turnSpeedDeg * directionSign * Time.deltaTime;

		float rad = angleThisFrame * (float)(Math.PI / 180.0);
		float cos = (float)Math.Cos(rad);
		float sin = (float)Math.Sin(rad);

		float newX = velocity.x * cos - velocity.z * sin;
		float newZ = velocity.x * sin + velocity.z * cos;
		velocity = new Vector3(newX, velocity.y, newZ);

		transform.position += velocity * Time.deltaTime;

		// 180度回りきったら、再び直進状態へ戻す
		if (uTurnTimer >= uTurnDuration) {
			uTurnState = UTurnState.Straight;
			straightTimer = 0.0f; // 直進のタイマーをリセットしてループ（重要）
		}
	}

	void CheckLifeTime() {
		lifeTime -= Time.deltaTime;
		if (lifeTime <= 0) {
			entity.Destroy();
		}
	}
}
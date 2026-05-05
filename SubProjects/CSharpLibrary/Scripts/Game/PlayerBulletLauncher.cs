using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum UTurnType {
	Left, Right
}

public class PlayerBulletLauncher : MonoScript {


    ///
    /// 基礎
    ///

    // 発射口の位置オフセット
    [SerializeField]
	Vector3 offset = new Vector3(0, 0, 1);

    // 弾の速度
    [SerializeField]
	public float bulletSpeed = 10.0f;


	///
	/// Uターンを制御するため
	///
	[SerializeField] UTurnType uTurnType = UTurnType.Left;
	[SerializeField] public Mouse launchKey = Mouse.Left;


	// PlayerLeftHand.objのX軸方向の自然長
	private const float modelNaturalLength = 2.249086f;

    // 発射方向（プレイヤーの前方）
    public Vector3 launchDirection = new Vector3(0, 0, 1);

    // 内部状態
    private Player player;


	public override void Initialize() {

        // プレイヤーエンティティを親に設定
        Entity playerEntity = ecsGroup.FindEntity("Player");
		if (playerEntity == null) {
			Debug.LogError("PlayerBulletLauncher: Playerエンティティが見つかりません");
			return;
		}
		entity.parent = playerEntity;

        // Playerスクリプトを取得
        player = playerEntity.GetScript<Player>();
		if (player == null) {
			Debug.LogError("PlayerBulletLauncher: PlayerスクリプトがPlayerエンティティに見つかりません");
		}
	}

	public override void Update() {
		if (player == null) { 
			return; 
		}

		// 腕の先端に位置を追従
		float sign = 1.0f;
		if (uTurnType == UTurnType.Left) { 
			sign = -1.0f; 
		}

        // プレイヤーの腕の先端に発射口を配置
        Vector3 pos = transform.position;
		pos.x = sign * player.armLength * modelNaturalLength;
		transform.position = pos;

		// 発射方向をプレイヤーの前方に更新
		Matrix4x4 rotMat = Matrix4x4.Rotate(player.transform.rotate);
		launchDirection = Matrix4x4.Transform(Vector3.forward, rotMat);
	}


	public void Fire() {
        // 発射キーが押されたら弾を発射
        FireBullet(uTurnType);
	}


	void FireBullet(UTurnType type) {

        // 弾のエンティティを生成
        var bullet = ecsGroup.CreateEntity("PlayerBullet");
        // PlayerBulletスクリプトを取得して初期化
        PlayerBullet bs = bullet.GetScript<PlayerBullet>();
        bs.startPosition = transform.GetWorldPosition();

        bs.velocity = launchDirection.Normalized() * bulletSpeed;
		bs.uTurnType = type;
	}
}

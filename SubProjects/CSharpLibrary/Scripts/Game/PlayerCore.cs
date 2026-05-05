using System;

public class PlayerCore : MonoScript
{

    // =========================================================
    // パラメーター
    // =========================================================

    // プレイヤーからのオフセット
    [SerializeField] public Vector3 offset = new Vector3();

    // 追従速度
    [SerializeField] public float followSpeed = 3.0f;

    // HP
    [SerializeField] public int maxHp = 3;

    // 大きさ
    [SerializeField] public float size = 3.0f;


    // =========================================================
    // 内部状態
    // =========================================================

    private int currentHp;
    private Transform playerTransform;


    // =========================================================
    // プロパティ
    // =========================================================

    public int CurrentHp => currentHp;
    public bool IsDead => currentHp <= 0;


    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        currentHp = maxHp;

        Entity playerEntity = ecsGroup.FindEntity("Player");
        if (playerEntity == null)
        {
            Debug.LogError("PlayerCore: Playerエンティティが見つかりません");
            return;
        }
        playerTransform = playerEntity.transform;

        transform.scale = new Vector3(size, size, size);
    }

    public override void Update()
    {
        if (playerTransform == null) { return; }
        FollowPlayer();
    }


    // =========================================================
    // 追従
    // =========================================================

    private void FollowPlayer()
    {

        // プレイヤーの回転から、前方と右方向のベクトルを計算
        Matrix4x4 rotMat = Matrix4x4.Rotate(playerTransform.rotate);
        Vector3 forward = Matrix4x4.Transform(Vector3.forward, rotMat);
        Vector3 right   = Matrix4x4.Transform(Vector3.right,   rotMat);

        // プレイヤーからのオフセットを計算
        Vector3 targetPos = playerTransform.position
            + forward  * offset.z // 前方オフセット
            + right    * offset.x // 横方向オフセット
            + Vector3.up * offset.y; // 上方向オフセット

        // Lerpで追従
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            followSpeed * Time.deltaTime
        );
    }


    // =========================================================
    // HP
    // =========================================================

    public void TakeDamage(int damage)
    {
        // ダメージを受ける
        currentHp -= damage;
        if (currentHp <= 0)
        {
            // HP0なので死亡
            currentHp = 0;
            OnDead();
        }
    }

    private void OnDead()
    {
        // ゲームオーバーシーンに遷移
        SceneManager.LoadScene("GameOver");
    }


    // =========================================================
    // 衝突
    // =========================================================

    public override void OnCollisionEnter(Entity collision)
    {
        // 敵の弾からダメージを受ける
        if (collision.name.Contains("EnemyBullet"))
        {
            TakeDamage(1);
        }
    }
}

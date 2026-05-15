using System;
using System.Collections.Generic;
public class CallingReinforcement : MonoScript
{

    // =========================================================
    // パラメーター
    // =========================================================

    // 進行方向に対する左右オフセット
    [SerializeField] public float xOffset = 2.0f;
    // 後方からの出現距離
    [SerializeField] public float spawnBehindDistance = 5.0f;
    // 援軍の移動速度
    [SerializeField] public float reinforcementSpeed = 8.0f;
    // スポーン間隔（秒）
    [SerializeField] public float spawnInterval = 3.0f;

    // =========================================================
    // 内部状態
    // =========================================================

    private Player player = null;
    private List<Entity> activeReinforcements = new List<Entity>();
    private float spawnTimer = 0.0f;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        // プレイヤーEntityを探す
        Entity playerEntity = ecsGroup.FindEntity("Player");

        // 見つかったらPlayerスクリプトを取得
        if (playerEntity != null)
        {
            player = playerEntity.GetScript<Player>();
        }

        spawnTimer = spawnInterval;
    }

    public override void Update()
    {
        // 発射処理
        HandleFiring();
        // 退散命令
        HandleRetreat();
    }

    // =========================================================
    // 発射
    // =========================================================

    private void HandleFiring()
    {
        if (player == null) { return; }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0.0f)
        {
            spawnTimer = spawnInterval;
            SpawnReinforcements();
        }
    }

    // =========================================================
    // 退散
    // =========================================================

    private void HandleRetreat()
    {

        // 退散キーが押されたか
        bool wantRetreat =Input.TriggerKey(KeyCode.E) ||Input.TriggerGamepad(Gamepad.B);

        // 押されていなければ何もしない
        if (!wantRetreat) { 
            return; 
        }

        // 退散させる軍隊がいなければ何もしない
        foreach (Entity ReinforcementEntity in activeReinforcements)
        {
            if (ReinforcementEntity == null) {
                continue; 
            }

            // 退散させる
            Reinforcement reinforcement = ReinforcementEntity.GetScript<Reinforcement>();
            if (reinforcement != null) { 
                reinforcement.Retreat(); 
            }
        }

        // 退散させた軍隊はリストから削除
        activeReinforcements.Clear();
    }

    // =========================================================
    // スポーン
    // =========================================================

    private void SpawnReinforcements()
    {
        // プレイヤーの前方・右方向ベクトルを算出
        Matrix4x4 rotMat = Matrix4x4.Rotate(player.transform.rotate);
        Vector3 forward = Matrix4x4.Transform(Vector3.forward, rotMat);
        Vector3 right = Matrix4x4.Transform(Vector3.right, rotMat);

        // 後方の基点
        Vector3 spawnBase = player.transform.position - forward * spawnBehindDistance;

        SpawnOne(spawnBase - right * xOffset, forward);
        SpawnOne(spawnBase + right * xOffset, forward);
    }

    private void SpawnOne(Vector3 spawnPos, Vector3 dir)
    {
        // Entityを生成
        Entity ReinforcementEntity = ecsGroup.CreateEntity("Reinforcement");

        // スクリプトを追加
        if (ReinforcementEntity == null) { 
            return;
        }

        // 初期位置と速度を設定
        Reinforcement reinforcement = ReinforcementEntity.GetScript<Reinforcement>();

        // スクリプトが取得できなければ何もしない
        if (reinforcement == null) { 
            return;
        }
        reinforcement.startPosition = spawnPos;
        reinforcement.velocity = dir * reinforcementSpeed;
        activeReinforcements.Add(ReinforcementEntity);
    }
}

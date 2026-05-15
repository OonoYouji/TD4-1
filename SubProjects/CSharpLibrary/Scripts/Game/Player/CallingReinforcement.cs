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

    // =========================================================
    // 内部状態
    // =========================================================

    private Player player = null;
    private List<Entity> activeReinforcements = new List<Entity>();

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
    }

    public override void Update()
    {

        // 発射と退散の入力を処理
        HandleFiring();
        //  
        HandleRetreat();
    }

    // =========================================================
    // 発射
    // =========================================================

    private void HandleFiring()
    {
        // 発射の入力をチェック
        bool wantFire = Input.TriggerKey(KeyCode.Space) || Input.TriggerGamepad(Gamepad.A);

        /// 発射したいけどプレイヤーが見つかっていない場合は何もしない 
        if (wantFire && player != null)
        {
            // 軍隊をスポーン
            SpawnReinforcements();
        }
    }

    // =========================================================
    // 退散
    // =========================================================

    private void HandleRetreat()
    {
        bool wantRetreat =
            Input.TriggerKey(KeyCode.E) ||
            Input.TriggerGamepad(Gamepad.B);

        if (!wantRetreat) { return; }

        foreach (Entity e in activeReinforcements)
        {
            if (e == null) { continue; }
            Reinforcement r = e.GetScript<Reinforcement>();
            if (r != null) { r.Retreat(); }
        }
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
        Entity e = ecsGroup.CreateEntity("Reinforcement");
        if (e == null) { return; }
        Reinforcement r = e.GetScript<Reinforcement>();
        if (r == null) { return; }
        r.startPosition = spawnPos;
        r.velocity = dir * reinforcementSpeed;
        activeReinforcements.Add(e);
    }
}

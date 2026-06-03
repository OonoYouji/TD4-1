using System;
using System.Collections.Generic;

public class CallingReinforcement : MonoScript
{
    // =========================================================
    // パラメーター
    // =========================================================

    // 左右それぞれのスポーン位置のオフセット
    [SerializeField] public float xOffset             = 2.0f;
    // プレイヤーの後方から何ユニット離れてスポーンするか
    [SerializeField] public float spawnBehindDistance = 5.0f;
    // 援軍を呼べる間隔（クールタイム）
    [SerializeField] public float spawnInterval       = 3.0f;

    // =========================================================
    // 内部状態
    // =========================================================

    // プレイヤーの参照
    private Player player = null;
    // 叩きつけモーションの参照
    private PlayerSmashUp smashUp = null;
    // 現在アクティブな援軍のリスト
    private List<Entity> activeReinforcements = new List<Entity>();
    // 発射クールタイムのタイマー
    private float spawnTimer = 0.0f;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        Entity playerEntity = ecsGroup.FindEntity("Player");
        if (playerEntity != null)
        {
            player  = playerEntity.GetScript<Player>();
            smashUp = playerEntity.GetScript<PlayerSmashUp>();
        }

        // 最初からすぐ呼べるようにタイマーを満タンにしておく
        spawnTimer = spawnInterval;
    }

    public override void Update()
    {
        // 消えた援軍をリストから掃除する
        activeReinforcements.RemoveAll(e => e == null || !e.enable);
        HandleFiring();
        HandleRetreat();
    }

    // =========================================================
    // 発射
    // =========================================================

    // 入力とクールタイムを見て援軍を召喚する
    private void HandleFiring()
    {
        if (player == null)
        {
            return;
        }
        // フリーズ中は召喚できない
        if (player.IsFrozen)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;

        bool wantFire =
            Input.PressMouse(Mouse.Left) ||
            Input.PressGamepad(Gamepad.LeftShoulder) ||
            Input.PressGamepad(Gamepad.RightShoulder);

        if (spawnTimer <= 0.0f && wantFire)
        {
            spawnTimer = spawnInterval;
            SpawnReinforcements();
            // 叩きつけモーションを再生
            smashUp?.Play();
        }
    }

    // =========================================================
    // 退散
    // =========================================================

    // 退散ボタンで全援軍を退散させる
    private void HandleRetreat()
    {
        bool wantRetreat =
            Input.TriggerKey(KeyCode.E) ||
            Input.TriggerGamepad(Gamepad.B);

        if (!wantRetreat)
        {
            return;
        }

        foreach (Entity reinforcementEntity in activeReinforcements)
        {
            if (reinforcementEntity == null)
            {
                continue;
            }

            Reinforcement reinforcement = reinforcementEntity.GetScript<Reinforcement>();
            if (reinforcement != null)
            {
                reinforcement.Retreat();
            }
        }

        // 退散させたのでリストをクリア
        activeReinforcements.Clear();
    }

    // =========================================================
    // スポーン
    // =========================================================

    // プレイヤーの後方左右に1体ずつスポーンする
    private void SpawnReinforcements()
    {
        Matrix4x4 rotMat  = Matrix4x4.Rotate(player.transform.rotate);
        Vector3   forward = Matrix4x4.Transform(Vector3.forward, rotMat);
        Vector3   right   = Matrix4x4.Transform(Vector3.right, rotMat);

        Vector3 spawnBase = player.transform.position - forward * spawnBehindDistance;

        SpawnOne(spawnBase - right * xOffset, forward);
        SpawnOne(spawnBase + right * xOffset, forward);
    }

    // 1体スポーンしてマネージャーに登録する
    private void SpawnOne(Vector3 spawnPos, Vector3 dir)
    {
        Entity reinforcementEntity = ecsGroup.CreateEntity("Reinforcement");
        if (reinforcementEntity == null)
        {
            return;
        }

        // 原点での衝突を防ぐために生成直後に座標をセット
        reinforcementEntity.transform.position = spawnPos;

        Reinforcement reinforcement = reinforcementEntity.GetScript<Reinforcement>();
        if (reinforcement == null)
        {
            return;
        }

        reinforcement.startPosition = spawnPos;
        reinforcement.direction     = dir;

        activeReinforcements.Add(reinforcementEntity);
        // 名前検索に頼らずInstanceを直接使う
        ReinforcementManager.Instance?.AddReinforcement(reinforcementEntity);
    }
}

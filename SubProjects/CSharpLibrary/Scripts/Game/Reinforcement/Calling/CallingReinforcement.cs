using System;
using System.Collections.Generic;

public class CallingReinforcement : MonoScript
{
    // =========================================================
    // パラメーター
    // =========================================================

    [SerializeField] public float xOffset = 2.0f;
    [SerializeField] public float spawnBehindDistance = 5.0f;
    [SerializeField] public float callCollTime = 0.5f;

    // =========================================================
    // 内部状態
    // =========================================================

    private Player player = null;
    private PlayerCallMotion callMotion = null;
    private PlayerAudio playerAudio_ = null;
    private List<Entity> activeReinforcements = new List<Entity>();

    // 最後に援軍を呼んでからの経過時間
    public float spawnTimer { get; private set; } = 0.0f;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        Entity playerEntity = ecsGroup.FindEntity("Player");
        if (playerEntity != null)
        {
            player = playerEntity.GetScript<Player>();
            callMotion = playerEntity.GetScript<PlayerCallMotion>();
        }
        playerAudio_ = entity.GetScript<PlayerAudio>();
    }

    public override void Update()
    {
        spawnTimer += Time.deltaTime;
        activeReinforcements.RemoveAll(e => e == null || e.Id == 0 || !e.enable);
        HandleFiring();
        HandleRetreat();
    }

    // =========================================================
    // 発射
    // =========================================================

    private void HandleFiring()
    {
        if (player == null || player.IsFrozen) { 
            return; 
        }

        if(spawnTimer < callCollTime)
        {
            return;
        }

        bool wantFire =
            Input.PressMouse(Mouse.Left) ||
            Input.PressGamepad(Gamepad.RightShoulder) ||
            Input.PressGamepad(Gamepad.RightThumb);

        if (!wantFire) { return; }

        spawnTimer = 0.0f;
        SpawnReinforcements();
        callMotion?.Play();
        playerAudio_?.PlayCall();
    }

    // =========================================================
    // 退散
    // =========================================================

    private void HandleRetreat()
    {
        bool wantRetreat =
            Input.TriggerMouse(Mouse.Right) ||
            Input.TriggerGamepad(Gamepad.B) ||
        Input.TriggerGamepad(Gamepad.A) ||
        Input.TriggerGamepad(Gamepad.X) ||
        Input.TriggerGamepad(Gamepad.Y);

        if (!wantRetreat) return;

        foreach (Entity reinforcementEntity in activeReinforcements)
        {
            if (reinforcementEntity == null) continue;
            Reinforcement reinforcement = reinforcementEntity.GetScript<Reinforcement>();
            reinforcement?.Retreat();
        }
        activeReinforcements.Clear();
        playerAudio_?.PlayLeave();
    }

    // =========================================================
    // スポーン
    // =========================================================

    private void SpawnReinforcements()
    {
        Matrix4x4 rotMat = Matrix4x4.Rotate(player.transform.rotate);
        Vector3 forward = Matrix4x4.Transform(Vector3.forward, rotMat);
        Vector3 right = Matrix4x4.Transform(Vector3.right, rotMat);
        Vector3 spawnBase = player.transform.position - forward * spawnBehindDistance;
        spawnBase.y = 0.0f;
        SpawnOne(spawnBase - right * xOffset, forward);
        SpawnOne(spawnBase + right * xOffset, forward);
    }

    private void SpawnOne(Vector3 spawnPos, Vector3 dir)
    {
        Entity reinforcementEntity = ecsGroup.CreateEntity("Reinforcement");
        if (reinforcementEntity == null) return;

        // 位置をセット
        reinforcementEntity.transform.position = spawnPos;

        Reinforcement reinforcement = reinforcementEntity.GetScript<Reinforcement>();
        if (reinforcement == null) return;

        reinforcement.startPosition = spawnPos;
        reinforcement.direction = dir;

        activeReinforcements.Add(reinforcementEntity);
        ReinforcementManager.Instance?.AddReinforcement(reinforcementEntity);
    }
}

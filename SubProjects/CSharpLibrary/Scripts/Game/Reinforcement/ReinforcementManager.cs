using System.Collections.Generic;

public class ReinforcementManager : MonoScript
{
    // シングルトンで参照できるようにしておく
    public static ReinforcementManager Instance { get; private set; }

    // =========================================================
    // 内部状態
    // =========================================================

    // 現在アクティブな援軍のEntityリスト
    private List<Entity> reinforcements = new List<Entity>();
    // プレイヤーへのダメージを通すための参照
    private Entity playerEntity = null;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        Instance     = this;
        playerEntity = ecsGroup.FindEntity("Player");
    }

    public override void Update()
    {
        // 死んだEntityは毎フレームリストから除く
        reinforcements.RemoveAll(e => e == null || e.Id == 0);

        if (playerEntity == null)
        {
            playerEntity = ecsGroup.FindEntity("Player");
        }
    }

    // =========================================================
    // 援軍リスト管理
    // =========================================================

    // 援軍をリストに追加して死亡コールバックをセット
    public void AddReinforcement(Entity reinforcementEntity)
    {
        if (reinforcementEntity == null)
        {
            return;
        }

        Reinforcement script = reinforcementEntity.GetScript<Reinforcement>();
        if (script == null)
        {
            return;
        }

        reinforcements.Add(reinforcementEntity);
        script.onDied = OnReinforcementDied;
    }

    // 任意のタイミングで援軍をリストから外す
    public void RemoveReinforcement(Entity reinforcementEntity)
    {
        reinforcements.Remove(reinforcementEntity);
    }

    // 援護バフの範囲チェックなどに使う
    public List<Entity> GetReinforcements()
    {
        return reinforcements;
    }

    // =========================================================
    // 死亡コールバック
    // =========================================================

    // 援軍が死ぬたびにプレイヤーにダメージを1与える
    private void OnReinforcementDied(Reinforcement reinforcement)
    {
        if (playerEntity == null)
        {
            playerEntity = ecsGroup.FindEntity("Player");
        }
        if (playerEntity == null)
        {
            return;
        }

        HP hp = playerEntity.GetScript<HP>();
        if (hp != null)
        {
            hp.TakeDamage(1);
        }
    }
}

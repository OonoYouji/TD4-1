using System.Collections.Generic;

public class ReinforcementManager : MonoScript
{

    // =========================================================
    // 内部状態
    // =========================================================

    // 管理中の援軍エンティティリスト
    private List<Entity> reinforcements = new List<Entity>();

    private Entity playerEntity = null;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        playerEntity = ecsGroup.FindEntity("Player");
    }

    public override void Update()
    {
        // 破棄済みエンティティをリストから除去
        reinforcements.RemoveAll(e => e == null);

        // プレイヤーEntityが見つからない場合、再度探す
        if (playerEntity == null) {
            playerEntity = ecsGroup.FindEntity("Player"); 
        }
    }

    // =========================================================
    // 援軍リスト管理
    // =========================================================

    // 援軍をリストに追加し、コールバックをセットする
    public void AddReinforcement(Entity reinforcementEntity)
    {
        if (reinforcementEntity == null) {
            return; 
        }

        // Reinforcementスクリプトを取得
        Reinforcement script = reinforcementEntity.GetScript<Reinforcement>();
        if (script == null) { 
            return; 
        }

        // リストに追加、死亡コールバックをセット
        reinforcements.Add(reinforcementEntity);
        script.onDied = OnReinforcementDied;
    }

    // 援軍をリストから削除する
    public void RemoveReinforcement(Entity reinforcementEntity)
    {
        reinforcements.Remove(reinforcementEntity);
    }

    // 管理中の援軍リストを取得する
    public List<Entity> GetReinforcements()
    {
        return reinforcements;
    }

    // =========================================================
    // 死亡コールバック
    // =========================================================

    // 援軍が死亡してプレイヤーがダメージを受ける
    private void OnReinforcementDied(Reinforcement reinforcement)
    {
        if (playerEntity == null)
        {
            playerEntity = ecsGroup.FindEntity("Player");
        }
        if (playerEntity == null) { 
            return; 
        }

        // プレイヤーのHPを減らす
        HP hp = playerEntity.GetScript<HP>();
        if (hp != null)
        {
            hp.TakeDamage(1);
        }
    }
}

public partial class Reinforcement
{
    // =========================================================
    // 援護バフ
    // =========================================================

    // 穴にはまった時に周囲の援軍にバフを配る
    private void ApplySupportBuff()
    {

        // ReinforcementManagerのnullチェック
        if (ReinforcementManager.Instance == null)
        {
            Debug.Log("<color=red>[SupportBuff]</color> ReinforcementManager.Instance is null.");
            return;
        }

        //  援軍のリストを取得
        var reinforcements = ReinforcementManager.Instance.GetReinforcements();
        int buffed = 0;

        foreach (var e in reinforcements)
        {
            // 自分自身はスキップ
            if (e == null || e.Id == entity.Id)
            {
                continue;
            }

            // 範囲外もスキップ
            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist > supportBuffRadius)
            {
                continue;
            }

            // 一定距離の兵士にバフを付与
            Reinforcement reinforcement = e.GetScript<Reinforcement>();
            if (reinforcement != null)
            {
                reinforcement.ReceiveSupportBuff();
                buffed++;
            }
        }

        Debug.Log($"<color=cyan>[SupportBuff]</color> {entity.name} buffed {buffed} units. (radius={supportBuffRadius})");
    }

    // バフを受け取ってSupportedに切り替える
    public void ReceiveSupportBuff()
    {

        // 既にバフを受けてる
        if (state_ == ReinforcementState.Supported)
        {
            return;
        }

        // サポートを受ける
        state_  = ReinforcementState.Supported;
        damage  = supportedDamage;

        // スケールアップアニメーションを再生
        ReinforcementAnimation anim = entity.GetScript<ReinforcementAnimation>();
        anim?.Play(transform.scale.x, supportedScale);

        Debug.Log($"<color=yellow>[SupportBuff:Recv]</color> {entity.name} → Supported ({transform.scale.x:F2}→{supportedScale}, dmg={supportedDamage})");
    }
}

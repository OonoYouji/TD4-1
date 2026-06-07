public partial class Reinforcement
{
    // =========================================================
    // 状態スケール
    // =========================================================

    private bool scaleLoggedOnce_ = false;
    private ReinforcementState lastAppliedState_ = ReinforcementState.Normal;

    // 状態が変化した時だけスケールを書き込む
    private void ApplyStateScale()
    {
        if (state_ == lastAppliedState_)
        {
            return;
        }
        float target = normalScale;
        if (state_ == ReinforcementState.Supported)
        {
            target = supportedScale;
            if (!scaleLoggedOnce_)
            {
                Debug.Log($"<color=orange>[ApplyStateScale]</color> {entity.name} scale → {supportedScale}");
                scaleLoggedOnce_ = true;
            }
        }
        transform.scale = new Vector3(target, target, target);
        lastAppliedState_ = state_;
    }

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

            // バフを付与
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

        // すでにSupported状態なら何もしない
        if (state_ == ReinforcementState.Supported)
        {
            return;
        }

        // Supported状態に切り替える
        state_ = ReinforcementState.Supported;
        damage  = supportedDamage;
        Debug.Log($"<color=yellow>[SupportBuff:Recv]</color> {entity.name} → Supported (scale={supportedScale}, dmg={supportedDamage})");
    }
}

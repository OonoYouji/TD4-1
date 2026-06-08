public partial class Reinforcement
{
    // =========================================================
    // フィールド落下インターフェース
    // =========================================================

    private ReinforcementFallIn fallIn_ = null;

    public bool IsTrapped => fallIn_ != null && fallIn_.IsActive;

    // 打ち上げ中チェック
    private bool CheckLaunchExit()
    {
        if (fallIn_ == null || !fallIn_.IsLaunched)
        {
            return false;
        }
        if (!IsInScreenFrustum())
        {
            isRetreating = true;
            entity.Destroy();
        }
        return true;
    }

    // はまり中チェック
    private bool CheckTrappedActive()
    {
        return fallIn_ != null && fallIn_.IsActive;
    }

    // 自分がいるセルが落下中かどうかを毎フレームチェック
    private void CheckFieldFall()
    {

        // FieldManagerがない場合スキップ
        if (fieldManager_ == null)
        {
            return;
        }

        // グリッド範囲内かチェック
        int row, col;
        if (!fieldManager_.WorldToGrid(transform.position, out row, out col))
        {
            return;
        }

        // その位置のフィールドエンティティを取得
        Entity cellEntity = fieldManager_.GetCellAt(row, col);
        if (cellEntity == null)
        {
            return;
        }

        // 
        FieldFall fieldFall = cellEntity.GetScript<FieldFall>();
        // 落下状態のセルかチェック
        if (fieldFall == null || !fieldFall.IsPlaying)
        {
            return;
        }

        // 落下開始
        fieldFall.TrapReinforcement(this);
    }

    public void TrapToCell(float stuckY, float cellCenterX, float cellCenterZ)
    {

        // 落下開始
        fallIn_?.StartFallIn(stuckY, cellCenterX, cellCenterZ);

        // コリジョン無効化
        isCollisionEnabled = false;
        if (!supportBuffApplied_)
        {
            // 援護兵モード
            ApplySupportBuff();
            supportBuffApplied_ = true;
        }
    }

    // 床が戻るタイミングで上に打ち上げる
    public void LaunchUpward()
    {
        // 打ち上げ開始
        isCollisionEnabled = false;
        fallIn_?.Launch();
    }
}

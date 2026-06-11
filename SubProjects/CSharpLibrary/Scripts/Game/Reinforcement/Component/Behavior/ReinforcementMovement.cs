public partial class Reinforcement
{
    // =========================================================
    // 移動
    // =========================================================

    // 通常移動か退散移動かを振り分ける
    private void UpdateMovement()
    {
        if (isRetreating)
        {
            UpdateRetreatMovement();
            return;
        }
        // 前方向にまっすぐ進む
        transform.position += direction.Normalized() * moveSpeed * Time.deltaTime;
    }

    // 退散中の移動、画面外に出たら消える
    private void UpdateRetreatMovement()
    {
        DisableCollisionAndRestoreColor();
        transform.position += retreatVelocity * Time.deltaTime;
        if (!IsInScreenFrustum())
        {
            entity.Destroy();
        }
    }

    // 退散開始時に当たり判定を切って元の色に戻す
    private void DisableCollisionAndRestoreColor()
    {
        if (!isCollisionEnabled)
        {
            return;
        }
        isCollisionEnabled = false;
        MeshRenderer meshRenderer = entity.GetComponent<MeshRenderer>();
        if (meshRenderer != null && colorSaved)
        {
            meshRenderer.color = originalColor;
        }
    }

    // =========================================================
    // 退散
    // =========================================================

    // 退散を開始する
    public void Retreat()
    {
        if (isRetreating)
        {
            return;
        }
        isRetreating    = true;
        retreatVelocity = ComputeRetreatVelocity();

        BoundsConstraint bc = entity.GetScript<BoundsConstraint>();
        if (bc != null) bc.enable = false;
    }

    // プレイヤーから離れる方向に退散速度を計算する
    private Vector3 ComputeRetreatVelocity()
    {
        Vector3 retreatDir = direction.Normalized();
        if (playerEntity != null)
        {
            Vector3 toSelf = transform.position - playerEntity.transform.position;
            if (toSelf.Length() > 0.001f)
            {
                retreatDir = toSelf.Normalized();
            }
        }
        return retreatDir * retreatSpeed;
    }
}

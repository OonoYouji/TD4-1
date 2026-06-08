public partial class Reinforcement
{
    // =========================================================
    // 画面内判定 / 色
    // =========================================================

    // 画面内なら赤く、画面外なら元の色に戻す
    private void UpdateFrustumVisibility()
    {
        if (isRetreating)
        {
            return;
        }

        MeshRenderer meshRenderer = entity.GetComponent<MeshRenderer>();
        TrySaveOriginalColor(meshRenderer);

        bool inFrustum = IsInScreenFrustum();
        if (inFrustum)
        {
            if (meshRenderer != null)
            {
                meshRenderer.color = new Vector4(1.0f, 0.5f, 0.5f, 1.0f);
            }
        }
        else
        {
            if (meshRenderer != null && colorSaved)
            {
                meshRenderer.color = originalColor;
            }
        }
    }

    // 元の色を1回だけ保存する
    private void TrySaveOriginalColor(MeshRenderer meshRenderer)
    {
        if (colorSaved || meshRenderer == null)
        {
            return;
        }
        originalColor = meshRenderer.color;
        colorSaved = true;
    }

    // カメラの視野内にいるかどうかを判定する
    private bool IsInScreenFrustum()
    {
        if (cameraEntity == null)
        {
            return false;
        }

        Vector3 cameraPos = cameraEntity.transform.position;
        Vector3 toTarget  = transform.position - cameraPos;
        float   distance  = toTarget.Length();

        // カメラに近すぎる場合は画面内とみなす
        if (distance <= 0.1f)
        {
            return true;
        }

        // カメラの向きとの角度で画面内かどうかを判定
        Vector3 cameraForward = GetCameraForward(cameraPos);
        toTarget = toTarget.Normalized();

        float dot          = Vector3.Dot(cameraForward, toTarget);
        float halfAngleRad = (viewAngle * 0.5f) * Mathf.Deg2Rad;
        float threshold    = Mathf.Cos(halfAngleRad);

        return dot >= threshold;
    }

    // カメラからプレイヤー方向を前方とする
    private Vector3 GetCameraForward(Vector3 cameraPos)
    {
        if (playerEntity != null)
        {
            Vector3 camToPlayer = playerEntity.transform.position - cameraPos;
            if (camToPlayer.Length() > 0.001f)
            {
                return camToPlayer.Normalized();
            }
        }
        return new Vector3(0, -1, 0);
    }
}

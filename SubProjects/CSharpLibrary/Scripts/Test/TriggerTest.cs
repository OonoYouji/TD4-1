using System;

/// <summary>
/// Triggerモードの動作を確認するためのテストスクリプト。
/// 衝突を検知するとログを出力します。
/// </summary>
public class TriggerTest : MonoScript
{
    private MeshRenderer renderer;
    private float resetTimer = 0.0f;

    public override void Initialize()
    {
        renderer = entity.GetComponent<MeshRenderer>();
    }

    public override void Update()
    {
        if (resetTimer > 0)
        {
            resetTimer -= Time.deltaTime;
            if (resetTimer <= 0)
            {
                if (renderer != null) renderer.color = Vector4.one; // デフォルト: 白
            }
        }
    }

    public override void OnCollisionEnter(Entity other)
    {
        if (renderer != null) renderer.color = new Vector4(1, 0, 0, 1); // 衝突発生: 赤
        resetTimer = 0.0f; // Stayに任せるかタイマーを止める
    }

    public override void OnCollisionStay(Entity other)
    {
        if (renderer != null) renderer.color = new Vector4(0, 0, 1, 1); // 衝突中: 青
        resetTimer = 0.1f; // 常に更新
    }

    public override void OnCollisionExit(Entity other)
    {
        if (renderer != null) renderer.color = new Vector4(0, 1, 0, 1); // 衝突終了: 緑
        resetTimer = 1.0f; // 1秒後に白に戻す
    }
}


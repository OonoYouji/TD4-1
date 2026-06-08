using System;

/// <summary>
/// プレイヤーのHPを画面上に表示するためのUIゲージ。
/// ボスのHP UIと同様の構造を持ち、背景と連動して動作します。
/// </summary>
public class PlayerHPGauge : MonoScript
{
    [SerializeField]
    public float maxScaleX = 1.9f;
    [SerializeField]
    public float height = 0.1f; // スケールが調整されているため高さも小さく設定
    [SerializeField]
    public string targetEntityName = "Player";

    private HP playerHp;
    private float defaultX = 0;
    private float lastLoggedRatio = -1.0f;

    public override void Initialize()
    {
        defaultX = transform.position.x;
        FindTarget();
    }

    private void FindTarget()
    {
        Entity target = ecsGroup.FindEntity(targetEntityName);
        if (target != null)
        {
            playerHp = target.GetScript<HP>();
        }
    }

    public override void Update()
    {
        if (playerHp == null)
        {
            FindTarget();
            return;
        }

        float hpRatio = playerHp.CurrentHpRatio();

        // ゲージのスケールと位置を更新
        // ユーザー指定の最大スケール(1.9)を超えないように計算
        float currentScaleX = maxScaleX * hpRatio;
        transform.scale = new Vector3(currentScaleX, height, 1.0f);
        
        // 左端を固定して右側を縮める計算
        transform.position.x = Mathf.Lerp(defaultX, -maxScaleX / 2.0f + defaultX, 1.0f - hpRatio);
    }
}

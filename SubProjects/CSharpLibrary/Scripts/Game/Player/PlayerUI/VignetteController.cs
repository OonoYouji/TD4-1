using System;

/// <summary>
/// プレイヤーのHPに連動してビネット（画面端の暗まり）をドクドクと拍動させるスクリプト。
/// HPが低いほど拍動が速く、強くなります。
/// </summary>
public class VignetteController : MonoScript
{
    [SerializeField] public float baseAlpha = 0.5f;      // 基本の透明度
    [SerializeField] public float pulsateIntensity = 0.3f; // 拍動の強さ
    [SerializeField] public float minHpThreshold = 0.4f;   // 演出を開始するHP割合の閾値

    private SpriteRenderer spriteRenderer;
    private HP playerHp;
    private float timer = 0.0f;

    public override void Initialize()
    {
        spriteRenderer = entity.GetComponent<SpriteRenderer>();
        FindPlayer();
    }

    private void FindPlayer()
    {
        Entity player = ecsGroup.FindEntity("Player");
        if (player != null)
        {
            playerHp = player.GetScript<HP>();
        }
    }

    public override void Update()
    {
        if (playerHp == null)
        {
            FindPlayer();
            return;
        }

        float hpRatio = playerHp.CurrentHpRatio();

        // HPが一定以下になったら演出を強める
        if (hpRatio <= minHpThreshold)
        {
            // 危険度 (0.0=瀕死, 1.0=閾値ちょうど)
            float dangerLevel = 1.0f - (hpRatio / minHpThreshold);
            
            // 拍動の速さをHPに応じて変える
            timer += Time.deltaTime * (2.0f + dangerLevel * 5.0f);
            float pulse = (float)Math.Sin(timer);
            
            // 赤色に変更し、透明度とスケールを変化させる
            float alpha = baseAlpha + (pulse * pulsateIntensity * dangerLevel);
            spriteRenderer.color = new Vector4(1, 0, 0, Mathf.Clamp01(alpha)); // 赤色 (R:1, G:0, B:0)
            
            // サイズを大きくして画面外まで覆うように (ベースを20%アップ)
            float scaleBase = 1.2f + (pulse * 0.1f * dangerLevel);
            transform.scale = new Vector3(1920.0f * scaleBase, 1080.0f * scaleBase, 1.0f);
            
            spriteRenderer.enable = 1;
        }
        else
        {
            // HPが十分ある時は非表示
            spriteRenderer.enable = 0;
            timer = 0.0f;
        }
    }
}

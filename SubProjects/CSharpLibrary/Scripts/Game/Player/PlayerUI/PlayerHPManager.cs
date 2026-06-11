using System;

/// <summary>
/// プレイヤーのHP UI（背景とゲージ）を一括管理するクラス。
/// インスペクターから背景とゲージの最小・最大スケールを調整可能です。
/// </summary>
public class PlayerHPManager : MonoScript
{
    float gaugeWidth = 110.0f;
    float gaugeHeight = 30.0f;

    [SerializeField] public string targetEntityName = "Player";

    private HP playerHp;
    private Entity bgEntity;
    private Entity gaugeEntity;

    private float gaugeY = 0.0f;
    private float left = 0.0f;

    public override void Initialize()
    {
        FindComponents();
    }

    private void FindComponents()
    {
        // ターゲットのHPを取得
        Entity target = ecsGroup.FindEntity(targetEntityName);
        if (target != null)
        {
            playerHp = target.GetScript<HP>();
        }

        // 子要素（BGとGauge）を取得
        uint childCount = entity.GetChildCount();
        for (uint i = 0; i < childCount; i++)
        {
            Entity child = entity.GetChild(i);
            if (child == null) continue;

            if (child.name.Contains("BG")) bgEntity = child;
            else if (child.name.Contains("Gauge")) gaugeEntity = child;
        }

        if (gaugeEntity)
        {
            gaugeWidth = gaugeEntity.transform.scale.x;
            gaugeHeight = gaugeEntity.transform.scale.y;

            left = gaugeEntity.transform.position.x - (gaugeWidth / 2.0f);
            gaugeY = gaugeEntity.transform.position.y;
        }
    }

    public override void Update()
    {
        if (playerHp == null || bgEntity == null || gaugeEntity == null)
        {
            FindComponents();
            return;
        }

        float hpRatio = playerHp.CurrentHpRatio();

        // 1. ゲージの更新 (左端固定・右端が減る)
        float gScaleX = Mathf.Lerp(0.0f, gaugeWidth, hpRatio);
        gaugeEntity.transform.scale = new Vector3(gScaleX, gaugeHeight, 1.0f);
        
        // ゲージの左端をBGの左端に合わせる計算
        // 親(PlayerHP)のローカル0が中心、BGの幅は bgMaxX
        // BGの左端は -bgMaxX / 2.0
        // ゲージの左端をそこに合わせるには、現在の中心を (-bgMaxX / 2.0 + gScaleX / 2.0) に置く
        float gPosX = left + (gScaleX / 2.0f);
        gaugeEntity.transform.position = new Vector3(gPosX, gaugeY, -0.01f);

        SpriteRenderer gaugeRenderer = gaugeEntity.GetComponent<SpriteRenderer>();
        if (gaugeRenderer != null)
        {
            // 歪まないようUVを調整
            float uvScaleX = hpRatio; // HP比率に応じてUVの幅を調整
            UVTransform uv = gaugeRenderer.uvTransform;
            uv.scale = new Vector2(uvScaleX, 1.0f);
            gaugeRenderer.uvTransform = uv;
        }
    }
}

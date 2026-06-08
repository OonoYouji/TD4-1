using System;

/// <summary>
/// プレイヤーのHP UI（背景とゲージ）を一括管理するクラス。
/// インスペクターから背景とゲージの最小・最大スケールを調整可能です。
/// </summary>
public class PlayerHPManager : MonoScript
{
    [SerializeField] public float gaugeMinX = 0.0f;
    [SerializeField] public float gaugeMaxX = 1.9f;
    [SerializeField] public float gaugeHeight = 0.1f;

    [SerializeField] public float bgMinX = 2.0f;
    [SerializeField] public float bgMaxX = 2.0f;
    [SerializeField] public float bgHeight = 0.12f;

    [SerializeField] public string targetEntityName = "Player";

    private HP playerHp;
    private Entity bgEntity;
    private Entity gaugeEntity;

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
        float gScaleX = Mathf.Lerp(gaugeMinX, gaugeMaxX, hpRatio);
        gaugeEntity.transform.scale = new Vector3(gScaleX, gaugeHeight, 1.0f);
        
        // ゲージの左端をBGの左端に合わせる計算
        // 親(PlayerHP)のローカル0が中心、BGの幅は bgMaxX
        // BGの左端は -bgMaxX / 2.0
        // ゲージの左端をそこに合わせるには、現在の中心を (-bgMaxX / 2.0 + gScaleX / 2.0) に置く
        float leftEdge = -bgMaxX / 2.0f;
        float gPosX = leftEdge + (gScaleX / 2.0f);
        gaugeEntity.transform.position = new Vector3(gPosX, 0, -0.01f);

        // 2. 背景の更新
        bgEntity.transform.scale = new Vector3(bgMaxX, bgHeight, 1.0f);
        bgEntity.transform.position = new Vector3(0, 0, 0); // 背景は中央固定
    }
}

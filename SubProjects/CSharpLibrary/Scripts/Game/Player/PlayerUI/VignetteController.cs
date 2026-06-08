using System;

/// <summary>
/// プレイヤーのHPが減少した（ダメージを受けた）瞬間に、
/// 一瞬だけ画面を赤くフラッシュさせるスクリプト。
/// </summary>
public class VignetteController : MonoScript
{
    [SerializeField] public float maxAlpha = 0.6f;         // 被弾した瞬間の最大透明度
    [SerializeField] public float fadeDuration = 0.4f;     // 消えるまでの時間
    
    private SpriteRenderer spriteRenderer;
    private HP playerHp;
    
    private float flickerTimer = 0.0f;
    private int lastHp = -1;
    
    private readonly Vector2 ScreenSize = new Vector2(1920.0f, 1080.0f);

    public override void Initialize()
    {
        spriteRenderer = entity.GetComponent<SpriteRenderer>();
        
        // 初期状態は非表示
        if (spriteRenderer != null)
        {
            spriteRenderer.enable = 0;
            spriteRenderer.color = new Vector4(1, 0, 0, 0); // 透明な赤
        }
        
        FindPlayer();
    }

    private void FindPlayer()
    {
        Entity player = ecsGroup.FindEntity("Player");
        if (player != null)
        {
            playerHp = player.GetScript<HP>();
            if (playerHp != null)
            {
                lastHp = playerHp.currentHp;
            }
        }
    }

    public override void Update()
    {
        if (playerHp == null)
        {
            FindPlayer();
            return;
        }

        // 1. ダメージ検知 (前フレームよりHPが減っていたら発動)
        if (playerHp.currentHp < lastHp)
        {
            flickerTimer = fadeDuration;
            // Debug.Log($"[Vignette] Damage! {lastHp} -> {playerHp.currentHp}");
        }
        lastHp = playerHp.currentHp;

        // 2. 演出の更新
        if (flickerTimer > 0)
        {
            flickerTimer -= Time.deltaTime;
            
            // 経過割合 (1.0 -> 0.0)
            float t = flickerTimer / fadeDuration;
            
            // 指数関数的にフェードアウト (最初パッと出て、スッと消える)
            float alpha = Mathf.Lerp(0.0f, maxAlpha, t * t);
            spriteRenderer.color = new Vector4(1, 0, 0, alpha);
            
            // 被弾の衝撃を演出 (少し縮小しながら戻る)
            float scaleMult = 1.0f + (0.1f * t);
            transform.scale = new Vector3(ScreenSize.x * scaleMult, ScreenSize.y * scaleMult, 1.0f);
            
            spriteRenderer.enable = 1;
        }
        else
        {
            // 演出が完全に終わったら無効化
            if (spriteRenderer.enable == 1)
            {
                spriteRenderer.enable = 0;
                spriteRenderer.color = new Vector4(1, 0, 0, 0);
                transform.scale = new Vector3(ScreenSize.x, ScreenSize.y, 1.0f);
            }
        }
    }
}

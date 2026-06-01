using System;

/// <summary>
/// 落下する岩の挙動を制御するスクリプト。
/// 地面やプレイヤーに接触した際に範囲ダメージとスタンを発生させる。
/// </summary>
public class FallingRock : MonoScript
{
    public float fallSpeed = 30.0f;
    public float impactRadius = 3.0f;
    public int damage = 40;
    public float stunDuration = 2.0f;

    private bool _isFalling = false;
    private Vector3 _targetPos;
    private bool _hasImpacted = false;

    public override void Initialize()
    {
        _isFalling = false;
        _hasImpacted = false;
    }

    /// <summary>
    /// 落下を開始させる。
    /// </summary>
    public void Launch(Vector3 targetPos)
    {
        _targetPos = targetPos;
        _isFalling = true;
        _hasImpacted = false;
        Debug.Log($"[FallingRock] Launched towards {Vector3.ToSimpleString(targetPos)}");
    }

    public override void Update()
    {
        if (!_isFalling || _hasImpacted) return;

        // 下方向に移動
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // 地面付近（Y座標がターゲットとほぼ同じ）になったら着弾
        if (transform.position.y <= _targetPos.y + 0.5f)
        {
            Impact();
        }
    }

    public override void OnCollisionEnter(Entity collision)
    {
        if (collision == null || collision.Id == 0) return;

        if (_isFalling && !_hasImpacted)
        {
            // 何かに当たったら即着弾（プレイヤーへの直撃も含む）
            Impact();
        }
    }

    private void Impact()
    {
        if (_hasImpacted) return;
        _hasImpacted = true;
        _isFalling = false;

        // 着弾位置を固定
        transform.position = new Vector3(transform.position.x, _targetPos.y, transform.position.z);

        // 範囲内のプレイヤーや援軍にダメージとスタンを与える
        var entities = entity.Group.GetEntities();
        foreach (var e in entities)
        {
            if (e == null || e.Id == 0) continue;

            if (e.name.Contains("Player") || e.name.Contains("Reinforcement"))
            {
                float dist = Vector3.Distance(transform.position, e.transform.position);
                if (dist <= impactRadius)
                {
                    ApplyImpact(e);
                }
            }
        }

        // 演出
        FrameEvent.EnqueueNamedEvent("Effect_RockImpact", entity.Id);
        Debug.Log($"<color=brown>[FallingRock]</color> IMPACT at {Vector3.ToSimpleString(transform.position)}");
    }

    private void ApplyImpact(Entity e)
    {
        // 1. 汎用 DamageHandler 経由
        DamageHandler handler = e.GetScript<DamageHandler>();
        if (handler != null)
        {
            handler.ApplyDamage(damage, transform.position);
        }
        else
        {
            // 2. 援軍専用 ReinforcementDamageHandler 経由
            ReinforcementDamageHandler rHandler = e.GetScript<ReinforcementDamageHandler>();
            if (rHandler != null)
            {
                rHandler.ApplyDamage(damage, transform.position);
            }
        }

        // スタン（AgentIntentComponentの移動制限）
        var intent = e.GetComponent<AgentIntentComponent>();
        if (intent != null)
        {
            Debug.Log($"[FallingRock] STUNNED {e.name} for {stunDuration}s");
        }
    }
}

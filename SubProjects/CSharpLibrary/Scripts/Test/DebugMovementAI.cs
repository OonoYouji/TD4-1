using System;

/// <summary>
/// フェーズ15の移動・回転・攻撃ロック機能を検証するためのAI
/// </summary>
public class DebugMovementAI : MonoScript
{
    private AgentIntentComponent _intent;
    private float _timer = 0;
    private int _phase = 0;

    public override void Initialize()
    {
        _intent = entity.GetComponent<AgentIntentComponent>();
        if (_intent == null)
        {
            _intent = entity.AddComponent<AgentIntentComponent>();
        }
        
        // 旋回速度をテスト用に設定 (低めに設定して補間を見やすくする)
        _intent.rotationSpeed = 2.0f;
        
    }

    public override void Update()
    {
        _timer += Time.deltaTime;

        // 5秒ごとにフェーズを切り替えて各機能をテスト
        if (_timer > 5.0f)
        {
            _timer = 0;
            _phase = (_phase + 1) % 3;
        }

        switch (_phase)
        {
            case 0: // フェーズ0: 加減速のテスト
                // (1, 0, 1) 方向に移動を指示。ログで speed が徐々に 5.0 に上がるのを確認。
                _intent.desiredMoveDirection = new Vector3(1, 0, 1);
                _intent.useDesiredRotation = false;
                _intent.isAttacking = false;
                break;

            case 1: // フェーズ1: スムーズな回転のテスト
                // 移動を止め、(0, 0, -1) の方向（後ろ）を向くように指示。
                _intent.desiredMoveDirection = Vector3.zero;
                _intent.useDesiredRotation = true;
                _intent.desiredRotation = Quaternion.LookAt(Vector3.zero, new Vector3(0, 0, -1), Vector3.up);
                _intent.isAttacking = false;
                break;

            case 2: // フェーズ2: 攻撃中の移動ロックテスト
                // 移動方向は指示しているが、isAttacking を true にする。
                // ログで speed が 0 になり、移動が止まるのを確認。
                _intent.desiredMoveDirection = new Vector3(1, 0, 1);
                _intent.isAttacking = true;
                break;
        }
    }
}


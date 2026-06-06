using System;

/// <summary>
/// 指定された時間経過後に特定のアクションを実行し、自身を破棄するスクリプト。
/// </summary>
public class DelayedAction : MonoScript
{
    public float delay = 1.0f;
    public Action action = null;
    private float timer = 0.0f;

    public override void Update()
    {
        timer += Time.deltaTime;
        if (timer >= delay)
        {
            if (action != null)
            {
                action.Invoke();
            }
            entity.Destroy();
        }
    }
}

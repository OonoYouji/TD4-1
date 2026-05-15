using System;

public class GoalPoint : MonoScript
{
    // ゴールできるかどうかのフラグ
    [SerializeField] public bool canGoal = false;
    
    // 次のシーン名
    [SerializeField] public string nextSceneName = "TitleScene";

    public override void OnCollisionEnter(Entity collision)
    {
        // フラグがtrueの時にゴールできる
        if (!canGoal) return;

        // プレイヤーのコライダーがゴールオブジェクトのコライダーに衝突したらゴール
        if (collision.name == "Player")
        {
            Debug.Log("Goal! Transitioning to scene: " + nextSceneName);
            
            // ゴール後はシーンを遷移する
            SceneManager.LoadScene(nextSceneName);
        }
    }
}

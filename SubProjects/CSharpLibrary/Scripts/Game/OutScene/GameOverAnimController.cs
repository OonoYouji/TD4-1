using ONEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

class GameOverAnimController : MonoScript
{
    [SerializeField]
    float ESCAPE_SPEED = 5f;

    List<Entity> humans = new List<Entity>();
    bool isEscaping = false;

    Entity boss;
    AnimationPlayer bossPlayer;

    public override void Initialize()
    {
        boss = ecsGroup.FindEntity("Boss");
        if (boss != null)
        {
            bossPlayer = boss.GetComponent<AnimationPlayer>();
        }

        for (int i = 1; ; i++)
        {
            var human = ecsGroup.FindEntity("Human" + i);
            if (human != null)
            {
                humans.Add(human);
            }
            else
            {
                break;
            }
        }
        Debug.LogInfo("GameOverAnimController: Found " + humans.Count + " humans.");
    }

    public override void Update()
    {
        if (bossPlayer == null)
        {
            return;
        }

        if (bossPlayer.IsPlaying == false && isEscaping == false)
        {
            isEscaping = true;
            // 後ろを向く
            foreach (Entity human in humans)
            {
                Quaternion baseRotate = human.transform.rotate;
                human.transform.rotation = Quaternion.MakeFromAxis(Vector3.up, Mathf.PI) * baseRotate;
            }

            Animator animator= boss.GetComponent<Animator>();
            if (animator != null)
            {
                // アニメーション変更
                animator.CrossFadeWithDuration("rock", 0.5f, 0.1f);
                animator.SetLoop(true, 0);
            }
        }

        if (isEscaping)
        {
            foreach (Entity human in humans)
            {
                // 前に進む
                Quaternion baseRotate = human.transform.rotate;
                Vector3 forward = baseRotate * Vector3.back; // なぜか後ろ向きで前に進む <- ？？？
                Vector3 velocity = forward * ESCAPE_SPEED;
                human.transform.position += velocity * Time.deltaTime;

                // サイズをだんだん小さくする
                human.transform.scale *= 0.993f;
            }
        }
    }
}

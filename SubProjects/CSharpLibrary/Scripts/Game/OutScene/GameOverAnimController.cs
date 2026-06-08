using ONEngine;
using System.Collections.Generic;

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

        for (int i = 0; ; i++)
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
                Vector3 forward = human.transform.forward;
                Vector3 backword = -forward;
                human.transform.rotation = Quaternion.LookRotation(backword);
            }
        }

        if (isEscaping)
        {
            foreach (Entity human in humans)
            {
                Vector3 forward = human.transform.forward;
                Vector3 velocity = forward * ESCAPE_SPEED;
                human.transform.position += velocity * Time.deltaTime;
            }
        }
    }
}

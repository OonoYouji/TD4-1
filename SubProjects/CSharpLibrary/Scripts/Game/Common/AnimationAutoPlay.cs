using ONEngine;

class AnimationAutoPlay : MonoScript
{
    [SerializeField]
    public float DELAY = 0.0f;

    float timer = 0.0f;
    AnimationPlayer player;

    public override void Initialize()
    {
        player = entity.GetComponent<AnimationPlayer>();
        if (player != null)
        {
            player.Stop();
        }
    }

    public override void Update()
    {
        if (player == null)
            return;
    
        timer += Time.deltaTime;
        if (timer >= DELAY)
        {
            player.Play();
            player = null;
        }
    }
}

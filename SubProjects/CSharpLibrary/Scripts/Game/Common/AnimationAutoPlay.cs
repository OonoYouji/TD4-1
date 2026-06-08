using ONEngine;

class AnimationAutoPlay : MonoScript
{
    public override void Initialize()
    {
        AnimationPlayer player = entity.GetComponent<AnimationPlayer>();
        if (player != null)
        {
            player.Play();
        }
    }
}

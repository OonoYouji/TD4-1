public enum UTurnType
{
    Left, Right
}


public class PlayerBulletLauncher : MonoScript
{
    ///
    /// 基礎
    ///
    [SerializeField]
    Vector3 offset = new Vector3(0, 0, 1);

    [SerializeField]
    public Vector3 launchDirection = new Vector3(0, 0, 1);

    [SerializeField]
    readonly float bulletSpeed = 10.0f;

    public override void Initialize() { }

    public override void Update()
    {
    }

    public void FireBullet(UTurnType type)
    {
        var bullet = ecsGroup.CreateEntity("PlayerBullet");
        bullet.transform.position = transform.position + offset;

        PlayerBullet bs = bullet.GetScript<PlayerBullet>();
        bs.velocity = launchDirection.Normalized() * bulletSpeed;
        bs.uTurnType = type;
    }

}

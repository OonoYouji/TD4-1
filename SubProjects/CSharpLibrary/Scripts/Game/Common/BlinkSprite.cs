using System.Threading;

class BlinkSprite : MonoScript
{
    [SerializeField]
    private float BLINK_CYCLE = 1.0f; // 点滅の周期（秒）

    private SpriteRenderer renderer;
    private Vector4 color;

    private float timer = 0.0f;

    public override void Initialize()
    {
        renderer = entity.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning("BlinkSprite: SpriteRenderer component not found on the entity!");
        }
        else
        {
            color = renderer.color;
        }
    }

    public override void Update()
    {
        if (renderer == null)
        {
            return;
        }

        timer += Time.deltaTime;
        // 0から1の範囲で点滅
        timer %= BLINK_CYCLE;
        color.z = Mathf.Cos(-Mathf.PI * 2 * timer) * 0.5f + 0.5f;
        Debug.LogInfo($"BlinkSprite: Timer={timer}, Alpha={color.z}");
        renderer.color = color; // TODO: C#のreadbackが動いてない可能性
    }
}

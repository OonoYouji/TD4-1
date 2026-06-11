class Bound : MonoScript
{
    [SerializeField]
    public float START_Y;

    [SerializeField]
    public float COEFFICIENT_OF_RESTITUTION;

    [SerializeField]
    public float START_VELOCITY_Y;

    float velocityY;

    const float THRESHOLD = 0.01f;

    public override void Initialize()
    {
        velocityY = START_VELOCITY_Y;
        transform.position.y = START_Y;
    }

    public override void Update()
    {
        if (velocityY <= THRESHOLD && transform.position.y <= THRESHOLD)
        {
            // ほぼ止まってる
            transform.position.y = 0.0f;
            velocityY = 0.0f;
            return;
        }

        velocityY += Mathf.Gravity * Time.deltaTime;
        transform.position.y += velocityY * Time.deltaTime;
        if (transform.position.y <= 0.0f)
        {
            velocityY *= -COEFFICIENT_OF_RESTITUTION;
            transform.position.y = 0.0f;
        }
    }
}

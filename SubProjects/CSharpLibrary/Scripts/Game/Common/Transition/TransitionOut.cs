class TransitionOut : MonoScript
{
    Entity left;
    Entity right;

    [SerializeField]
    public float CLOSE_TIME = 0.3f;
    [SerializeField]
    public float SHAKE_START_TIME = 1.0f;
    [SerializeField]
    public float SHAKE_TIME = 1.0f;
    private float timer = 0f;

    [SerializeField]
    public float SHAKE_MAGNITUDE_MAX = 10.0f;

    float screenSizeX;
    float screenHalfX;
    float screenQuadX;

    public override void Initialize()
    {
        left = entity.GetChild(0);
        right = entity.GetChild(1);
        if (left == null || right == null)
        {
        }
        else
        {
            if (left.name == "Left")
            {
                (left, right) = (right, left);
            }
        }

        screenSizeX = 1920;
        screenHalfX = screenSizeX / 2;
        screenQuadX = screenHalfX / 2;
    }

    public void Reset()
    {
        timer = 0.0f;
        if (left != null)
        {
            left.enable = true;
        }
        if (right != null)
        {
            right.enable = true;
        }
    }

    public override void Update()
    {
        if (left == null || right == null)
        {
            return;
        }

        timer += Time.deltaTime;

        float offsetX = screenQuadX + SHAKE_MAGNITUDE_MAX / 2;
        if (timer < CLOSE_TIME)
        {
            float param = timer / CLOSE_TIME;
            transform.position = Vector3.zero;
            float positionX = Mathf.Lerp(screenHalfX, 0.0f, Ease.Out.Bounce(param)) + offsetX;
            left.transform.position.x = positionX;
            right.transform.position.x = -positionX;
        }
        if (timer < SHAKE_START_TIME)
        {
            float param = (timer - SHAKE_START_TIME) / SHAKE_TIME;
            float shakeMagnitude = Mathf.Lerp(SHAKE_MAGNITUDE_MAX, 0.0f, Mathf.Cos(param * Mathf.PI / 2));
            Vector2 noise = RandomUtil.RandomCircle() * shakeMagnitude;

            Vector3 basePosition = new Vector3(noise.x, noise.y, 0.0f);
        }
        if(timer >= SHAKE_START_TIME + SHAKE_TIME && timer >= CLOSE_TIME)
        {
            left.transform.position.x = offsetX;
            right.transform.position.x = -offsetX;
            transform.position = Vector3.zero;
        }
    }
}


class TransitionIn : MonoScript
{
    Entity left;
    Entity right;

    [SerializeField]
    public float OPEN_TIME = 1.0f;
    [SerializeField]
    public float SHAKE_MAGNITUDE = 20.0f;

    float screenSizeX;
    float screenHalfX;
    float screenQuadX;

    float timer = 0.0f;

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
        timer += Time.deltaTime;

        if (left == null || right == null)
        {
            return;
        }

        if (timer < OPEN_TIME)
        {
            float param = timer / OPEN_TIME;
            float positionX = Mathf.Lerp(SHAKE_MAGNITUDE / 2, screenHalfX, param) + screenQuadX;

            Vector2 noise = RandomUtil.RandomCircumference() * SHAKE_MAGNITUDE;
            Vector3 noiseV3 = new Vector3(noise.x, noise.y, 0.0f);

            transform.position = noiseV3;

            left.transform.position.x = positionX;
            right.transform.position.x = -positionX;
        }
        else
        {
            transform.position = Vector3.zero;
            left.enable = false;
            right.enable = false;
        }
    }
}


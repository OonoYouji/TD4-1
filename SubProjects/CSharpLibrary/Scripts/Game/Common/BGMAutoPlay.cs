class BGMAutoPlay : MonoScript
{
    [SerializeField]
    public string BGM_PATH = "";
    [SerializeField]
    public float DELAY = 0.0f;
    [SerializeField]
    public float PHADE_IN_TIME = 0.0f;
    [SerializeField]
    public float BGM_LOOP_TIME = 0.0f;

    float volume;
    float timer = 0.0f;

    AudioSource audioSource;

    public override void Initialize()
    {
        audioSource = entity.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource?.OneShotPlay(0.7f, 1.0f, BGM_PATH); // 多分ループしない
            volume = audioSource.volume;
            audioSource.volume = 0.0f;
        }
        timer = 0.0f;
    }

    public override void Update()
    {
        if (audioSource == null)
        {
            return;
        }
        timer += Time.deltaTime;
        if (timer < DELAY)
        {
            audioSource.volume = 0.0f;
        }
        else if (timer < DELAY + PHADE_IN_TIME)
        {
            audioSource.volume = volume * (timer - DELAY) / PHADE_IN_TIME;
        }
        else
        {
            audioSource.volume = volume;
        }

        // ループ再生
        if (BGM_LOOP_TIME > 0.0f && timer >= BGM_LOOP_TIME)
        {
            float progress = timer % BGM_LOOP_TIME;
            if (progress < Time.deltaTime)
            {
                audioSource?.OneShotPlay(0.7f, 1.0f, BGM_PATH); // 多分ループしない
            }
        }
    }
}

class BGMAutoPlay : MonoScript
{
    [SerializeField]
    public string BGM_PATH = "";
    [SerializeField]
    public float DELAY = 0.0f;
    [SerializeField]
    public float PHADE_IN_TIME = 0.0f;

    float volume;
    float timer = 0.0f;

    AudioSource audioSource;

    public override void Initialize()
    {
        audioSource = entity.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            volume = 0.7f;
            audioSource.volume = 0.0f;
            audioSource.path = BGM_PATH;
        }
        timer = -DELAY;
    }

    public override void Update()
    {
        if (audioSource == null)
        {
            return;
        }
        timer += Time.deltaTime;
        if (timer < PHADE_IN_TIME)
        {
            float param = timer / PHADE_IN_TIME;
            audioSource?.SetParams(Mathf.Lerp(0.0f, volume, param), 1.0f);
        }
        else
        {
            audioSource?.SetParams(0.7f, 1.0f);
            audioSource = null;
        }

        // ループ再生
        if (timer >= 0.0f && timer <= Time.deltaTime)
        {
            audioSource?.Play();
        }
    }
}

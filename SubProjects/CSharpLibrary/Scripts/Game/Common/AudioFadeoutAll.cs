using System.Collections.Generic;

class AudioFadeoutAll : MonoScript
{
    [SerializeField]
    public float FADE_OUT_TIME = 1.0f;

    struct AudioSourceInfo
    {
        public AudioSource audioSource;
        public float initialVolume;
    }
    readonly List<AudioSourceInfo> audioSources = new List<AudioSourceInfo>();
    bool isFadingOut = false;
    float timer = 0.0f;

    public override void Initialize()
    {
        isFadingOut = false;
        timer = 0.0f;

        audioSources.Clear();
        for (uint i = 0; i < entity.GetChildCount(); i++) // 子にあるAudioSourceを取得
        {
            Entity child = entity.GetChild(i);
            if (child == null)
            {
                continue;
            }
            AudioSource audioSource = child.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSources.Add(new AudioSourceInfo
                {
                    audioSource = audioSource,
                    initialVolume = 0.7f // 初期ボリュームを保存しておく
                });
            }
        }
    }

    public override void Update()
    {
        if (!isFadingOut)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= FADE_OUT_TIME)
        {
            // フェードアウト完了後、すべてのAudioSourceを停止する
            foreach (AudioSourceInfo audioSourceInfo in audioSources)
            {
                //audioSourceInfo.audioSource.Stop(); <- まだない？
                audioSourceInfo.audioSource?.SetParams(0.0f, 1.0f);
            }
            isFadingOut = false;
        }
        else
        {
            // フェードアウト中、すべてのAudioSourceのボリュームを徐々に下げる
            foreach (AudioSourceInfo audioSourceInfo in audioSources)
            {
                audioSourceInfo.audioSource?.SetParams(Mathf.Lerp(audioSourceInfo.initialVolume, 0.0f, timer / FADE_OUT_TIME), 1.0f);
            }
        }
    }

    public void StartFadeOut()
    {
        if (!isFadingOut)
        {
            isFadingOut = true;
            timer = 0.0f;
        }
    }
}

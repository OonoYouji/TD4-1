public class PlayerAudio : MonoScript
{
    private AudioSource audio_ = null;

    private const string CallSoundPath  = "./Assets/Sounds/MainGameSounds/se/player/call.mp3";
    private const string HitSoundPath   = "./Assets/Sounds/MainGameSounds/se/player/hit.mp3";
    private const string LeaveSoundPath = "./Assets/Sounds/MainGameSounds/se/player/leave.mp3";

    public override void Initialize()
    {
        audio_ = entity.GetComponent<AudioSource>();
    }

    public void PlayCall()  => audio_?.OneShotPlay(1.0f, 1.0f, CallSoundPath);
    public void PlayHit()   => audio_?.OneShotPlay(1.0f, 1.0f, HitSoundPath);
    public void PlayLeave() => audio_?.OneShotPlay(1.0f, 1.0f, LeaveSoundPath);
}

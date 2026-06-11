public class ReinforcementGltfAnimator : MonoScript
{
    // =========================================================
    // 通常状態のgltf設定
    // =========================================================

    [SerializeField] public string normalMeshPath = "./Assets/Models/Soldier/soldier.gltf";
    [SerializeField] public string normalTexturePath = "./Assets/Models/Soldier/soldier.png";
    [SerializeField] public string normalClipName = "move";

    // =========================================================
    // はまり状態のgltf設定
    // =========================================================

    [SerializeField] public string supportMeshPath = "./Assets/Models/Soldier/soldier_support.gltf";
    [SerializeField] public string supportTexturePath = "./Assets/Models/Soldier/soldier_support.png";
    [SerializeField] public string supportClipName = "beating";

    // =========================================================
    // 内部状態
    // =========================================================

    private SkinMeshRenderer skinMeshRenderer_;
    private Animator animator_;
    private ReinforcementFallIn fallIn_;
    private bool isSupport_ = false;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        skinMeshRenderer_ = entity.GetComponent<SkinMeshRenderer>();
        animator_ = entity.GetComponent<Animator>();
        fallIn_ = entity.GetScript<ReinforcementFallIn>();
        isSupport_ = false;
        PlayNormal();
    }

    public override void Update()
    {
        if (entity.Id == 0) return;

        bool shouldSupport = (fallIn_ != null && fallIn_.IsActive);
        if (shouldSupport == isSupport_) return;

        isSupport_ = shouldSupport;
        if (isSupport_)
        {
            PlaySupport();
        }
        else
        {
            PlayNormal();
        }
    }

    // =========================================================
    // 再生
    // =========================================================

    private void PlayNormal()
    {
        if (skinMeshRenderer_ == null) return;
        skinMeshRenderer_.meshPath = normalMeshPath;
        skinMeshRenderer_.texturePath = normalTexturePath;
        skinMeshRenderer_.animationTime = 0f;
        skinMeshRenderer_.isPlaying = true;
        if (animator_ != null)
            animator_.Play(normalClipName);
    }

    private void PlaySupport()
    {
        if (skinMeshRenderer_ == null) return;
        skinMeshRenderer_.meshPath = supportMeshPath;
        skinMeshRenderer_.texturePath = supportTexturePath;
        skinMeshRenderer_.animationTime = 0f;
        skinMeshRenderer_.isPlaying = true;
        if (animator_ != null)
            animator_.Play(supportClipName);
    }
}

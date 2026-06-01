using System;
using System.Collections.Generic;

public class SceneTransition : MonoScript {
    private static SceneTransition instance;
    public static SceneTransition Instance {
        get {
            if (instance != null) {
                // 破棄チェック
                if (instance.entity == null || instance.entity.Id == 0) {
                    instance = null;
                    return null;
                }

                // 【重要】現在のアクティブなシーンのグループに所属しているかチェック
                // これを行わないと、古いシーンのインスタンスを操作してしまい遷移が止まる
                if (SceneManager.sceneName_ != instance.ecsGroup.groupName) {
                    // Debug.LogWarning(string.Format("SceneTransition: Current scene '{0}' does not match instance group '{1}'. Clearing stale instance.", SceneManager.sceneName_, instance.ecsGroup.groupName));
                    instance = null;
                }
            }
            return instance;
        }
    }

    [SerializeField] public float fadeDuration = 0.5f;
    [SerializeField] public bool fadeInOnStart = true;

    private SpriteRenderer faderSprite;
    private float timer = 0.0f;
    private bool isFadingOut = false;
    private bool isFadingIn = false;
    private string nextScene = "";

    public override void Initialize() {
        // もし古いインスタンスが残っていたら警告
        if (instance != null && instance != this) {
            Debug.LogWarning("SceneTransition: Overwriting existing instance. This usually happens during scene load.");
        }
        instance = this;
        faderSprite = entity.GetComponent<SpriteRenderer>();
        
        if (faderSprite == null) {
            Debug.LogError("SceneTransition: SpriteRenderer not found on Fader entity!");
        }

        if (fadeDuration <= 0) fadeDuration = 0.5f;

        if (fadeInOnStart) {
            isFadingIn = true;
            isFadingOut = false;
            timer = fadeDuration;
            UpdateAlpha(1.0f);
        } else {
            isFadingIn = false;
            isFadingOut = false;
            UpdateAlpha(0.0f);
        }
    }

    public override void Update() {
        float dt = Time.deltaTime;
        
        if (isFadingOut) {
            timer += dt;
            float alpha = Math.Min(1.0f, timer / fadeDuration);
            UpdateAlpha(alpha);
            
            if (timer >= fadeDuration) {
                isFadingOut = false;
                Debug.Log("SceneTransition: Fade out finished. Loading: " + nextScene);
                SceneManager.LoadScene(nextScene);
            }
        } else if (isFadingIn) {
            timer -= dt;
            float alpha = Math.Max(0.0f, timer / fadeDuration);
            UpdateAlpha(alpha);
            
            if (timer <= 0.0f) {
                isFadingIn = false;
            }
        }
    }

    public override void OnDestroy() {
        if (instance == this) {
            instance = null;
            Debug.Log("SceneTransition instance cleared on destroy.");
        }
    }

    public void TransitionTo(string sceneName) {
        if (isFadingOut) return;

        if (isFadingIn) {
            isFadingIn = false;
        }
        
        nextScene = sceneName;
        isFadingOut = true;
        timer = 0.0f;
        UpdateAlpha(0.0f);
        
        Debug.Log("SceneTransition: Transition started to: " + sceneName);
    }

    private void UpdateAlpha(float alpha) {
        if (faderSprite != null) {
            Vector4 color = faderSprite.color;
            color.w = alpha;
            faderSprite.color = color;
        }
    }
}

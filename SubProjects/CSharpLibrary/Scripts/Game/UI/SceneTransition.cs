using System;
using System.Collections.Generic;

public class SceneTransition : MonoScript {
    private static SceneTransition instance;
    public static SceneTransition Instance => instance;

    [SerializeField] public float fadeDuration = 0.5f;
    private SpriteRenderer sprite;
    private float timer = 0.0f;
    private bool isFadingOut = false;
    private bool isFadingIn = false;
    private string nextScene = "";

    public override void Initialize() {
        instance = this;
        sprite = entity.GetComponent<SpriteRenderer>();
        
        // シーン開始時にフェードイン
        isFadingIn = true;
        timer = fadeDuration;
        UpdateAlpha(1.0f);
    }

    public override void Update() {
        if (isFadingOut) {
            timer += Time.deltaTime;
            float alpha = Math.Min(1.0f, timer / fadeDuration);
            UpdateAlpha(alpha);
            if (timer >= fadeDuration) {
                isFadingOut = false;
                SceneManager.LoadScene(nextScene);
            }
        } else if (isFadingIn) {
            timer -= Time.deltaTime;
            float alpha = Math.Max(0.0f, timer / fadeDuration);
            UpdateAlpha(alpha);
            if (timer <= 0.0f) {
                isFadingIn = false;
            }
        }
    }

    public void TransitionTo(string sceneName) {
        if (isFadingOut || isFadingIn) return;
        nextScene = sceneName;
        isFadingOut = true;
        timer = 0.0f;
        UpdateAlpha(0.0f);
    }

    private void UpdateAlpha(float alpha) {
        if (sprite != null) {
            Vector4 color = sprite.color;
            color.w = alpha;
            sprite.color = color;
        }
    }
}

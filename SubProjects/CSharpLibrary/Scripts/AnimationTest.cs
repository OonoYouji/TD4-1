using System;

public class AnimationTest : Script {
    public override void OnStart() {
        Animator animator = entity.GetComponent<Animator>();
        if (animator != null) {
            // walk.gltf に含まれるクリップ名
            string clipName = "Armature|mixamo.com|Layer0";
            Console.WriteLine("AnimationTest: Starting animation '{0}'", clipName);
            animator.Play(clipName);
        } else {
            Console.WriteLine("AnimationTest: Animator component not found.");
        }
    }

    public override void OnUpdate() {
        Animator animator = entity.GetComponent<Animator>();
        if (animator == null) return;

        // キー入力でアニメーションを切り替え (動作検証用)
        if (Input.TriggerKey(KeyCode.W)) {
            Console.WriteLine("AnimationTest: Play 'Armature|mixamo.com|Layer0'");
            animator.Play("Armature|mixamo.com|Layer0");
        }
        if (Input.TriggerKey(KeyCode.S)) {
            Console.WriteLine("AnimationTest: Stop/Pause logic would go here");
        }
    }
}

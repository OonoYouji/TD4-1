using System;

/// <summary>
/// ボスの範囲攻撃の予測表示を制御するスクリプト。
/// 指定された地点に円を表示する。
/// </summary>
public class TelegraphCircle : MonoScript
{
    public Vector3 centerPosition;
    public float size = 1.0f;
    public float offsetHeight = 0.1f;
    public Vector4 color = new Vector4(1.0f, 0.0f, 0.0f, 0.5f);

    public override void Update()
    {
        // 円は指定された centerPosition の足元に配置
        transform.position = new Vector3(centerPosition.x, offsetHeight, centerPosition.z);
        // Z方向に引き延ばされるのを防ぐため、XZに同じサイズを適用し、Y(高さ)は極めて薄くする
        transform.scale = new Vector3(size, 0.01f, size);

        // レイヤーと色の適用
        var renderer = entity.GetComponent<MeshRenderer>();
        if (renderer == null) {
            // 子要素にある場合を考慮
            for (uint i = 0; i < entity.GetChildCount(); i++) {
                var child = entity.GetChild(i);
                if (child != null) {
                    renderer = child.GetComponent<MeshRenderer>();
                    if (renderer != null) break;
                }
            }
        }

        if (renderer != null)
        {
            renderer.color = color;
            renderer.renderQueue = RenderQueue.Telegraph;
        }

        // デバッグ用描画
        GizmoBatch.DrawLine(new Vector3(centerPosition.x - size * 0.5f, offsetHeight + 0.05f, centerPosition.z), new Vector3(centerPosition.x + size * 0.5f, offsetHeight + 0.05f, centerPosition.z), color);
        GizmoBatch.DrawLine(new Vector3(centerPosition.x, offsetHeight + 0.05f, centerPosition.z - size * 0.5f), new Vector3(centerPosition.x, offsetHeight + 0.05f, centerPosition.z + size * 0.5f), color);
    }
}

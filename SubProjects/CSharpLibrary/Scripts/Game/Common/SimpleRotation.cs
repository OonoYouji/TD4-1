using System;

/// <summary>
/// オブジェクトを一定速度で回転させるシンプルなスクリプト。
/// ヴォルテックスの演出などに使用。
/// </summary>
public class SimpleRotation : MonoScript
{
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 180.0f; // 度/秒

    public override void Update()
    {
        float deltaRot = rotationSpeed * Time.deltaTime;
        transform.rotate *= Quaternion.MakeFromAxis(rotationAxis, deltaRot * Mathf.Deg2Rad);
    }
}

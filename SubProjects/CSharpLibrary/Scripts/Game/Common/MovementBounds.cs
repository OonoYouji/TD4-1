public class MovementBounds : MonoScript
{
  
    // =========================================================
    // パラメーター
    // =========================================================

    // 中心からの半サイズ
    [SerializeField] public float extentX = 10.0f;
    [SerializeField] public float extentZ = 10.0f;

    // =========================================================
    // 計算済み境界
    // =========================================================

    public float MinX => transform.position.x - extentX;
    public float MaxX => transform.position.x + extentX;
    public float MinZ => transform.position.z - extentZ;
    public float MaxZ => transform.position.z + extentZ;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
      
    }
}

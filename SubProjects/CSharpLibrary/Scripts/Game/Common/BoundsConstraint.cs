using System.Runtime.CompilerServices;

public class BoundsConstraint : MonoScript
{
    // =========================================================
    // パラメーター
    // =========================================================

    // trueにするとフィールド内に入るまで制限を適用しない
    [SerializeField] public bool waitForEntry = false;

    // =========================================================
    // 内部状態
    // =========================================================

    private bool hasEntered_ = false;
    private Entity movementBoundsEntity_ = null;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        hasEntered_ = !waitForEntry;
        movementBoundsEntity_ = ecsGroup.FindEntity("MovementBounds");
    }

    public override void Update()
    {

        // 無ければ取得する
        if (movementBoundsEntity_ == null || movementBoundsEntity_.Id == 0)
        {
            movementBoundsEntity_ = ecsGroup.FindEntity("MovementBounds");
            if (movementBoundsEntity_ == null) return;
        }

        // スクリプト取得
        MovementBounds script = movementBoundsEntity_.GetScript<MovementBounds>();
        if (script == null) return;

        // transformの有効性チェック
        if (transform == null) return;

        if (!hasEntered_)
        {
            if (IsInside(script, transform.position))
            {
                hasEntered_ = true;
            }
            return;
        }

        transform.position = Clamp(script, transform.position);
    }

    // =========================================================
    // ユーティリティ
    // =========================================================

    private bool IsInside(MovementBounds bounds, Vector3 pos)
    {
        return pos.x >= bounds.MinX && pos.x <= bounds.MaxX
            && pos.z >= bounds.MinZ && pos.z <= bounds.MaxZ;
    }

    private Vector3 Clamp(MovementBounds bounds, Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, bounds.MinX, bounds.MaxX);
        pos.z = Mathf.Clamp(pos.z, bounds.MinZ, bounds.MaxZ);
        return pos;
    }
}

public partial class Reinforcement
{
    private Entity boundsEntity_ = null;
    private bool hasEnteredBounds_ = false;

    private void InitBoundsCheck()
    {
        boundsEntity_ = ecsGroup.FindEntity("MovementBounds");
        hasEnteredBounds_ = false;
    }

    private void CheckBoundsRetreat()
    {
        if (isRetreating) return;

        if (boundsEntity_ == null || boundsEntity_.Id == 0)
        {
            boundsEntity_ = ecsGroup.FindEntity("MovementBounds");
            if (boundsEntity_ == null) return;
        }

        MovementBounds bounds = boundsEntity_.GetScript<MovementBounds>();
        if (bounds == null) return;

        Vector3 pos = transform.position;
        bool inside = pos.x >= bounds.MinX && pos.x <= bounds.MaxX
                   && pos.z >= bounds.MinZ && pos.z <= bounds.MaxZ;

        if (!hasEnteredBounds_)
        {
            if (inside) hasEnteredBounds_ = true;
            return;
        }

        if (!inside) Retreat();
    }
}

public partial class Reinforcement
{
    private Entity boundsEntity_ = null;

    private void InitBoundsCheck()
    {
        boundsEntity_ = ecsGroup.FindEntity("MovementBounds");
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
        if (pos.x < bounds.MinX || pos.x > bounds.MaxX ||
            pos.z < bounds.MinZ || pos.z > bounds.MaxZ)
        {
            Retreat();
        }
    }
}

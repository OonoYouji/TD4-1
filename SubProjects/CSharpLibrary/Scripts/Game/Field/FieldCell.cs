public class FieldCell : MonoScript
{
    [SerializeField] public FieldCellType cellType = FieldCellType.TypeA;
    [SerializeField] public int gridRow = 0;
    [SerializeField] public int gridCol = 0;

    // BossMovementBlockManager によって実行時に設定される
    public bool isBossBlock    = false;
    public int  bossBlockIndex = -1;
}

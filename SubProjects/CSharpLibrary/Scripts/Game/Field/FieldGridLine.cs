public class FieldGridLine : MonoScript
{
    public enum LineType
    {
        Row = 0, // Z方向に伸びる（列ラインごとに1本）
        Col = 1, // X方向に伸びる（行ラインごとに1本）
    }

    // =========================================================
    // パラメーター
    // =========================================================

    [SerializeField] public int  lineType  = 0;     // 0=Row, 1=Col
    [SerializeField] public int  lineIndex = 0;     // 何列目 / 何行目か
    [SerializeField] public float yPosition  = 0.1f;
    [SerializeField] public float thickness  = 0.2f; // 細い方の辺のスケール
    [SerializeField] public float yScale     = 0.05f;

    // ランタイムからスポーナーが上書きするインデックス（-1 = 未設定）
    private int lineIndexOverride_ = -1;
    public int runtimeLineIndex { set { lineIndexOverride_ = value; } }

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        Entity fmEntity = ecsGroup.FindEntity("FieldManager");
        if (fmEntity == null) return;

        FieldManager fm = fmEntity.GetScript<FieldManager>();
        if (fm == null) return;

        Vector3 origin   = fmEntity.transform.position;
        float   cellSize = fm.CellSize;
        int     rows     = fm.Rows;
        int     cols     = fm.Columns;

        // グリッド全体のXZ中心
        float xCenter = origin.x - (cols - 1) * cellSize * 0.5f;
        float zCenter = origin.z - (rows - 1) * cellSize * 0.5f;

        int idx = lineIndexOverride_ >= 0 ? lineIndexOverride_ : lineIndex;

        if (lineType == (int)LineType.Row)
        {
            // Z方向に全行分を伸ばす。idx は列インデックス
            float posX = origin.x + idx * cellSize - (cols - 1) * cellSize;
            transform.position = new Vector3(posX, yPosition, zCenter);
            transform.scale    = new Vector3(thickness, yScale, rows * cellSize);
        }
        else
        {
            // X方向に全列分を伸ばす。idx は行インデックス
            float posZ = origin.z + idx * cellSize - (rows - 1) * cellSize;
            transform.position = new Vector3(xCenter, yPosition, posZ);
            transform.scale    = new Vector3(cols * cellSize, yScale, thickness);
        }
    }
}

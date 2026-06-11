public class FieldGridLineSpawner : MonoScript
{
    public override void Initialize()
    {
        Entity fmEntity = ecsGroup.FindEntity("FieldManager");
        if (fmEntity == null) return;

        FieldManager fm = fmEntity.GetScript<FieldManager>();
        if (fm == null) return;

        // Row (Z方向): 列数分スポーン
        for (int c = 0; c < fm.Columns; c++)
        {
            Entity e = ecsGroup.CreateEntity("FieldGridLine_Row");
            if (e == null) continue;
            FieldGridLine line = e.GetScript<FieldGridLine>();
            if (line != null) line.runtimeLineIndex = c;
        }

        // Col (X方向): 行数分スポーン
        for (int r = 0; r < fm.Rows; r++)
        {
            Entity e = ecsGroup.CreateEntity("FieldGridLine_Col");
            if (e == null) continue;
            FieldGridLine line = e.GetScript<FieldGridLine>();
            if (line != null) line.runtimeLineIndex = r;
        }
    }
}

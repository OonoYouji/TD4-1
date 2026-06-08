using System.Collections.Generic;

/// <summary>
/// Fieldのグリッド上の3x3セルを1つのボス移動ブロックにするクラス
/// </summary>
public class BossMovementBlock
{
    public string blockId;
    public int centerRow;
    public int centerCol;
    public bool isOccupied;

    private FieldManager fieldManager_;

    public BossMovementBlock(string id, int row, int col, FieldManager fm)
    {

        blockId      = id;
        centerRow    = row;
        centerCol    = col;
        fieldManager_ = fm;
    }

    // 中心から3x3の範囲にセルがあるか判定
    public bool IsValid =>
        fieldManager_ != null &&
        centerRow >= 1 && centerRow < fieldManager_.Rows - 1 &&
        centerCol >= 1 && centerCol < fieldManager_.Columns - 1;

  
    /// ボスの3x3の範囲内のセルを取得
    public List<(int row, int col)> GetCoveredCells()
    {
        // 最大9マス(3x3)入るリストを用意
        var result = new List<(int, int)>(9);

        // 縦の中心からの位置のずれ[-1上, 0中心, +1下]
        for (int dr = -1; dr <= 1; dr++)
        {
            // 横の中心からの位置のずれ[-1左, 0中心, +1右]
            for (int dc = -1; dc <= 1; dc++)
            {
                // 中心座標にずれを足して実際のグリッド座標を求める
                int row = centerRow + dr;
                int col = centerCol + dc;

                // 特定のrow,colを対象として追加
                if (row >= 0 && row < fieldManager_.Rows && col >= 0 && col < fieldManager_.Columns)
                    result.Add((row, col));
            }
        }
        return result;
    }


    /// <summary>ブロック中心のワールド座標</summary>
   public Vector3 GetCenterWorldPos() => fieldManager_.GridToWorld(centerRow, centerCol);


}

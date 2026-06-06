using System;
using System.Collections.Generic;

/// <summary>
/// ボス移動ブロックの配置をエディタ上で管理するマネージャー。
/// </summary>
public class BossMovementBlockManager : MonoScript
{
    public static BossMovementBlockManager Instance { get; private set; }

    // =========================================================
    // エディタで設定するパラメーター
    // =========================================================

    /// ボス移動ブロックの配置リスト。Size でブロック数を変更できる
    [SerializeField] public List<BossBlockConfig> blockConfigs = new List<BossBlockConfig>();

    // =========================================================
    // 内部状態
    // =========================================================

    private readonly List<BossMovementBlock> blocks_ = new List<BossMovementBlock>();

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {

        // シングルトンインスタンスの設定
        Instance = this;

        // FieldManagerエンティティの取得
        Entity FieldManagerEntity = ecsGroup.FindEntity("FieldManager");
        if (FieldManagerEntity == null)
        {
            Debug.LogWarning("[BossMovementBlockManager] FieldManager が見つかりません。");
            return;
        }

        // FieldManagerスクリプトを取得
        FieldManager fieldManagerScript = FieldManagerEntity.GetScript<FieldManager>();
        if (fieldManagerScript == null) {
            Debug.LogWarning("[BossMovementBlockManager] FieldManagerスクリプトが見つかりません。");
            return; 
        }

        // ブロック配置の初期化
        for (int i = 0; i < blockConfigs.Count; i++)
        {
            RegisterBlock(blockConfigs[i], i, fieldManagerScript);
        }

        Debug.Log($"[BossMovementBlockManager] {blocks_.Count} 個のボス移動ブロックを登録しました。");
    }

    // =========================================================
    // 内部ヘルパー
    // =========================================================

    private void RegisterBlock(BossBlockConfig cfg, int index, FieldManager fieldManagerScript)
    {
        // ブロックIDは "Block1", "Block2", ... の形式で自動生成
        var block = new BossMovementBlock($"Block_{index}", cfg.centerRow, cfg.centerCol, fieldManagerScript);

        // ブロックの有効性をチェック
        if (!block.IsValid)
        {
            Debug.LogWarning($"[BossMovementBlockManager] Block_{index} (row={cfg.centerRow}, col={cfg.centerCol}) が無効です。端から1マス以上内側を指定してください。");
            return;
        }

        // ブロックを登録
        blocks_.Add(block);

        // 配置する位置のFieldCellを取得
        Entity centerEntity = fieldManagerScript.GetCellAt(block.centerRow, block.centerCol);
        if (centerEntity != null)
        {
            // 3x3なので普通のFieldCellの3倍の大きさで配置
            float size = fieldManagerScript.CellSize * 1.5f;
            centerEntity.transform.scale = new Vector3(size, centerEntity.transform.scale.y, size);

            // FieldCellのスクリプトを取得
            FieldCell centerCell = centerEntity.GetScript<FieldCell>();

            // ボスブロックのFieldCellのindexやフラグ設定
            if (centerCell != null)
            {
                centerCell.isBossBlock    = true;
                centerCell.bossBlockIndex = index;
            }
        }

        // ボスブロックの回りのブロックを消す
        foreach (var (r, c) in block.GetCoveredCells())
        {

            // 中心セルはスキップ
            if (r == block.centerRow && c == block.centerCol)
            {
                continue;
            }

            // 配置する位置のFieldCellを削除
            fieldManagerScript.GetCellAt(r, c)?.Destroy();
        }
    }

    // =========================================================
    // クエリ API
    // =========================================================

    public int BlockCount => blocks_.Count;
    
    
    private List<BossMovementBlock> GetAvailableBlocks()
    {
        var result = new List<BossMovementBlock>(blocks_.Count);
        foreach (var block in blocks_)
        {
            // 占有されていないブロックだけ追加
            if (!block.isOccupied)
            {
                result.Add(block);
            }
        }
        return result;
    }

}

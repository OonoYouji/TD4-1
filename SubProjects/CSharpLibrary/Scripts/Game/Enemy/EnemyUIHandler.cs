class EnemyUIHandler : MonoScript
{
    [SerializeField]
    public string HP_UI_NAME = "HP";
    DissolveMeshRenderer renderer;

    public override void Initialize()
    {
        Debug.LogInfo("EnemyUIHandler Initializing");
        // 再帰関数で子オブジェクトを探索して、HP_UI_NAMEと一致するオブジェクトを見つける
        Entity hpEntity = FindEntity(entity, HP_UI_NAME);
        if (hpEntity == null)
        {
            Debug.LogError($"Failed to find entity with name {HP_UI_NAME}");
            return;
        }

        renderer = hpEntity.GetComponent<DissolveMeshRenderer>();
        if (renderer == null)
        {
            Debug.LogError("Failed to find DissolveMeshRenderer component on " + HP_UI_NAME);
            return;
        }
        Debug.LogInfo("EnemyUIHandler initialized");
    }

    public void OnDamaged(float currentHpPercent)
    {
        if (renderer == null) return;
        Debug.LogInfo($"EnemyUIHandler OnDamaged called with currentHpPercent: {currentHpPercent}");
        renderer.threshold = Mathf.Clamp01(currentHpPercent);
    }

    static Entity FindEntity(Entity parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }
        for (uint i = 0; i < parent.GetChildCount(); i++)
        {
            Entity child = parent.GetChild(i);
            Entity result = FindEntity(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}
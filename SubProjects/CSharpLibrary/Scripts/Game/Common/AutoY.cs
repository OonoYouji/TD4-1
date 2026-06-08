class AutoY : MonoScript
{
    [SerializeField]
    readonly float HEIGHT = 0.0f;

    [SerializeField]
    readonly string SELECTION_NAME = "Selection";
    SceneSelector selector;

    Vector3 basePosition;

    public override void Initialize()
    {
        Entity selectorEntity = ecsGroup.FindEntity(SELECTION_NAME);
        if (selectorEntity != null)
        {
            selector = selectorEntity.GetScript<SceneSelector>();
            basePosition = transform.position;
        }
    }

    public override void Update()
    {
        if (selector == null) {
            return;
        }

        int index = selector.GetNextSceneIndex();

        entity.transform.position = basePosition + new Vector3(0, -HEIGHT * index, 0);
    }
}

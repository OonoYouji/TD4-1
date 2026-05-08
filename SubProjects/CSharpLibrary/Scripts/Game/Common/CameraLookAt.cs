class CameraLookAt : MonoScript
{
    [SerializeField]
    public string cameraName = "Camera";
    Transform camera;

    Entity parent;

    public override void Initialize()
    {
        Debug.LogInfo("CameraLookAt Initializing");
        Entity cameraEntity = ecsGroup.FindEntity(cameraName);
        if (cameraEntity != null)
        {
            camera = cameraEntity.transform;
        }else
        {
            Debug.LogError($"CameraLookAt failed to find camera entity with name {cameraName}");
        }
        parent = entity.parent;
        Debug.LogInfo("CameraLookAt Initialized");
    }

    public override void Update()
    {
        if (camera != null && parent != null)
        {
            Quaternion.MakeFromAxis(Vector3.forward, Mathf.PI);
            Quaternion baseRotate = camera.rotate;
            Quaternion parentRotateInv = Quaternion.CreateFromRotationMatrix(parent.transform.matrix).Conjugate();
            transform.rotate =  parentRotateInv * baseRotate * Quaternion.MakeFromAxis(Vector3.up, Mathf.PI);
        }
    }
}
class CameraLookAt : MonoScript
{
    [SerializeField]
    public string cameraName = "Camera";
    Transform camera;

    Entity parent;

    public override void Initialize() {
        Entity cameraEntity = ecsGroup.FindEntity(cameraName);
        if (cameraEntity != null) {
            camera = cameraEntity.transform;
        }
        parent = entity.parent;
    }

    public override void Update()
    {
        if (camera != null && parent != null) {
            Quaternion baseRotate = Quaternion.MakeFromAxis(Vector3.up, Mathf.PI) * camera.rotate;
            Quaternion parentRotateInv = parent.transform.rotate.Inverse();
            transform.rotate = baseRotate * parentRotateInv;
        }
    }
}
public class TestEnemy : MonoScript
{
    [SerializeField]
    public string ENTITY_NAME = "Player";

    Entity targetEntity;

    [SerializeField]
    public float searchRange = 20.0f;
    [SerializeField]
    public float rotateSpeed = 3.0f;

    [SerializeField]
    public float attackRange = 10.0f;
    [SerializeField]
    public float speed = 5.0f;

    MeshRenderer meshRenderer;

    PlayerBulletLauncher launcher;

    public override void Initialize()
    {
        Debug.Log("Start Enemy");
        targetEntity = ecsGroup.FindEntity(ENTITY_NAME);
        if (targetEntity == null)
        {
            Debug.LogWarning("Failed to find target entity");
        }
        meshRenderer = entity.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError("Failed to find MeshRenderer component");
        }
        launcher = entity.GetScript<PlayerBulletLauncher>();
        if (launcher == null)
        {
            Debug.LogError("Failed to find PlayerBulletLauncher script");
        }
        launcher.enable = false;

        Debug.Log("Initialized Enemy");
    }

    public override void Update()
    {
        Debug.Log("TestEnemy Update");

        if (targetEntity == null)
        {
            return;
        }

        launcher.enable = false;

        float distance = Vector3.Distance(transform.position, targetEntity.transform.position);
        if (distance < searchRange)
        {

            Quaternion lookAt = Quaternion.LookAt(transform.position, targetEntity.transform.position, Vector3.up);
            Quaternion current = transform.rotate;
            transform.rotate = Quaternion.Slerp(current, lookAt, rotateSpeed * Time.deltaTime);
        }

        if (distance <= searchRange && distance > attackRange)
        {
            meshRenderer.color = new Vector4(0.941f, 0.901f, 0.549f, 1.0f); // Khaki

            Vector3 forward = Matrix4x4.Transform(Vector3.forward, Matrix4x4.Rotate(transform.rotate));
            transform.position += forward * Time.deltaTime * speed;
        }
        else if (distance <= attackRange)
        {
            meshRenderer.color = Vector4.red;
            launcher.enable = true;

            float diff = distance - attackRange;
            Vector3 forward = Matrix4x4.Transform(Vector3.forward, Matrix4x4.Rotate(transform.rotate));
            transform.position += forward * Time.deltaTime * speed * diff;

            launcher.launchDirection = forward.Normalized();
        }
        else
        {
            meshRenderer.color = Vector4.green;
        }
    }
}

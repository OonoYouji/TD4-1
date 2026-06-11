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
    public float attackRange = 5.0f;
    [SerializeField]
    public float speed = 5.0f;

    [SerializeField]
    public float fireInterval = 1.0f;
    private float fireTimer = 0.0f;

    MeshRenderer meshRenderer;
    EnemyBulletLauncher launcher;

    public override void Initialize()
    {
        targetEntity = ecsGroup.FindEntity(ENTITY_NAME);
        if (targetEntity == null)
        {
        }
        meshRenderer = entity.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
        }
        launcher = entity.GetScript<EnemyBulletLauncher>();
        if (launcher == null)
        {
        }

    }

    public override void Update()
    {

        if (targetEntity == null)
        {
            return;
        }

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
            fireTimer = 0.0f;
        }
        else if (distance <= attackRange)
        {
            meshRenderer.color = Vector4.red;

            fireTimer += Time.deltaTime;
            if (fireTimer >= fireInterval)
            {
                fireTimer = 0.0f;
                if (launcher != null)
                {
                    launcher.Fire();
                }
                else
                {
                }
            }
        }
        else
        {
            meshRenderer.color = Vector4.green;
            fireTimer = 0.0f;
        }
    }
}


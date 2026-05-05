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

    MeshRenderer meshRenderer;

    public override void Initialize()
    {
        Debug.Log("Start Enemy");
        targetEntity = ecsGroup.FindEntity(ENTITY_NAME);
        if (targetEntity == null)
        {
            Debug.LogWarning("Failed to find target entity");
        }
        meshRenderer = entity.GetComponent<MeshRenderer>();

        Debug.Log("Initialized Enemy");
    }

    public override void Update()
    {
        Debug.Log("TestEnemy Update");

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
        }
        else if (distance <= attackRange)
        {
            meshRenderer.color = Vector4.red;
        }
        else
        {
            meshRenderer.color = Vector4.green;
        }
    }
}

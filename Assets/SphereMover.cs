using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SphereMover : MonoBehaviour
{
    private Rigidbody rb;

    private float speed;
    private float changeTime;
    private float timer;

    private Vector3 targetPos;
    private Vector3 center;
    private Vector3 bounds;

    private SphereTrackSystem spawner;
    private bool initialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>(); // ALWAYS get this early
    }

    public void Init(SphereTrackSystem s, float moveSpeed, float changeDirTime, Vector3 c, Vector3 b)
    {
        spawner = s;
        speed = moveSpeed;
        changeTime = changeDirTime;

        center = c;
        bounds = b;

        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        PickNewTarget();

        initialized = true; // mark ready
    }

    void FixedUpdate()
    {
        if (!initialized) return; // 🔥 prevents crash

        timer += Time.fixedDeltaTime;

        Vector3 dir = (targetPos - rb.position).normalized;
        rb.velocity = dir * speed;

        if (timer >= changeTime || Vector3.Distance(rb.position, targetPos) < 0.2f)
        {
            PickNewTarget();
            timer = 0f;
        }
    }

    void PickNewTarget()
    {
        targetPos = new Vector3(
            center.x + Random.Range(-bounds.x / 2, bounds.x / 2),
            center.y + Random.Range(-bounds.y / 2, bounds.y / 2),
            center.z
        );
    }

    void OnMouseDown()
    {
        if (spawner != null)
        {
            spawner.RegisterHit(gameObject);
        }

        Destroy(gameObject);
    }
}
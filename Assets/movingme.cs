using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Rigidbody3DMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;         // Movement speed
    public float rotationSpeed = 720f;   // Degrees per second for turning

    private Rigidbody rb;
    private Vector3 inputVector;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Prevent physics rotation
    }

    void Update()
    {
        // Get horizontal/vertical input (WASD or Arrow Keys)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        inputVector = new Vector3(h, 0f, v).normalized;

        // Rotate player toward movement direction
        if (inputVector.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputVector);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // Move horizontally only
        Vector3 velocity = inputVector * moveSpeed;
        velocity.y = rb.velocity.y; // Preserve vertical velocity (gravity)
        rb.velocity = velocity;
    }
}
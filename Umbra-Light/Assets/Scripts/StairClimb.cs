using UnityEngine;

public class StairClimb : MonoBehaviour
{
    [Header("Step Settings")]
    public float stepHeight = 0.4f;
    // Max height of one step player can climb
    // Increase if stairs are taller

    public float stepSmooth = 0.1f;
    // How fast player is pushed up the step

    public float rayDistance = 0.5f;
    // How far forward to detect steps

    [Header("Layers")]
    public LayerMask environmentLayer;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        ClimbSteps();
    }

    void ClimbSteps()
    {
        // Get movement direction from velocity
        Vector3 moveDir = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z);

        // Only check if actually moving
        if (moveDir.magnitude < 0.1f) return;

        moveDir.Normalize();

        // RAY 1 — shoot at ground level forward
        // Checks if there is a step face in front
        Vector3 rayLow = transform.position +
            Vector3.up * 0.05f;

        bool hitLow = Physics.Raycast(
            rayLow,
            moveDir,
            rayDistance,
            environmentLayer);

        // RAY 2 — shoot above step height forward
        // Checks if there is clear space above the step
        Vector3 rayHigh = transform.position +
            Vector3.up * (stepHeight + 0.05f);

        bool hitHigh = Physics.Raycast(
            rayHigh,
            moveDir,
            rayDistance,
            environmentLayer);

        // Step detected — low hit but high is clear
        // Push player up smoothly
        if (hitLow && !hitHigh)
        {
            rb.position += Vector3.up * stepSmooth;
        }
    }
}


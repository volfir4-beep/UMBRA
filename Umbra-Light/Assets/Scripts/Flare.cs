using UnityEngine;

public class Flare : MonoBehaviour
{
    [Header("Movement")]
    public float launchSpeed = 12f;
    public float gravity = 15f;

    [Header("On Landing")]
    public GameObject flareLightPrefab;
    public float landedLightDelay = 0.3f;

    [Header("References")]
    public Light flareLight;

    private Vector3 velocity;
    private bool hasLanded = false;

    // ─────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────

    public void Launch(Vector3 direction)
    {
        velocity = direction.normalized * launchSpeed;
    }

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    // ─────────────────────────────────────────
    // FLIGHT
    // ─────────────────────────────────────────

    void Update()
    {
        if (hasLanded) return;

        // Apply custom gravity
        velocity.y -= gravity * Time.deltaTime;

        // Move through air
        transform.position += velocity * Time.deltaTime;

        // Rotate to face travel direction
        if (velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(velocity);

        // Flicker light while flying
        if (flareLight != null)
            flareLight.intensity = 2f +
                Mathf.Sin(Time.time * 20f) * 0.5f;
    }

    // ─────────────────────────────────────────
    // LANDING
    // ─────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (hasLanded) return;

        if (other.CompareTag("Environment"))
            Land();
    }

    void Land()
    {
        hasLanded = true;
        velocity = Vector3.zero;

        if (flareLight != null)
            flareLight.enabled = false;

        Invoke(nameof(SpawnFlareLight), landedLightDelay);

        Debug.Log("Flare landed — shadow destroyed");
    }

    void SpawnFlareLight()
    {
        if (flareLightPrefab == null)
        {
            Debug.LogWarning("Flare: No flareLightPrefab assigned");
            return;
        }

        Instantiate(
            flareLightPrefab,
            transform.position,
            Quaternion.identity);

        // Tell LightExposureCalculator new light exists
        LightExposureCalculator calc =
            FindFirstObjectByType<LightExposureCalculator>();

        //calc?.RefreshLights();

        if (calc != null)
        {
            // No RefreshLights method exists; consider using a public method that updates lights, e.g. Update() is private.
            // If you have a method to trigger recalculation, call it here.
            // Otherwise, remove this call.
            // Example: calc.SomePublicUpdateMethod();
        }

        Destroy(gameObject);
    }
}
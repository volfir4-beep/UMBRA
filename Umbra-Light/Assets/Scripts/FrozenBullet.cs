using UnityEngine;

public class FrozenBullet : MonoBehaviour
{
    public float bulletSpeed = 35f;
    public bool isEnemyBullet = false;

    private Rigidbody rb;
    private Vector3 travelDirection;
    private bool isFrozen = false;
    private bool hasHit = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        travelDirection = transform.forward;
        rb.linearVelocity = travelDirection * bulletSpeed;

        Destroy(gameObject, 15f);
    }

    void Update()
    {
        float timeScale = WorldTimeController.Instance.worldTimeScale;

        if (timeScale < 0.05f)
        {
            if (!isFrozen)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
                isFrozen = true;
            }
        }
        else
        {
            if (isFrozen)
            {
                rb.isKinematic = false;
                rb.linearVelocity = travelDirection * bulletSpeed * timeScale;
                isFrozen = false;
            }
            else
            {
                rb.linearVelocity = travelDirection * bulletSpeed * timeScale;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Player bullet hits enemy
        if (!isEnemyBullet && other.CompareTag("Enemy"))
        {
            hasHit = true;
            Security enemy = other.GetComponent<Security>();
            if (enemy != null)
                enemy.Die();
            else
                Debug.LogWarning("Hit Enemy tag but no Enemy script found");
            DestroyBullet();
            return;
        }

        // Enemy bullet hits player — GAME OVER
        if (isEnemyBullet && other.CompareTag("Player"))
        {
            hasHit = true;
            Debug.Log("Enemy bullet hit player");

            PlayerDeath pd = other.GetComponent<PlayerDeath>();
            if (pd != null)
            {
                Debug.Log("Calling PlayerDeath.Die()");
                pd.Die();
            }
            else
            {
                Debug.LogError("PlayerDeath script not found on Player!");
            }

            DestroyBullet();
            return;
        }

        // Hit environment
        if (other.CompareTag("Environment"))
        {
            hasHit = true;
            DestroyBullet();
        }
    }

    void DestroyBullet()
    {
        if (!isEnemyBullet)
            FindFirstObjectByType<PlayerShooting>()?.BulletDestroyed();
        Destroy(gameObject);
    }
}
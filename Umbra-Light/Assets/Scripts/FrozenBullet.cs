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

        if (!isEnemyBullet && other.CompareTag("Enemy"))
        {
            hasHit = true;
            other.GetComponent<Enemy>()?.Die();
            DestroyBullet();
            return;
        }

        if (isEnemyBullet && other.CompareTag("Player"))
        {
            hasHit = true;
            FindFirstObjectByType<PlayerDeath>()?.Die();
            DestroyBullet();
            return;
        }

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
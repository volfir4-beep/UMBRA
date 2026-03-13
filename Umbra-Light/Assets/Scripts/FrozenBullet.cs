using NUnit.Framework;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using static UnityEngine.UIElements.UxmlAttributeDescription;

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
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        travelDirection = transform.forward;
        rb.linearVelocity = travelDirection * bulletSpeed;

        // Shorter lifetime — bullets disappear in 4 seconds
        Destroy(gameObject, 4f);
    }

    void Update()
    {
        if (hasHit) return;

        float timeScale = WorldTimeController.Instance != null
            ? WorldTimeController.Instance.worldTimeScale
            : 1f;

        if (timeScale < 0.05f)
        {
            // Freeze bullet
            if (!isFrozen)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
                isFrozen = true;
            }
        }
        else
        {
            // Unfreeze bullet
            if (isFrozen)
            {
                rb.isKinematic = false;
                rb.linearVelocity =
                    travelDirection * bulletSpeed * timeScale;
                isFrozen = false;
            }
            else
            {
                rb.linearVelocity =
                    travelDirection * bulletSpeed * timeScale;
            }
        }
    }

    // ─────────────────────────────────────────
    // TRIGGER — Catches Player and Enemy hits
    // ─────────────────────────────────────────

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
            DestroyBullet();
            return;
        }

        // Enemy bullet hits player
        if (isEnemyBullet && other.CompareTag("Player"))
        {
            hasHit = true;
            Debug.Log("Enemy bullet hit player");

            PlayerDeath pd = other.GetComponent<PlayerDeath>();
            if (pd != null)
                pd.Die();
            else
                Debug.LogError("No PlayerDeath on Player object");

            DestroyBullet();
            return;
        }

        // Either bullet hits environment via trigger
        if (other.CompareTag("Environment"))
        {
            hasHit = true;
            DestroyBullet();
        }
    }

    // ─────────────────────────────────────────
    // COLLISION — Catches Wall hits reliably
    // This fires when bullet physically hits
    // a solid non-trigger collider like a wall
    // ─────────────────────────────────────────

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        // Hit a wall or floor
        if (collision.gameObject.CompareTag("Environment"))
        {
            hasHit = true;
            DestroyBullet();
            return;
        }

        // Safety net — if bullet hits anything solid
        // that isn't a player or enemy, destroy it
        if (!collision.gameObject.CompareTag("Player") &&
            !collision.gameObject.CompareTag("Enemy"))
        {
            hasHit = true;
            DestroyBullet();
        }
    }

    // ─────────────────────────────────────────
    // DESTROY
    // ─────────────────────────────────────────

    void DestroyBullet()
    {
        // Tell PlayerShooting the bullet is gone
        if (!isEnemyBullet)
        {
            PlayerShooting ps =
                FindFirstObjectByType<PlayerShooting>();
            ps?.BulletDestroyed();
        }

        Destroy(gameObject);
    }
}

using NUnit.Framework;
using System.Collections;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class FlareGunner : MonoBehaviour
{
    // ─────────────────────────────────────────
    // SETTINGS
    // ─────────────────────────────────────────

    [Header("Movement")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 5f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    private int patrolIndex = 0;
    private float patrolWaitTimer = 0f;
    public float patrolWaitTime = 1.5f;

    [Header("Detection Radius")]
    public float detectionRadius = 15f;
    // Player inside this radius = FlareGunner reacts
    // Outside = ignores player completely

    [Header("Flare Settings")]
    public GameObject flarePrefab;
    public Transform shootPoint;
    public int maxFlares = 3;
    private int currentFlares;
    public float flareAimTime = 2f;
    public float flareCooldown = 8f;
    private float lastFlareFiredTime = -999f;
    private bool isFiringFlare = false;

    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public float bulletShootCooldown = 2f;
    private float lastBulletFiredTime = -999f;

    [Header("Drop")]
    public GameObject gunPickupPrefab;

    // Internal
    private NavMeshAgent agent;
    private Transform player;
    private LightExposureCalculator lightCalc;
    private bool isDead = false;

    public enum State
    {
        Patrolling,
        AimingFlare,
        ShootingBullet
    }
    public State currentState = State.Patrolling;

    // ─────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentFlares = maxFlares;

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("FlareGunner: Cannot find Player tag");

        lightCalc =
            FindFirstObjectByType<LightExposureCalculator>();

        if (lightCalc == null)
            Debug.LogError("FlareGunner: Cannot find " +
                "LightExposureCalculator");

        WorldTimeController.Instance?.RegisterFlareGunner(this);
        GoToNextPatrolPoint();
    }

    void OnDestroy()
    {
        WorldTimeController.Instance?.UnregisterFlareGunner(this);
    }

    // ─────────────────────────────────────────
    // TIME SCALE
    // ─────────────────────────────────────────

    public void SetTimeScale(float scale)
    {
        agent.speed = patrolSpeed * scale;
        agent.angularSpeed = 180f * scale;
        agent.acceleration = 10f * scale;

        if (scale < 0.02f)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    // ─────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────

    void Update()
    {
        if (isDead) return;
        if (player == null) return;
        if (lightCalc == null) return;

        float timeScale = WorldTimeController.Instance != null
            ? WorldTimeController.Instance.worldTimeScale : 1f;

        if (timeScale < 0.02f) return;

        // Currently firing flare — wait for coroutine to finish
        if (isFiringFlare) return;

        float distToPlayer = Vector3.Distance(
            transform.position, player.position);

        bool playerInRadius = distToPlayer <= detectionRadius;
        bool playerInShadow = lightCalc.lightExposure < 0.05f;
        bool playerInLight = lightCalc.lightExposure >= 0.05f;
        bool flaresAvailable = currentFlares > 0;
        bool flareReady =
            Time.time > lastFlareFiredTime + flareCooldown;

        // ── DECISION ──────────────────────────

        if (playerInRadius && playerInShadow
            && flaresAvailable && flareReady)
        {
            // Player hiding in shadow inside radius
            // Stop and fire flare at their position
            currentState = State.AimingFlare;
            StartCoroutine(AimAndFireFlare());
        }
        else if (playerInRadius && playerInLight)
        {
            // Player visible in light inside radius
            // Shoot bullet at them
            currentState = State.ShootingBullet;
            agent.speed = chaseSpeed * timeScale;
            DoShootBullet();
        }
        else
        {
            // Player outside radius or flare on cooldown
            // Just patrol normally
            currentState = State.Patrolling;
            agent.speed = patrolSpeed * timeScale;
            DoPatrol();
        }
    }

    // ─────────────────────────────────────────
    // PATROL
    // ─────────────────────────────────────────

    void DoPatrol()
    {
        if (patrolPoints.Length == 0) return;
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0f;
                GoToNextPatrolPoint();
            }
        }
    }

    // ─────────────────────────────────────────
    // FLARE — aim and fire at player shadow pos
    // ─────────────────────────────────────────

    IEnumerator AimAndFireFlare()
    {
        isFiringFlare = true;

        // Stop moving
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        // Lock in player position right now
        // This is where flare will land
        Vector3 flareTarget = player.position;

        Debug.Log("FlareGunner: Aiming flare...");

        float aimTimer = 0f;

        // Rotate to face target while aiming
        while (aimTimer < flareAimTime)
        {
            Vector3 dir = new Vector3(
                flareTarget.x - transform.position.x,
                0f,
                flareTarget.z - transform.position.z);

            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    Time.deltaTime * 5f);
            }

            aimTimer += Time.deltaTime;
            yield return null;
        }

        // Fire the flare
        FireFlare(flareTarget);

        currentFlares--;
        lastFlareFiredTime = Time.time;
        isFiringFlare = false;

        Debug.Log("FlareGunner: Flare fired. Remaining: "
            + currentFlares);

        // Resume patrol
        GoToNextPatrolPoint();
    }

    void FireFlare(Vector3 targetPosition)
    {
        if (flarePrefab == null)
        {
            Debug.LogWarning("FlareGunner: No flare prefab assigned");
            return;
        }

        if (shootPoint == null)
        {
            Debug.LogWarning("FlareGunner: No shoot point assigned");
            return;
        }

        Vector3 direction = CalculateArcDirection(
            shootPoint.position, targetPosition);

        GameObject flareObj = Instantiate(
            flarePrefab,
            shootPoint.position,
            Quaternion.LookRotation(direction));

        Flare flare = flareObj.GetComponent<Flare>();

        if (flare != null)
            flare.Launch(direction);
        else
            Debug.LogWarning("FlareGunner: Flare prefab " +
                "missing Flare.cs script");
    }

    Vector3 CalculateArcDirection(Vector3 from, Vector3 to)
    {
        Vector3 horizontal = to - from;
        horizontal.y = 0f;
        float horizontalDist = horizontal.magnitude;

        float arcHeight = Mathf.Clamp(
            horizontalDist * 0.4f, 2f, 8f);

        Vector3 direction = horizontal.normalized;
        direction.y = arcHeight / Mathf.Max(horizontalDist, 0.1f);

        return direction.normalized;
    }

    // ─────────────────────────────────────────
    // BULLET — when player is in light
    // ─────────────────────────────────────────

    void DoShootBullet()
    {
        // Stop moving
        agent.SetDestination(transform.position);

        // Face player
        Vector3 lookTarget = new Vector3(
            player.position.x,
            transform.position.y,
            player.position.z);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(
                lookTarget - transform.position),
            Time.deltaTime * 8f);

        // Shoot on cooldown
        if (Time.time > lastBulletFiredTime + bulletShootCooldown)
        {
            FireBullet();
            lastBulletFiredTime = Time.time;
        }
    }

    void FireBullet()
    {
        if (bulletPrefab == null || shootPoint == null)
        {
            Debug.LogWarning("FlareGunner: " +
                "Missing bullet prefab or shoot point");
            return;
        }

        Vector3 target = player.position + Vector3.up;
        Vector3 dir = (target - shootPoint.position).normalized;

        GameObject bullet = Instantiate(
            bulletPrefab,
            shootPoint.position,
            Quaternion.LookRotation(dir));

        FrozenBullet fb = bullet.GetComponent<FrozenBullet>();
        if (fb != null) fb.isEnemyBullet = true;
    }

    // ─────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        agent.SetDestination(patrolPoints[patrolIndex].position);
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    void OnDrawGizmosSelected()
    {
        // Yellow sphere shows detection radius in scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position, detectionRadius);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (gunPickupPrefab != null)
            Instantiate(gunPickupPrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity);

        WorldTimeController.Instance?.UnregisterFlareGunner(this);
        Destroy(gameObject);
    }
}

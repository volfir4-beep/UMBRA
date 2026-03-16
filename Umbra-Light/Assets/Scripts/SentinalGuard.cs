using UnityEngine;
using UnityEngine.AI;

public class SentinelGuard : MonoBehaviour
{
    [Header("Detection")]
    public float visionRange = 8f;
    public float visionAngle = 90f;
    public float alertRadius = 12f;
    // If bullet lands within this radius — guard investigates

    [Header("Investigation")]
    public float investigateTime = 5f;
    // How long guard searches before returning to post
    public float investigateSpeed = 3.5f;

    [Header("Shooting")]
    public GameObject enemyBulletPrefab;
    public Transform shootPoint;
    public float shootRange = 8f;
    public float shootCooldown = 2f;
    private float lastShootTime;

    [Header("Drop")]
    public GameObject gunPickupPrefab;

    [Header("Animation")]
    public float deathAnimationLength = 2f;

    // Internal
    private NavMeshAgent agent;
    private Transform player;
    private bool isDead = false;
    private float currentTimeScale = 1f;
    private Animator animator;

    // Post = original standing position
    // Guard always returns here after investigating
    private Vector3 postPosition;
    private Quaternion postRotation;

    // Investigation
    private Vector3 investigateTarget;
    private float investigateTimer = 0f;
    private bool isInvestigating = false;
    private bool isReturning = false;

    public enum State
    {
        Standing,
        Investigating,
        Returning,
        Chasing,
        Shooting
    }
    public State currentState = State.Standing;

    // ─────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        // Store starting position as permanent post
        postPosition = transform.position;
        postRotation = transform.rotation;

        // Stand still at start
        agent.SetDestination(postPosition);

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("SentinelGuard: " +
                "Cannot find Player tag");

        // Register with WorldTimeController
        WorldTimeController.Instance?.RegisterSentinel(this);
    }

    void OnDestroy()
    {
        WorldTimeController.Instance?.UnregisterSentinel(this);
    }

    // ─────────────────────────────────────────
    // TIME SCALE
    // ─────────────────────────────────────────

    public void SetTimeScale(float scale)
    {
        currentTimeScale = scale;

        agent.speed = investigateSpeed * currentTimeScale;
        agent.angularSpeed = 180f * currentTimeScale;
        agent.acceleration = 10f * currentTimeScale;

        if (currentTimeScale < 0.02f)
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

        float timeScale =
            WorldTimeController.Instance != null
            ? WorldTimeController.Instance.worldTimeScale
            : 1f;

        // Frozen — face player if close
        if (timeScale < 0.02f)
        {
            float dist = Vector3.Distance(
                transform.position, player.position);
            if (dist <= visionRange)
                RotateToFacePlayer();
            return;
        }

        float distToPlayer = Vector3.Distance(
            transform.position, player.position);

        bool canSee = CanSeePlayer();

        // If sees player at any point — switch to combat
        if (canSee && distToPlayer <= visionRange)
        {
            if (distToPlayer <= shootRange)
                currentState = State.Shooting;
            else
                currentState = State.Chasing;
        }

        switch (currentState)
        {
            case State.Standing:
                DoStand();
                break;

            case State.Investigating:
                DoInvestigate();
                break;

            case State.Returning:
                DoReturn();
                break;

            case State.Chasing:
                DoChase(distToPlayer);
                break;

            case State.Shooting:
                DoShoot(distToPlayer);
                break;
        }

        // Update animator
        if (animator != null)
        {
            animator.SetFloat("Speed",
                agent.velocity.magnitude);
            animator.SetBool("IsShooting",
                currentState == State.Shooting);
        }
    }

    // ─────────────────────────────────────────
    // STATES
    // ─────────────────────────────────────────

    void DoStand()
    {
        // Stay at post — face original direction
        agent.SetDestination(postPosition);

        // Slowly return to post rotation when standing
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            postRotation,
            Time.deltaTime * 2f);
    }

    void DoInvestigate()
    {
        agent.SetDestination(investigateTarget);

        // Count down while near the target
        float distToTarget = Vector3.Distance(
            transform.position, investigateTarget);

        if (distToTarget < 1.5f)
        {
            investigateTimer += Time.deltaTime;

            // Look around slowly while investigating
            transform.Rotate(
                Vector3.up, 60f * Time.deltaTime);

            if (investigateTimer >= investigateTime)
            {
                // Nothing found — return to post
                investigateTimer = 0f;
                isInvestigating = false;
                currentState = State.Returning;
                Debug.Log("SentinelGuard: " +
                    "Nothing found — returning to post");
            }
        }
    }

    void DoReturn()
    {
        agent.SetDestination(postPosition);

        float distToPost = Vector3.Distance(
            transform.position, postPosition);

        if (distToPost < 0.5f)
        {
            // Back at post — stand still again
            currentState = State.Standing;
            agent.SetDestination(postPosition);
        }
    }

    void DoChase(float distToPlayer)
    {
        agent.SetDestination(player.position);

        // Switch to shoot if close enough
        if (distToPlayer <= shootRange && CanSeePlayer())
            currentState = State.Shooting;

        // Lost sight — investigate last known position
        if (!CanSeePlayer())
        {
            investigateTarget = player.position;
            investigateTimer = 0f;
            currentState = State.Investigating;
        }
    }

    void DoShoot(float distToPlayer)
    {
        // Stop moving
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.SetDestination(transform.position);

        // Face player
        Vector3 dirToPlayer = new Vector3(
            player.position.x - transform.position.x,
            0f,
            player.position.z - transform.position.z)
            .normalized;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dirToPlayer),
            Time.deltaTime * 10f);

        // Back away if player too close
        if (distToPlayer > shootRange || !CanSeePlayer())
        {
            currentState = State.Chasing;
            return;
        }

        // Shoot on cooldown
        if (Time.time > lastShootTime + shootCooldown)
        {
            FireBullet();
            lastShootTime = Time.time;
        }
    }

    // ─────────────────────────────────────────
    // SOUND ALERT — called by FrozenBullet
    // ─────────────────────────────────────────

    public void HearSound(Vector3 soundPosition)
    {
        // Only react if not already in combat
        if (currentState == State.Chasing ||
            currentState == State.Shooting)
            return;

        float dist = Vector3.Distance(
            transform.position, soundPosition);

        if (dist <= alertRadius)
        {
            Debug.Log("SentinelGuard: " +
                "Heard something — investigating");

            investigateTarget = soundPosition;
            investigateTimer = 0f;
            currentState = State.Investigating;
        }
    }

    // ─────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────

    void RotateToFacePlayer()
    {
        Vector3 dir = new Vector3(
            player.position.x - transform.position.x,
            0f,
            player.position.z - transform.position.z);

        if (dir == Vector3.zero) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * 3f);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 dirToPlayer =
            player.position - transform.position;
        float dist = dirToPlayer.magnitude;

        if (dist < 2.5f) return true;

        float angle = Vector3.Angle(
            transform.forward, dirToPlayer);
        if (angle > visionAngle / 2f) return false;

        Vector3 eyePos =
            transform.position + Vector3.up * 1.5f;
        Vector3 playerChest =
            player.position + Vector3.up * 0.9f;
        Vector3 dir = playerChest - eyePos;

        if (Physics.Raycast(eyePos, dir.normalized,
            dir.magnitude,
            LayerMask.GetMask("Environment")))
            return false;

        return true;
    }

    void FireBullet()
    {
        if (enemyBulletPrefab == null ||
            shootPoint == null) return;

        Vector3 targetPos =
            player.position + Vector3.up * 0.9f;
        Vector3 dir =
            (targetPos - shootPoint.position).normalized;

        GameObject bullet = Instantiate(
            enemyBulletPrefab,
            shootPoint.position,
            Quaternion.LookRotation(dir));

        FrozenBullet fb =
            bullet.GetComponent<FrozenBullet>();
        if (fb != null) fb.isEnemyBullet = true;
    }

    void OnDrawGizmosSelected()
    {
        // Yellow = alert radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position, alertRadius);

        // Red = vision range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position, visionRange);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null)
            animator.SetBool("IsDead", true);

        if (gunPickupPrefab != null)
            Instantiate(gunPickupPrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity);

        WorldTimeController.Instance?
            .UnregisterSentinel(this);

        agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.enabled = false;

        Destroy(gameObject, deathAnimationLength);
    }
}
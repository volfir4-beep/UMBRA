using UnityEngine;
using UnityEngine.AI;

public class SentinelGuard : MonoBehaviour
{
    [Header("Detection")]
    public float visionRange = 8f;
    public float visionAngle = 90f;
    public float alertRadius = 12f;

    [Header("Investigation")]
    public float investigateTime = 5f;
    public float investigateSpeed = 3.5f;

    [Header("Shooting")]
    public GameObject enemyBulletPrefab;
    public Transform shootPoint;
    public float shootRange = 8f;
    public float shootCooldown = 2f;
    private float lastShootTime;

    [Header("Aim Settings")]
    public float aimTime = 0.5f;
    public float facingThreshold = 15f;
    private bool isAimed = false;
    private float aimTimer = 0f;

    [Header("Drop")]
    public GameObject gunPickupPrefab;

    [Header("Animation")]
    public float deathAnimationLength = 2f;

    private NavMeshAgent agent;
    private Transform player;
    private bool isDead = false;
    private float currentTimeScale = 1f;
    private Animator animator;

    private Vector3 postPosition;
    private Quaternion postRotation;

    private Vector3 investigateTarget;
    private float investigateTimer = 0f;

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

        // Force full cooldown before first shot
        lastShootTime = Time.time;

        postPosition = transform.position;
        postRotation = transform.rotation;

        agent.SetDestination(postPosition);

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("SentinelGuard: " +
                "Cannot find Player tag");

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

        // If sees player — switch to combat
        if (canSee && distToPlayer <= visionRange)
        {
            if (distToPlayer <= shootRange)
                currentState = State.Shooting;
            else
                currentState = State.Chasing;
        }

        switch (currentState)
        {
            case State.Standing: DoStand(); break;
            case State.Investigating: DoInvestigate(); break;
            case State.Returning: DoReturn(); break;
            case State.Chasing: DoChase(distToPlayer); break;
            case State.Shooting: DoShoot(distToPlayer); break;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed",
                agent.velocity.magnitude);
            animator.SetBool("IsShooting",
                currentState == State.Shooting && isAimed);
        }
    }

    // ─────────────────────────────────────────
    // STATES
    // ─────────────────────────────────────────

    void DoStand()
    {
        agent.SetDestination(postPosition);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            postRotation,
            Time.deltaTime * 2f);
    }

    void DoInvestigate()
    {
        agent.SetDestination(investigateTarget);

        float distToTarget = Vector3.Distance(
            transform.position, investigateTarget);

        if (distToTarget < 1.5f)
        {
            investigateTimer += Time.deltaTime;

            transform.Rotate(
                Vector3.up, 60f * Time.deltaTime);

            if (investigateTimer >= investigateTime)
            {
                investigateTimer = 0f;
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
            currentState = State.Standing;
            agent.SetDestination(postPosition);
        }
    }

    void DoChase(float distToPlayer)
    {
        // Reset aim — must re-aim when entering shoot state
        isAimed = false;
        aimTimer = 0f;

        agent.SetDestination(player.position);

        if (distToPlayer <= shootRange && CanSeePlayer())
            currentState = State.Shooting;

        if (!CanSeePlayer())
        {
            investigateTarget = player.position;
            investigateTimer = 0f;
            currentState = State.Investigating;
        }
    }

    void DoShoot(float distToPlayer)
    {
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.SetDestination(transform.position);

        Vector3 dirToPlayer = new Vector3(
            player.position.x - transform.position.x,
            0f,
            player.position.z - transform.position.z)
            .normalized;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dirToPlayer),
            Time.deltaTime * 10f);

        float angleToPlayer = Vector3.Angle(
            transform.forward, dirToPlayer);

        // Lost player — chase and reset aim
        if (distToPlayer > shootRange || !CanSeePlayer())
        {
            isAimed = false;
            aimTimer = 0f;
            currentState = State.Chasing;
            return;
        }

        if (!isAimed)
        {
            // AIM PHASE — face player for aimTime seconds
            if (angleToPlayer < facingThreshold)
            {
                aimTimer += Time.deltaTime;
                if (aimTimer >= aimTime)
                {
                    isAimed = true;
                    aimTimer = 0f;
                    // Force full cooldown before first shot
                    lastShootTime = Time.time;
                    Debug.Log("SentinelGuard: Aimed");
                }
            }
            else
            {
                // Not facing — reset timer
                aimTimer = 0f;
            }
            return; // No shooting during aim phase
        }

        // FIRE PHASE
        if (angleToPlayer < facingThreshold * 2f &&
            Time.time > lastShootTime + shootCooldown)
        {
            FireBullet();
            lastShootTime = Time.time;
        }
    }

    // ─────────────────────────────────────────
    // SOUND ALERT
    // ─────────────────────────────────────────

    public void HearSound(Vector3 soundPosition)
    {
        if (currentState == State.Chasing ||
            currentState == State.Shooting)
            return;

        float dist = Vector3.Distance(
            transform.position, soundPosition);

        if (dist <= alertRadius)
        {
            Debug.Log("SentinelGuard: Heard something");
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

        Debug.DrawRay(eyePos, dir, Color.red);

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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position, alertRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position, visionRange);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsShooting", false);
            animator.SetBool("IsDead", true);
        }

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
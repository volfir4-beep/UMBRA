using UnityEngine;
using UnityEngine.AI;

public class Security : MonoBehaviour
{
    [Header("Movement")]
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 6f;
    public float rushDistance = 4f;
    public float rushMultiplier = 1.4f;
    public bool immuneToTimeFreeze = false;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    private int patrolIndex = 0;
    private float patrolWaitTimer = 0f;
    public float patrolWaitTime = 1.5f;

    [Header("Detection")]
    public float alertRange = 12f;
    public float chaseRange = 18f;
    public float shootRange = 8f;
    public float backupDistance = 3f;
    public float visionAngle = 90f;
    public float awarenessRange = 15f;

    [Header("Alert / Investigation")]
    public float investigateTime = 4f;
    private Vector3 lastKnownPlayerPos;
    private float investigateTimer = 0f;
    private bool isAlerted = false;

    [Header("Shooting")]
    public GameObject enemyBulletPrefab;
    public Transform shootPoint;
    public float shootCooldown = 1.8f;
    // Time between shots after first shot

    [Header("Aim Settings")]
    public float aimTime = 0.5f;
    // Seconds guard must face player before firing FIRST shot
    public float facingThreshold = 15f;
    public float rotationSpeed = 10f;

   

    [Header("Animation")]
    public float deathAnimationLength = 2f;

    // Internal
    private NavMeshAgent agent;
    private Transform player;
    private bool isDead = false;
    private float currentTimeScale = 1f;
    private Animator animator;

    // Shoot state tracking
    private bool isAimed = false;
    private float aimTimer = 0f;
    private float lastShootTime;
    private bool wasInShootState = false;
    // wasInShootState tracks if we were shooting last frame
    // so we know the exact frame we enter shooting state

    // Lost sight grace — prevents flickering
    private float lostSightTimer = 0f;
    public float lostSightGrace = 0.5f;

    public enum State
    {
        Patrolling,
        Alerted,
        Chasing,
        Shooting,
        Backing
    }
    public State currentState = State.Patrolling;

    // ─────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        animator = GetComponentInChildren<Animator>();

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Security: Cannot find Player tag");

        WorldTimeController.Instance.RegisterEnemy(this);
        GoToNextPatrolPoint();

        lastShootTime = Time.time;
    }

    void OnDestroy()
    {
        WorldTimeController.Instance?.UnregisterEnemy(this);
    }

    // ─────────────────────────────────────────
    // TIME SCALE
    // ─────────────────────────────────────────

    public void SetTimeScale(float scale)
    {
        currentTimeScale = immuneToTimeFreeze ? 1f : scale;

        agent.speed = GetCurrentSpeed() * currentTimeScale;
        agent.angularSpeed = 180f * currentTimeScale;
        agent.acceleration = 12f * currentTimeScale;

        if (currentTimeScale < 0.02f && !immuneToTimeFreeze)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    float GetCurrentSpeed()
    {
        if (currentState == State.Patrolling)
            return patrolSpeed;

        if (player != null)
        {
            float d = Vector3.Distance(
                transform.position, player.position);
            if (d < rushDistance)
                return chaseSpeed * rushMultiplier;
        }
        return chaseSpeed;
    }

    // ─────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────

    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float timeScale = WorldTimeController.Instance.worldTimeScale;
        float dist = Vector3.Distance(
            transform.position, player.position);

        // FROZEN
        if (timeScale < 0.02f && !immuneToTimeFreeze)
        {
            if (dist <= awarenessRange)
                RotateToFacePlayer();
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetBool("IsShooting", false);
            }
            return;
        }

        agent.speed = GetCurrentSpeed() * currentTimeScale;

        DecideState(dist);

        // Detect the EXACT frame we enter shooting state
        bool enteringShootNow =
            currentState == State.Shooting && !wasInShootState;

        if (enteringShootNow)
        {
            // Just entered shoot state this frame
            // Reset aim and set lastShootTime to NOW
            // This means first shot won't fire until
            // aimTime passes AND full cooldown passes
            isAimed = false;
            aimTimer = 0f;
            lastShootTime = Time.time;
            // Setting to Time.time forces full cooldown
            // after aim phase before first shot fires
        }

        // Track whether we were in shoot state last frame
        wasInShootState = (currentState == State.Shooting);

        // Reset aim when truly leaving combat
        if (currentState == State.Patrolling ||
        currentState == State.Alerted ||
        currentState == State.Chasing ||
        currentState == State.Backing)
        {
            isAimed = false;
            aimTimer = 0f;
        }

        // Animator
        if (animator != null)
        {
            animator.SetFloat("Speed",
                agent.velocity.magnitude);
            animator.SetBool("IsShooting",
                (currentState == State.Shooting ||
                 currentState == State.Backing) && isAimed);
        }

        switch (currentState)
        {
            case State.Patrolling: DoPatrol(); break;
            case State.Alerted: DoAlerted(); break;
            case State.Chasing: DoChase(); break;
            case State.Shooting: DoShoot(); break;
            case State.Backing: DoBackup(); break;
        }
    }

    // ─────────────────────────────────────────
    // STATE DECISION
    // ─────────────────────────────────────────

    void DecideState(float dist)
    {
        bool canSee = CanSeePlayer();

        // Too close — back up
        if (dist < backupDistance && canSee)
        {
            lostSightTimer = 0f;
            currentState = State.Backing;
            return;
        }

        // Shoot range
        if (dist <= shootRange && canSee)
        {
            lostSightTimer = 0f;
            currentState = State.Shooting;
            lastKnownPlayerPos = player.position;
            isAlerted = true;
            return;
        }

        // Grace period — just left shoot range
        // Prevents flickering when on boundary
        if (wasInShootState)
        {
            if (!canSee || dist > shootRange)
            {
                lostSightTimer += Time.deltaTime;
                if (lostSightTimer < lostSightGrace)
                {
                    currentState = State.Shooting;
                    return;
                }
            }
            else
            {
                lostSightTimer = 0f;
            }
        }

        // Chase range
        if (dist <= chaseRange && (canSee || isAlerted))
        {
            if (canSee)
            {
                currentState = State.Chasing;
                lastKnownPlayerPos = player.position;
                isAlerted = true;
                investigateTimer = 0f;
            }
            else if (isAlerted)
            {
                currentState = State.Alerted;
            }
            return;
        }

        // Alert range
        if (dist <= alertRange && canSee)
        {
            currentState = State.Chasing;
            lastKnownPlayerPos = player.position;
            isAlerted = true;
            return;
        }

        if (!isAlerted)
            currentState = State.Patrolling;
        else
            currentState = State.Alerted;
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
    // ALERTED
    // ─────────────────────────────────────────

    void DoAlerted()
    {
        if (agent.destination != lastKnownPlayerPos)
            agent.SetDestination(lastKnownPlayerPos);

        investigateTimer += Time.deltaTime;
        if (investigateTimer >= investigateTime)
        {
            isAlerted = false;
            investigateTimer = 0f;
            currentState = State.Patrolling;
            GoToNextPatrolPoint();
        }
    }

    // ─────────────────────────────────────────
    // CHASE
    // ─────────────────────────────────────────

    void DoChase()
    {
        agent.SetDestination(player.position);
        lastKnownPlayerPos = player.position;
    }

    // ─────────────────────────────────────────
    // SHOOT
    // ─────────────────────────────────────────

    void DoShoot()
    {
        // Hard stop every frame — no sliding
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.SetDestination(transform.position);

        // Horizontal direction to player only
        // No vertical tilt when rotating
        Vector3 dirToPlayer = new Vector3(
            player.position.x - transform.position.x,
            0f,
            player.position.z - transform.position.z).normalized;

        // Always rotate toward player
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dirToPlayer),
            Time.deltaTime * rotationSpeed);

        float angleToPlayer = Vector3.Angle(
            transform.forward, dirToPlayer);

        if (!isAimed)
        {
            // AIM PHASE — must face player for aimTime seconds
            if (angleToPlayer < facingThreshold)
            {
                aimTimer += Time.deltaTime;
                if (aimTimer >= aimTime)
                {
                    isAimed = true;
                    aimTimer = 0f;
                    // Reset shoot timer so full cooldown runs
                    // before first shot fires
                    lastShootTime = Time.time;
                    Debug.Log("Security: Aimed — firing soon");
                }
            }
            else
            {
                // Not facing — reset aim timer
                aimTimer = 0f;
            }
            // No shooting in aim phase
            return;
        }

        // FIRE PHASE
        // Only fire when facing within threshold
        if (angleToPlayer < facingThreshold * 2f)
        {
            float dist = Vector3.Distance(
                transform.position, player.position);

            float cooldown = dist < shootRange / 2f
                ? shootCooldown * 0.6f
                : shootCooldown;

            // Only shoot in backup if already aimed from shoot state
            if (isAimed && Time.time > lastShootTime + shootCooldown)
            {
                FireBullet();
                lastShootTime = Time.time;
            }
        }
    }

    // ─────────────────────────────────────────
    // BACKUP
    // ─────────────────────────────────────────

    void DoBackup()
    {
        Vector3 away = (transform.position -
            player.position).normalized;
        away.y = 0f;

        Vector3 backupTarget = transform.position + away * 2f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(backupTarget, out hit,
            2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);

        Vector3 dirToPlayer = new Vector3(
            player.position.x - transform.position.x,
            0f,
            player.position.z - transform.position.z).normalized;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dirToPlayer),
            Time.deltaTime * rotationSpeed);

        if (Time.time > lastShootTime + shootCooldown)
        {
            FireBullet();
            lastShootTime = Time.time;
        }
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
        Vector3 playerCenter =
            player.position + Vector3.up * 0.9f;
        Vector3 dir = playerCenter - eyePos;

        if (Physics.Raycast(eyePos, dir.normalized,
            dir.magnitude,
            LayerMask.GetMask("Environment")))
            return false;

        return true;
    }

    // ─────────────────────────────────────────
    // FIRE BULLET
    // ─────────────────────────────────────────

    void FireBullet()
    {
        if (enemyBulletPrefab == null)
        {
            Debug.LogWarning("Security: No bullet prefab");
            return;
        }
        if (shootPoint == null)
        {
            Debug.LogWarning("Security: No shoot point");
            return;
        }

        // Aim at player center body — not feet, not head
        Vector3 playerCenter =
            player.position + Vector3.up * 0.9f;
        Vector3 finalDir =
            (playerCenter - shootPoint.position).normalized;

        GameObject bullet = Instantiate(
            enemyBulletPrefab,
            shootPoint.position,
            Quaternion.LookRotation(finalDir));

        FrozenBullet fb = bullet.GetComponent<FrozenBullet>();
        if (fb != null)
            fb.isEnemyBullet = true;
    }

    // ─────────────────────────────────────────
    // DEATH
    // ─────────────────────────────────────────

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Stop animator fighting death state
        // by disabling all parameter updates
        if (animator != null)
        {
            // Reset everything before triggering death
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsShooting", false);
            animator.SetBool("IsDead", true);
        }

        

        WorldTimeController.Instance?.UnregisterEnemy(this);

        // Stop all movement
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.enabled = false;

        // Disable this script so Update stops running
        // Prevents animator parameter fighting
        this.enabled = false;

        Destroy(gameObject, deathAnimationLength);
    }
}
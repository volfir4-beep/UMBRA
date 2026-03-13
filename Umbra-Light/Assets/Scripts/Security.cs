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
    public float shootCooldown = 2f;
    private float lastShootTime;

    [Header("Drop")]
    public GameObject gunPickupPrefab;

    // Internal
    private NavMeshAgent agent;
    private Transform player;
    private bool isDead = false;
    private float currentTimeScale = 1f;

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

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("Security Officer: Found player");
        }
        else
        {
            Debug.LogError("Security Officer: Cannot find " +
                "Player tag — set Tag on Player object");
        }

        WorldTimeController.Instance.RegisterEnemy(this);
        GoToNextPatrolPoint();
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

        float currentSpeed = GetCurrentSpeed();
        agent.speed = currentSpeed * currentTimeScale;
        agent.angularSpeed = 180f * currentTimeScale;
        agent.acceleration = 12f * currentTimeScale;

        // Hard stop when fully frozen
        if (currentTimeScale < 0.02f && !immuneToTimeFreeze)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    // Returns correct speed based on state and distance
    float GetCurrentSpeed()
    {
        if (currentState == State.Patrolling)
            return patrolSpeed;

        // Rushing — very close to player
        if (player != null)
        {
            float dist = Vector3.Distance(
                transform.position, player.position);

            if (dist < rushDistance)
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
        float distToPlayer = Vector3.Distance(
            transform.position, player.position);

        // FROZEN — only rotate to watch player
        if (timeScale < 0.02f && !immuneToTimeFreeze)
        {
            if (distToPlayer <= awarenessRange)
                RotateToFacePlayer();
            return;
        }

        // Update speed every frame based on current state
        float speed = GetCurrentSpeed();
        agent.speed = speed * currentTimeScale;

        // Decide and execute state
        DecideState(distToPlayer);

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
            currentState = State.Backing;
            return;
        }

        // Shoot range
        if (dist <= shootRange && canSee)
        {
            currentState = State.Shooting;
            lastKnownPlayerPos = player.position;
            isAlerted = true;
            return;
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

        // Alert range — vision cone + LOS
        if (dist <= alertRange && canSee)
        {
            currentState = State.Chasing;
            lastKnownPlayerPos = player.position;
            isAlerted = true;
            return;
        }

        // Out of all ranges
        if (!isAlerted)
            currentState = State.Patrolling;
        else
            currentState = State.Alerted;
    }

    // ─────────────────────────────────────────
    // STATE BEHAVIOURS
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
            Debug.Log("Security Officer: Lost player — resuming patrol");
        }
    }

    void DoChase()
    {
        agent.SetDestination(player.position);
        lastKnownPlayerPos = player.position;
    }

    void DoShoot()
    {
        // Stop moving
        agent.SetDestination(transform.position);

        // Smooth face toward player
        Vector3 lookTarget = new Vector3(
            player.position.x,
            transform.position.y,
            player.position.z);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(lookTarget - transform.position),
            Time.deltaTime * 8f);

        // Close range = faster shooting
        float dist = Vector3.Distance(
            transform.position, player.position);

        float currentCooldown = dist < shootRange / 2f
            ? shootCooldown / 2f   // Close = shoot twice as fast
            : shootCooldown;       // Normal range = normal cooldown

        if (Time.time > lastShootTime + currentCooldown)
        {
            FireBullet();
            lastShootTime = Time.time;
        }
    }

    void DoBackup()
    {
        // Move away from player
        Vector3 away = (transform.position -
            player.position).normalized;
        away.y = 0f;

        Vector3 backupTarget = transform.position + away * 2f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(backupTarget, out hit,
            2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        // Still face player
        Vector3 lookTarget = new Vector3(
            player.position.x,
            transform.position.y,
            player.position.z);
        transform.LookAt(lookTarget);

        // Keep shooting while backing up
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
        Vector3 lookTarget = new Vector3(
            player.position.x,
            transform.position.y,
            player.position.z);

        Vector3 dir = lookTarget - transform.position;
        if (dir == Vector3.zero) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * 3f);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 dirToPlayer = player.position - transform.position;
        float dist = dirToPlayer.magnitude;

        // Always detect if extremely close
        if (dist < 2.5f) return true;

        // Vision cone check
        float angle = Vector3.Angle(
            transform.forward, dirToPlayer);
        if (angle > visionAngle / 2f) return false;

        // Wall blocking check
        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 playerChest = player.position + Vector3.up * 1f;
        Vector3 direction = playerChest - eyePos;

        if (Physics.Raycast(
            eyePos,
            direction.normalized,
            direction.magnitude,
            LayerMask.GetMask("Environment")))
            return false;

        return true;
    }

    // ─────────────────────────────────────────
    // SHOOTING
    // ─────────────────────────────────────────

    void FireBullet()
    {
        if (enemyBulletPrefab == null)
        {
            Debug.LogWarning("Security Officer: " +
                "No bullet prefab assigned");
            return;
        }

        if (shootPoint == null)
        {
            Debug.LogWarning("Security Officer: " +
                "No shoot point assigned");
            return;
        }

        // Aim at chest not feet
        Vector3 targetPos = player.position + Vector3.up * 1f;
        Vector3 direction =
            (targetPos - shootPoint.position).normalized;

        GameObject bullet = Instantiate(
            enemyBulletPrefab,
            shootPoint.position,
            Quaternion.LookRotation(direction));

        FrozenBullet fb = bullet.GetComponent<FrozenBullet>();
        if (fb != null)
        {
            fb.isEnemyBullet = true;
            Debug.Log("Security Officer: Fired bullet at player");
        }
    }

    // ─────────────────────────────────────────
    // DEATH
    // ─────────────────────────────────────────

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (gunPickupPrefab != null)
            Instantiate(
                gunPickupPrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity);

        WorldTimeController.Instance?.UnregisterEnemy(this);
        Destroy(gameObject);
    }
}
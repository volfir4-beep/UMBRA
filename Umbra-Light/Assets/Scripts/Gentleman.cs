using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.Rendering.DebugUI.Table;

public class Gentleman : MonoBehaviour
{
    [Header("Detection")]
    public float visionRange = 7f;
    public float visionAngle = 90f;
    public float alertRadius = 10f;

    [Header("Melee Attack")]
    public float meleeRange = 1.8f;
    public float meleeCooldown = 1.5f;
    public float chaseSpeed = 4f;

    [Header("Cane")]
    public CaneHitbox caneHitbox;
    // Drag the Cane object here in Inspector
    // Script automatically finds it if left empty

    public float hitboxActiveTime = 0.4f;
    // How long cane hitbox stays active per swing
    // Set this to the moment in animation when cane swings

    public float hitboxDelay = 0.3f;
    // Seconds after animation starts before hitbox activates
    // Matches when cane actually swings in the animation

    [Header("Investigation")]
    public float investigateTime = 4f;
    public float investigateSpeed = 2.5f;

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
    private float lastHitTime;
    private bool isSwinging = false;

    public enum State
    {
        Standing,
        Investigating,
        Returning,
        Chasing,
        Hitting
    }
    public State currentState = State.Standing;

    // ─────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        // Auto find cane hitbox if not assigned
        if (caneHitbox == null)
            caneHitbox =
                GetComponentInChildren<CaneHitbox>();

        if (caneHitbox == null)
            Debug.LogWarning("Gentleman: No CaneHitbox found " +
                "— drag Cane object into Cane Hitbox field");

        postPosition = transform.position;
        postRotation = transform.rotation;
        agent.SetDestination(postPosition);

        // Force full cooldown before first swing
        lastHitTime = Time.time;

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Gentleman: Cannot find Player tag");

        WorldTimeController.Instance?.RegisterGentleman(this);
    }

    void OnDestroy()
    {
        WorldTimeController.Instance?.UnregisterGentleman(this);
    }

    // ─────────────────────────────────────────
    // TIME SCALE
    // ─────────────────────────────────────────

    public void SetTimeScale(float scale)
    {
        currentTimeScale = scale;

        agent.speed = chaseSpeed * currentTimeScale;
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

        // Sees player — switch to combat
        if (canSee && distToPlayer <= visionRange)
        {
            if (distToPlayer <= meleeRange)
                currentState = State.Hitting;
            else
                currentState = State.Chasing;
        }

        switch (currentState)
        {
            case State.Standing: DoStand(); break;
            case State.Investigating: DoInvestigate(); break;
            case State.Returning: DoReturn(); break;
            case State.Chasing: DoChase(distToPlayer); break;
            case State.Hitting: DoHit(distToPlayer); break;
        }

        if (animator != null)
            animator.SetFloat("Speed",
                agent.velocity.magnitude);
    }

    // ─────────────────────────────────────────
    // STATES
    // ─────────────────────────────────────────

    void DoStand()
    {
        agent.SetDestination(postPosition);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, postRotation,
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
            transform.Rotate(Vector3.up, 50f * Time.deltaTime);

            if (investigateTimer >= investigateTime)
            {
                investigateTimer = 0f;
                currentState = State.Returning;
            }
        }
    }

    void DoReturn()
    {
        agent.SetDestination(postPosition);

        if (Vector3.Distance(transform.position, postPosition) < 0.5f)
        {
            currentState = State.Standing;
            agent.SetDestination(postPosition);
        }
    }

    void DoChase(float distToPlayer)
    {
        agent.SetDestination(player.position);

        if (distToPlayer <= meleeRange && CanSeePlayer())
            currentState = State.Hitting;

        if (!CanSeePlayer())
        {
            investigateTarget = player.position;
            investigateTimer = 0f;
            currentState = State.Investigating;
        }
    }

    void DoHit(float distToPlayer)
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

        // Player moved too far away — chase
        if (distToPlayer > meleeRange * 1.8f)
        {
            currentState = State.Chasing;
            return;
        }

        // Lost sight — investigate
        if (!CanSeePlayer() && distToPlayer > meleeRange)
        {
            investigateTarget = player.position;
            investigateTimer = 0f;
            currentState = State.Investigating;
            return;
        }

        // Swing on cooldown
        if (!isSwinging &&
            Time.time > lastHitTime + meleeCooldown)
        {
            lastHitTime = Time.time;
            StartCoroutine(SwingCane());
        }
    }

    IEnumerator SwingCane()
    {
        isSwinging = true;

        // Play animation
        if (animator != null)
            animator.SetBool("IsHitting", true);

        // Wait for the moment in animation when cane swings
        yield return new WaitForSeconds(hitboxDelay);

        // Enable cane hitbox — NOW the cane can hurt player
        if (caneHitbox != null)
            caneHitbox.EnableHitbox();

        // Keep hitbox active for swing duration
        yield return new WaitForSeconds(hitboxActiveTime);

        // Disable hitbox — swing over
        if (caneHitbox != null)
            caneHitbox.DisableHitbox();

        // Wait for animation to finish
        yield return new WaitForSeconds(0.4f);

        if (animator != null)
            animator.SetBool("IsHitting", false);

        isSwinging = false;
    }

    // ─────────────────────────────────────────
    // SOUND ALERT
    // ─────────────────────────────────────────

    public void HearSound(Vector3 soundPosition)
    {
        if (currentState == State.Chasing ||
            currentState == State.Hitting)
            return;

        float dist = Vector3.Distance(
            transform.position, soundPosition);

        if (dist <= alertRadius)
        {
            Debug.Log("Gentleman: Heard something");
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

        if (dist < 2f) return true;

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alertRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Disable cane hitbox on death
        if (caneHitbox != null)
            caneHitbox.DisableHitbox();

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsHitting", false);
            animator.SetBool("IsDead", true);
        }

        WorldTimeController.Instance?.UnregisterGentleman(this);

        agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.enabled = false;

        Destroy(gameObject, deathAnimationLength);
    }
}

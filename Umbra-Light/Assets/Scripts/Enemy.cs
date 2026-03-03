using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float baseSpeed = 3f;
    public bool immuneToTimeFreeze = false;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    private int patrolIndex = 0;

    [Header("Detection")]
    public float detectionRange = 10f;
    public float detectionAngle = 50f;
    public float shootRange = 8f;

    [Header("Shooting")]
    public GameObject enemyBulletPrefab;
    public Transform shootPoint;
    public float shootCooldown = 2f;
    private float lastShootTime;

    [Header("Drop")]
    public GameObject gunPickupPrefab;

    private NavMeshAgent agent;
    private Transform player;
    private float currentTimeScale = 1f;
    private bool isDead = false;

    public enum State { Patrolling, Chasing, Shooting }
    private State currentState = State.Patrolling;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        WorldTimeController.Instance.RegisterEnemy(this);
    }

    void OnDestroy()
    {
        WorldTimeController.Instance?.UnregisterEnemy(this);
    }

    public void SetTimeScale(float scale)
    {
        currentTimeScale = immuneToTimeFreeze ? 1f : scale;
        agent.speed = baseSpeed * currentTimeScale;
        agent.angularSpeed = 180f * currentTimeScale;
        agent.acceleration = 10f * currentTimeScale;
    }

    void Update()
    {
        if (isDead) return;
        if (currentTimeScale < 0.02f && !immuneToTimeFreeze) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= shootRange && CanSeePlayer())
            currentState = State.Shooting;
        else if (dist <= detectionRange && CanSeePlayer())
            currentState = State.Chasing;
        else
            currentState = State.Patrolling;

        switch (currentState)
        {
            case State.Patrolling: Patrol(); break;
            case State.Chasing: ChasePlayer(); break;
            case State.Shooting: ShootAtPlayer(); break;
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;
        agent.SetDestination(patrolPoints[patrolIndex].position);
        if (Vector3.Distance(transform.position,
            patrolPoints[patrolIndex].position) < 0.8f)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    void ChasePlayer()
    {
        agent.SetDestination(player.position);
    }

    void ShootAtPlayer()
    {
        agent.SetDestination(transform.position);
        transform.LookAt(new Vector3(
            player.position.x,
            transform.position.y,
            player.position.z));

        if (Time.time > lastShootTime + shootCooldown)
        {
            FireBullet();
            lastShootTime = Time.time;
        }
    }

    void FireBullet()
    {
        if (enemyBulletPrefab == null || shootPoint == null) return;

        Vector3 direction = (player.position - shootPoint.position).normalized;
        GameObject bullet = Instantiate(enemyBulletPrefab,
            shootPoint.position,
            Quaternion.LookRotation(direction));

        FrozenBullet fb = bullet.GetComponent<FrozenBullet>();
        if (fb != null) fb.isEnemyBullet = true;
    }

    bool CanSeePlayer()
    {
        Vector3 dir = player.position - transform.position;
        if (Vector3.Angle(transform.forward, dir) > detectionAngle / 2f)
            return false;
        if (Physics.Raycast(transform.position + Vector3.up,
            dir.normalized, dir.magnitude,
            LayerMask.GetMask("Environment")))
            return false;
        return true;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        if (gunPickupPrefab != null)
            Instantiate(gunPickupPrefab, transform.position, Quaternion.identity);
        WorldTimeController.Instance?.UnregisterEnemy(this);
        Destroy(gameObject);
    }
}
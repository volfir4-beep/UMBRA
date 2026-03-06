using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float baseSpeed = 3f;
    public bool immuneToTimeFreeze = false;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    private int patrolIndex = 0;
    private float patrolWaitTimer = 0f;

    [Header("Detection")]
    public float detectionRange = 15f;
    public float detectionAngle = 360f;
    public float shootRange = 10f;

    [Header("Shooting")]
    public GameObject enemyBulletPrefab;
    public Transform shootPoint;
    public float shootCooldown = 2f;
    private float lastShootTime;

    [Header("Drop")]
    public GameObject gunPickupPrefab;

    private NavMeshAgent agent;
    private Transform player;
    private bool isDead = false;

    public enum State { Patrolling, Chasing, Shooting }
    public State currentState = State.Patrolling;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("Enemy found player successfully");
        }
        else
        {
            Debug.LogError("ENEMY ERROR — Cannot find Player tag! " +
                "Select Player object and set Tag to Player");
        }

        WorldTimeController.Instance.RegisterEnemy(this);

        // Go to first patrol point immediately
        if (patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[0].position);
    }

    void OnDestroy()
    {
        WorldTimeController.Instance?.UnregisterEnemy(this);
    }

    public void SetTimeScale(float scale)
    {
        float s = immuneToTimeFreeze ? 1f : scale;
        agent.speed = baseSpeed * s;
        agent.angularSpeed = 180f * s;
        agent.acceleration = 10f * s;
    }

    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float timeScale = WorldTimeController.Instance.worldTimeScale;
        if (timeScale < 0.02f && !immuneToTimeFreeze) return;

        float distToPlayer = Vector3.Distance(
            transform.position, player.position);

        // DETECTION — simplified, no angle check for now
        bool playerVisible = distToPlayer <= detectionRange;

        // Decide state based on distance
        if (distToPlayer <= shootRange && playerVisible)
            currentState = State.Shooting;
        else if (distToPlayer <= detectionRange && playerVisible)
            currentState = State.Chasing;
        else
            currentState = State.Patrolling;

        // Execute state
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

        // Check if agent has reached current patrol point
        if (!agent.pathPending &&
            agent.remainingDistance < 1f)
        {
            // Wait briefly then move to next point
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= 1f)
            {
                patrolWaitTimer = 0f;
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[patrolIndex].position);
                Debug.Log("Enemy moving to patrol point " + patrolIndex);
            }
        }
    }

    void ChasePlayer()
    {
        agent.SetDestination(player.position);
        Debug.Log("Enemy chasing player!");
    }

    void ShootAtPlayer()
    {
        agent.SetDestination(transform.position);

        Vector3 lookTarget = new Vector3(
            player.position.x,
            transform.position.y,
            player.position.z);
        transform.LookAt(lookTarget);

        if (Time.time > lastShootTime + shootCooldown)
        {
            FireBullet();
            lastShootTime = Time.time;
            Debug.Log("Enemy fired bullet!");
        }
    }

    void FireBullet()
    {
        if (enemyBulletPrefab == null)
        {
            Debug.LogError("Enemy bullet prefab not assigned!");
            return;
        }
        if (shootPoint == null)
        {
            Debug.LogError("ShootPoint not assigned!");
            return;
        }

        Vector3 direction = (player.position -
            shootPoint.position).normalized;

        GameObject bullet = Instantiate(
            enemyBulletPrefab,
            shootPoint.position,
            Quaternion.LookRotation(direction));

        FrozenBullet fb = bullet.GetComponent<FrozenBullet>();
        if (fb != null) fb.isEnemyBullet = true;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (gunPickupPrefab != null)
            Instantiate(gunPickupPrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity);

        WorldTimeController.Instance?.UnregisterEnemy(this);
        Destroy(gameObject);
    }
}

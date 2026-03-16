using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Bullet")]
    public GameObject bulletPrefab;
    public Transform gunPoint;

    [Header("Bullet Limit")]
    public int maxBullets = 6;
    private int currentBullets;
    private bool bulletInAir = false;

    [Header("Melee")]
    public float meleeRange = 2f;
    public float meleeCooldown = 0.8f;
    private float lastMeleeTime = -10f;

    [Header("Animation")]
    public Animator playerAnimator;
    // Drag the MyCharacter_TPose object here

    [Header("Gun In Hand")]
    public GameObject gunInHand;
    // Drag GunInHand mesh (child of RightHand bone)

    private bool hasGun = false;

    void Start()
    {
        currentBullets = maxBullets;
        hasGun = false;

        if (gunInHand != null)
            gunInHand.SetActive(false);

        if (playerAnimator == null)
            playerAnimator =
                GetComponentInChildren<Animator>();

        // Force reset ALL parameters on start
        // Prevents Unity caching values from last session
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("HasGun", false);
            playerAnimator.SetBool("IsShooting", false);
            playerAnimator.SetBool("IsMelee", false);
            playerAnimator.SetBool("IsDead", false);
            playerAnimator.SetBool("IsPickingUp", false);
            playerAnimator.SetFloat("Speed", 0f);

            // Force animator to reset to default state
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }
    }

    void Update()
    {
        // Update movement speed for walk/run blend
        if (playerAnimator != null)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                float speed = new Vector3(
                    rb.linearVelocity.x,
                    0f,
                    rb.linearVelocity.z).magnitude;

                playerAnimator.SetFloat("Speed", speed);
            }
        }

        if (Input.GetButtonDown("Fire1"))
        {
            if (TryMelee()) return;
            TryShoot();
        }
    }

    // ─────────────────────────────────────────
    // MELEE
    // ─────────────────────────────────────────

    bool TryMelee()
    {
        if (Time.time < lastMeleeTime + meleeCooldown)
            return false;

        Collider[] hits = Physics.OverlapSphere(
            transform.position, meleeRange);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Security sec =
                    hit.GetComponent<Security>() ??
                    hit.GetComponentInParent<Security>();

                if (sec != null)
                {
                    sec.Die();
                    lastMeleeTime = Time.time;

                    // Punch animation
                    if (playerAnimator != null)
                    {
                        playerAnimator.SetBool(
                            "IsMelee", true);
                        Invoke(nameof(ResetMelee), 0.6f);
                    }
                    return true;
                }
            }
        }
        return false;
    }

    void ResetMelee()
    {
        if (playerAnimator != null)
            playerAnimator.SetBool("IsMelee", false);
    }

    // ─────────────────────────────────────────
    // SHOOTING
    // ─────────────────────────────────────────

    void TryShoot()
    {
        if (!hasGun)
        {
            Debug.Log("No gun — pick one up");
            return;
        }

        if (bulletInAir)
        {
            Debug.Log("Bullet in air — wait");
            return;
        }

        if (currentBullets <= 0)
        {
            Debug.Log("Out of bullets");
            return;
        }

        if (bulletPrefab == null || gunPoint == null)
        {
            Debug.LogWarning("Missing bulletPrefab " +
                "or gunPoint");
            return;
        }

        // Spawn bullet
        GameObject bullet = Instantiate(
            bulletPrefab,
            gunPoint.position,
            Camera.main.transform.rotation);

        FrozenBullet fb =
            bullet.GetComponent<FrozenBullet>();
        if (fb != null)
            fb.SetShooter(this);

        bulletInAir = true;
        currentBullets--;

        // Shoot animation
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsShooting", true);
            Invoke(nameof(ResetShoot), 0.5f);
        }

        Debug.Log("Shot fired. Bullets left: "
            + currentBullets);
    }

    void ResetShoot()
    {
        if (playerAnimator != null)
            playerAnimator.SetBool("IsShooting", false);
    }

    // ─────────────────────────────────────────
    // BULLET DESTROYED
    // ─────────────────────────────────────────

    public void BulletDestroyed()
    {
        bulletInAir = false;
    }

    // ─────────────────────────────────────────
    // GUN PICKUP
    // ─────────────────────────────────────────

    public void PickUpGun()
    {
        hasGun = true;
        currentBullets = maxBullets;

        // Show gun mesh in hand
        if (gunInHand != null)
            gunInHand.SetActive(true);

        // Tell animator player now has gun
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("HasGun", true);

            // Play pickup animation
            playerAnimator.SetBool("IsPickingUp", true);
            Invoke(nameof(ResetPickup), 1f);
        }

        Debug.Log("Gun picked up");
    }

    void ResetPickup()
    {
        if (playerAnimator != null)
            playerAnimator.SetBool("IsPickingUp", false);
    }

    // ─────────────────────────────────────────
    // GETTERS
    // ─────────────────────────────────────────

    public bool HasGun() => hasGun;
    public int GetBullets() => currentBullets;
    public int GetMaxBullets() => maxBullets;
}

using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Bullet")]
    public GameObject bulletPrefab;
    public Transform gunPoint;

    [Header("Bullet Limit")]
    public int maxBullets = 3;
    private int currentBullets;
    private bool bulletInAir = false;

    [Header("Animation")]
    public Animator playerAnimator;

    [Header("Gun In Hand")]
    public GameObject gunInHand;

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

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("HasGun", false);
            playerAnimator.SetBool("IsShooting", false);
            playerAnimator.SetBool("IsDead", false);
            playerAnimator.SetBool("IsPickingUp", false);
            playerAnimator.SetFloat("Speed", 0f);
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
            TryShoot();
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
            Debug.LogWarning("Missing bulletPrefab or gunPoint");
            return;
        }

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

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsShooting", true);
            Invoke(nameof(ResetShoot), 0.5f);
        }

        Debug.Log("Shot fired. Bullets left: " + currentBullets);
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

        if (gunInHand != null)
            gunInHand.SetActive(true);

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("HasGun", true);
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
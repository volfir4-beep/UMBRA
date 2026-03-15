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
    // Melee fires when left click AND close to enemy
    // Kills enemy instantly — no bullet needed

    [Header("Gun State")]
    private bool hasGun = false;

    void Start()
    {
        currentBullets = maxBullets;
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            // Try melee first if close enough
            if (TryMelee()) return;

            // Otherwise shoot
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

        // Spherecast forward to find nearby enemies
        Collider[] hits = Physics.OverlapSphere(
            transform.position, meleeRange);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                // Found enemy in range — melee hit
                Security sec =
                    hit.GetComponent<Security>() ??
                    hit.GetComponentInParent<Security>();

                if (sec != null)
                {
                    sec.Die();
                    lastMeleeTime = Time.time;
                    Debug.Log("Melee hit!");
                    return true;
                }
            }
        }

        return false;
        // Returns false if no enemy in range
        // Shooting proceeds normally
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
            Debug.LogWarning("PlayerShooting: " +
                "Missing bulletPrefab or gunPoint");
            return;
        }

        // Spawn bullet
        GameObject bullet = Instantiate(
            bulletPrefab,
            gunPoint.position,
            Camera.main.transform.rotation);

        // Pass direct reference — no searching later
        FrozenBullet fb = bullet.GetComponent<FrozenBullet>();
        if (fb != null)
            fb.SetShooter(this);

        bulletInAir = true;
        currentBullets--;

        Debug.Log("Shot fired. Bullets left: " + currentBullets);
    }

    // ─────────────────────────────────────────
    // CALLED BY FROZENBULLET WHEN DESTROYED
    // ─────────────────────────────────────────

    public void BulletDestroyed()
    {
        bulletInAir = false;
        Debug.Log("Bullet destroyed — can fire again");
    }

    // ─────────────────────────────────────────
    // PICKUP
    // ─────────────────────────────────────────

    public void PickUpGun()
    {
        hasGun = true;
        currentBullets = maxBullets;
        Debug.Log("Gun picked up — bullets: " + currentBullets);
    }

    // ─────────────────────────────────────────
    // GETTERS — for HUD display
    // ─────────────────────────────────────────

    public bool HasGun() => hasGun;
    public int GetBullets() => currentBullets;
    public int GetMaxBullets() => maxBullets;
}
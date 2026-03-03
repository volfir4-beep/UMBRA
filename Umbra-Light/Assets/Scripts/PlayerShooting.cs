using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Bullet")]
    public GameObject bulletPrefab;
    public Transform gunPoint;

    private bool bulletInAir = false;
    private bool hasGun = false;

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
            TryShoot();
    }

    void TryShoot()
    {
        if (!hasGun) return;
        if (bulletInAir) return;

        GameObject bullet = Instantiate(
            bulletPrefab,
            gunPoint.position,
            Camera.main.transform.rotation
        );

        bulletInAir = true;
    }

    public void BulletDestroyed()
    {
        bulletInAir = false;
    }

    public void PickUpGun()
    {
        hasGun = true;
        Debug.Log("Gun picked up!");
    }

    public bool HasGun() => hasGun;
}
using UnityEngine;

public class GunPickup : MonoBehaviour
{
    [Header("Settings")]
    public float pickupRange = 2.5f;
    public KeyCode pickupKey = KeyCode.E;

    private Transform player;
    private PlayerShooting playerShooting;
    private bool pickedUp = false;

    void Start()
    {
        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;

            // PlayerShooting is on Player root object
            // Same object that has PlayerController
            // and PlayerDeath — all on Player root
            playerShooting =
                playerObj.GetComponent<PlayerShooting>();

            if (playerShooting == null)
                Debug.LogError("GunPickup: No PlayerShooting " +
                    "found on Player object");
        }
        else
        {
            Debug.LogError("GunPickup: Cannot find Player tag");
        }
    }

    void Update()
    {
        if (pickedUp) return;
        if (player == null) return;

        float dist = Vector3.Distance(
            transform.position, player.position);

        if (dist <= pickupRange &&
            Input.GetKeyDown(pickupKey))
        {
            PickUp();
        }
    }

    void PickUp()
    {
        if (playerShooting == null) return;

        pickedUp = true;

        // Calls PickUpGun() which already exists
        // in your PlayerShooting.cs
        // Sets hasGun = true
        // Sets currentBullets = maxBullets
        playerShooting.PickUpGun();

        Debug.Log("Gun picked up");
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
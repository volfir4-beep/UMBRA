using UnityEngine;

public class GunPickup : MonoBehaviour
{
    private bool playerInRange = false;
    private PlayerShooting playerShooting;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerShooting = other.GetComponent<PlayerShooting>();
            Debug.Log("Press E to pick up gun");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            playerShooting.PickUpGun();
            Destroy(gameObject);
        }
    }
}
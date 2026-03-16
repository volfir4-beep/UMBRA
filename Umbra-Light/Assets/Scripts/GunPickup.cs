using UnityEngine;
using TMPro;

public class GunPickup : MonoBehaviour
{
    [Header("Settings")]
    public float pickupRange = 2.5f;
    public float nudgeRange = 4.5f; // pickupRange + 2
    public KeyCode pickupKey = KeyCode.E;

    [Header("UI Reference")]
    public GameObject pickupMessage; 

    private Transform player;
    private PlayerShooting playerShooting;
    private bool pickedUp = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
            playerShooting = playerObj.GetComponent<PlayerShooting>();
        }
        
        if (pickupMessage != null) pickupMessage.SetActive(false);
    }

    void Update()
    {
        if (pickedUp || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 1. Check if we are close enough to PICK UP
        if (dist <= pickupRange)
        {
            UpdateUI("Press [" + pickupKey + "] to Pickup Gun");
            
            if (Input.GetKeyDown(pickupKey))
            {
                PickUp();
            }
        }
        // 2. Check if we are in the "NUDGE" zone
        else if (dist <= nudgeRange)
        {
            UpdateUI("Go near the gun to pick it up");
        }
        // 3. Completely out of range
        else
        {
            if (pickupMessage != null && pickupMessage.activeSelf)
            {
                pickupMessage.SetActive(false);
            }
        }
    }

    // Helper function to handle text updates and visibility
    void UpdateUI(string msg)
    {
        if (pickupMessage != null)
        {
            if (!pickupMessage.activeSelf) pickupMessage.SetActive(true);
            
            var text = pickupMessage.GetComponent<TextMeshProUGUI>();
            if (text != null) text.text = msg;
        }
    }

    void PickUp()
    {
        pickedUp = true;
        
        if (pickupMessage != null) pickupMessage.SetActive(false);

        if (playerShooting != null)
        {
            playerShooting.PickUpGun();
            Debug.Log("Gun successfully picked up!");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("Pickup failed: PlayerShooting component not found!");
        }
    }
}
using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [Header("Settings")]
    public float pickupRange = 2f;
    public KeyCode pickupKey = KeyCode.F;

    [Header("Key ID")]
    public string keyID = "DoorKey";

    private Transform player;
    private PlayerInventory playerInventory;
    private bool pickedUp = false;

    void Start()
    {
        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
            playerInventory =
                playerObj.GetComponent<PlayerInventory>();

            if (playerInventory == null)
                Debug.LogError("KeyPickup: PlayerInventory " +
                    "missing from Player. " +
                    "Click Player → Add Component → PlayerInventory");
        }
        else
        {
            Debug.LogError("KeyPickup: No object tagged Player");
        }
    }

    void Update()
    {
        if (pickedUp) return;
        if (player == null) return;

        float dist = Vector3.Distance(
            transform.position, player.position);

        // Show distance in console when close
        // So you can see exactly when range is met
        if (dist <= pickupRange * 2f)
        {
            Debug.Log("Key distance: " + dist.ToString("F2") +
                " / Range: " + pickupRange +
                " / Press: " + pickupKey);
        }

        if (dist <= pickupRange &&
            Input.GetKeyDown(pickupKey))
        {
            PickUp();
        }
    }

    void PickUp()
    {
        if (playerInventory == null)
        {
            Debug.LogError("KeyPickup: Cannot pick up — " +
                "PlayerInventory missing from Player");
            return;
        }

        pickedUp = true;
        playerInventory.AddKey(keyID);
        Debug.Log("Key picked up: " + keyID);
        Destroy(gameObject);
    }

    // Always visible in scene — not just when selected
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, pickupRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
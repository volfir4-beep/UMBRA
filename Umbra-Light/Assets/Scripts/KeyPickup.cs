using UnityEngine;
using TMPro;

public class KeyPickup : MonoBehaviour
{
    [Header("Settings")]
    public float pickupRange = 2f;
    public float nudgeRange = 4f; // pickupRange + 2
    public KeyCode pickupKey = KeyCode.F;

    [Header("Key ID")]
    public string keyID = "DoorKey";

    [Header("UI Reference")]
    public GameObject contextUI; 

    private Transform player;
    private PlayerInventory playerInventory;
    private bool pickedUp = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerInventory = playerObj.GetComponent<PlayerInventory>();
        }
        if (contextUI != null) contextUI.SetActive(false);
    }

    void Update()
    {
        if (pickedUp || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 1. Check if we are close enough to PICK UP
        if (dist <= pickupRange)
        {
            ShowMessage("Press [" + pickupKey + "] to pick up key");
            if (Input.GetKeyDown(pickupKey)) PickUp();
        }
        // 2. Check if we are close enough to be NUDGED
        else if (dist <= nudgeRange)
        {
            ShowMessage("Go near the key to pick it up");
        }
        // 3. Out of range
        else
        {
            if (contextUI != null && contextUI.activeSelf) contextUI.SetActive(false);
        }
    }

    void ShowMessage(string msg)
    {
        if (contextUI != null)
        {
            if (!contextUI.activeSelf) contextUI.SetActive(true);
            var text = contextUI.GetComponent<TextMeshProUGUI>();
            if (text != null) text.text = msg;
        }
    }

    void PickUp()
    {
        if (playerInventory == null) return;
        pickedUp = true;
        if (contextUI != null) contextUI.SetActive(false);
        playerInventory.AddKey(keyID);
        Destroy(gameObject);
    }
}
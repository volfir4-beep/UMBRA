using System.Collections;
using UnityEngine;
using TMPro; // Add this for UI

public class Door : MonoBehaviour
{
    [Header("Key Required")]
    public string requiredKeyID = "DoorKey";

    [Header("Interaction")]
    public float interactRange = 2.5f;
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Reference")]
    public GameObject contextUI; // Drag "ContextMessage" here

    [Header("Opening")]
    public float openAngle = 90f;
    public float openDuration = 0.8f;

    public enum OpenDirection { Left, Right }
    public OpenDirection openDirection = OpenDirection.Left;

    // Internal
    private Transform player;
    private PlayerInventory playerInventory;
    private bool isOpen = false;
    private bool isMoving = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
            playerInventory = playerObj.GetComponent<PlayerInventory>();
        }

        if (contextUI != null) contextUI.SetActive(false);

        closedRotation = transform.rotation;
        float angle = openDirection == OpenDirection.Left ? -openAngle : openAngle;
        openRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
    }

    void Update()
    {
        if (isOpen || isMoving || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= interactRange)
        {
            // Handle UI Messaging
            if (playerInventory != null && playerInventory.HasKey(requiredKeyID))
            {
                UpdateUI("Unlock the door by pressing [" + interactKey + "]");
                
                if (Input.GetKeyDown(interactKey))
                {
                    TryOpen();
                }
            }
            else
            {
                UpdateUI("You don't have the key for this");
            }
        }
        else
        {
            // Hide UI when out of range
            if (contextUI != null && contextUI.activeSelf)
            {
                contextUI.SetActive(false);
            }
        }
    }

    // Helper function to update the text safely
    void UpdateUI(string msg)
    {
        if (contextUI != null)
        {
            if (!contextUI.activeSelf) contextUI.SetActive(true);
            var text = contextUI.GetComponent<TextMeshProUGUI>();
            if (text != null) text.text = msg;
        }
    }

    void TryOpen()
    {
        if (playerInventory == null) return;

        if (playerInventory.HasKey(requiredKeyID))
        {
            playerInventory.RemoveKey(requiredKeyID);
            
            // Hide UI as soon as interaction begins
            if (contextUI != null) contextUI.SetActive(false);
            
            StartCoroutine(OpenDoor());
        }
    }

    IEnumerator OpenDoor()
    {
        isMoving = true;
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float smoothT = t * t * (3f - 2f * t);
            transform.rotation = Quaternion.Lerp(startRot, openRotation, smoothT);
            yield return null;
        }

        transform.rotation = openRotation;
        isOpen = true;
        isMoving = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
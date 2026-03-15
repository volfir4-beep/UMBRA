using NUnit.Framework.Internal;
using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Key Required")]
    public string requiredKeyID = "DoorKey";
    // Must exactly match keyID in KeyPickup script
    // Both default to DoorKey — change both if needed

    [Header("Interaction")]
    public float interactRange = 2.5f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Opening")]
    public float openAngle = 90f;
    public float openDuration = 0.8f;
    // Seconds for door to complete rotation
    // Higher = slower, more dramatic

    public enum OpenDirection { Left, Right }
    public OpenDirection openDirection = OpenDirection.Left;
    // If door opens wrong way — switch this in Inspector

    // Internal
    private Transform player;
    private PlayerInventory playerInventory;
    private bool isOpen = false;
    private bool isMoving = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;

            // Gets PlayerInventory from Player root
            // Same object as PlayerShooting, PlayerDeath
            playerInventory =
                playerObj.GetComponent<PlayerInventory>();

            if (playerInventory == null)
                Debug.LogError("Door: No PlayerInventory " +
                    "on Player — Add Component → PlayerInventory");
        }
        else
        {
            Debug.LogError("Door: Cannot find Player tag");
        }

        // Store rotations at start
        // so we know exactly where closed and open are
        closedRotation = transform.rotation;

        float angle = openDirection == OpenDirection.Left
            ? -openAngle : openAngle;

        openRotation = closedRotation *
            Quaternion.Euler(0f, angle, 0f);
    }

    void Update()
    {
        // Already open or moving — ignore input
        if (isOpen || isMoving) return;
        if (player == null) return;

        float dist = Vector3.Distance(
            transform.position, player.position);

        if (dist <= interactRange &&
            Input.GetKeyDown(interactKey))
        {
            TryOpen();
        }
    }

    void TryOpen()
    {
        if (playerInventory == null) return;

        if (playerInventory.HasKey(requiredKeyID))
        {
            // Has correct key — open door
            // Remove key so it cant be reused
            // Delete next line if key should stay in inventory
            playerInventory.RemoveKey(requiredKeyID);

            StartCoroutine(OpenDoor());
        }
        else
        {
            // No key — tell player
            Debug.Log("This door is locked. Need: " +
                requiredKeyID);

            // Later replace Debug.Log with UI popup text
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

            // 0 to 1 progress
            float t = Mathf.Clamp01(elapsed / openDuration);

            // Smooth ease in/out
            // Makes door feel heavy not robotic
            float smoothT = t * t * (3f - 2f * t);

            transform.rotation = Quaternion.Lerp(
                startRot, openRotation, smoothT);

            yield return null;
        }

        // Snap to exact final position
        transform.rotation = openRotation;
        isOpen = true;
        isMoving = false;

        Debug.Log("Door open");
    }

    void OnDrawGizmosSelected()
    {
        // Green sphere shows interact range in scene view
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            transform.position, interactRange);
    }
}

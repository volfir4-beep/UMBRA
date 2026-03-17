using UnityEngine;

public class CaneHitbox : MonoBehaviour
{
    // Attached to the Cane mesh object
    // Collider is disabled by default
    // Gentleman enables it during hit animation only

    private Collider caneCollider;
    private bool canHit = false;

    void Start()
    {
        caneCollider = GetComponent<Collider>();

        // Always disabled at start
        // Only enabled mid-swing
        if (caneCollider != null)
            caneCollider.enabled = false;
    }

    // Called by Gentleman when swing starts
    public void EnableHitbox()
    {
        canHit = true;
        if (caneCollider != null)
            caneCollider.enabled = true;
    }

    // Called by Gentleman when swing ends
    public void DisableHitbox()
    {
        canHit = false;
        if (caneCollider != null)
            caneCollider.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!canHit) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Cane hit player!");

            PlayerDeath pd =
                other.GetComponent<PlayerDeath>() ??
                other.GetComponentInParent<PlayerDeath>();

            if (pd != null)
                pd.Die();

            // Disable immediately so only hits once per swing
            DisableHitbox();
        }
    }
}
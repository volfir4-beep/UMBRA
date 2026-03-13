using UnityEngine;

public class LightExposureCalculator : MonoBehaviour
{
    [Header("Output")]
    public float lightExposure; // 0 = shadow, 1 = light

    [Header("Settings")]
    public LayerMask wallLayers; // Set to Environment layer
    public float exposureSmoothing = 8f; // How fast exposure transitions

    // All torches register themselves here
    private System.Collections.Generic.List<TorchLight> allTorches
        = new System.Collections.Generic.List<TorchLight>();

    void Update()
    {
        float targetExposure = CalculateExposure();

        // Smooth transition instead of instant snap
        lightExposure = Mathf.Lerp(
            lightExposure,
            targetExposure,
            Time.deltaTime * exposureSmoothing);
    }

    float CalculateExposure()
    {
        Vector3 playerPos = transform.position + Vector3.up;

        foreach (TorchLight torch in allTorches)
        {
            if (torch == null) continue;
            if (!torch.isActiveAndEnabled) continue;

            // Check 1 — is player inside this torch sphere?
            float dist = Vector3.Distance(
                playerPos, torch.transform.position);

            if (dist > torch.GetRadius()) continue;

            // Check 2 — is there a wall between torch and player?
            Vector3 direction = playerPos - torch.transform.position;

            bool wallBlocking = Physics.Raycast(
                torch.transform.position,
                direction.normalized,
                direction.magnitude,
                wallLayers);

            if (!wallBlocking)
            {
                // Player is inside torch sphere with no wall blocking
                // = player is in light
                return 1f;
            }
        }

        // No torch reached player
        return 0f;
    }

    // Torches call this when they spawn
    public void RegisterTorch(TorchLight torch)
    {
        if (!allTorches.Contains(torch))
            allTorches.Add(torch);
    }

    public void UnregisterTorch(TorchLight torch)
    {
        allTorches.Remove(torch);
    }
}
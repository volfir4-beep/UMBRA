using UnityEngine;

public class LightExposureCalculator : MonoBehaviour
{
    [Header("Output")]
    public float lightExposure;

    [Header("Settings")]
    public LayerMask wallLayers;
    public float exposureSmoothing = 8f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private Light[] allLights;

    void Start()
    {
        RefreshLights();
    }

    void Update()
    {
        float target = CalculateExposure();

        lightExposure = Mathf.Lerp(
            lightExposure,
            target,
            Time.deltaTime * exposureSmoothing);
    }

    float CalculateExposure()
    {
        Vector3 playerPos =
            transform.position + Vector3.up * 0.9f;

        foreach (Light light in allLights)
        {
            if (light == null) continue;
            if (!light.enabled) continue;
            if (!light.gameObject.activeInHierarchy) continue;
            if (light.intensity < 0.05f) continue;

            if (!IsPlayerInLightRange(light, playerPos))
                continue;

            Vector3 lightPos = light.transform.position;
            Vector3 direction = playerPos - lightPos;
            float distance = direction.magnitude;

            bool wallBlocking = Physics.Raycast(
                lightPos,
                direction.normalized,
                distance,
                wallLayers);

            if (!wallBlocking)
            {
                if (showDebugLogs)
                    Debug.Log("In light: " +
                        light.gameObject.name);
                return 1f;
            }
        }

        return 0f;
    }

    bool IsPlayerInLightRange(Light light, Vector3 playerPos)
    {
        float dist = Vector3.Distance(
            light.transform.position, playerPos);

        if (light.type == LightType.Directional)
            return true;

        if (light.type == LightType.Point)
            return dist <= light.range;

        if (light.type == LightType.Spot)
        {
            if (dist > light.range) return false;

            Vector3 toPlayer =
                (playerPos - light.transform.position).normalized;

            float angle = Vector3.Angle(
                light.transform.forward, toPlayer);

            return angle <= light.spotAngle / 2f;
        }

        if (light.type == LightType.Rectangle)
            return dist <= light.range;

        return false;
    }

    // Still here for Watcher sweeping light if needed
    public void RefreshLights()
    {
        allLights = FindObjectsByType<Light>(
            FindObjectsSortMode.None);

        if (showDebugLogs)
            Debug.Log("Found " + allLights.Length + " lights");
    }
}
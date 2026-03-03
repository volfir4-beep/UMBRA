using UnityEngine;

public class LightExposureCalculator : MonoBehaviour
{
    public float lightExposure;
    public LayerMask shadowCasterLayers;

    private Light[] allLights;

    private Vector3[] rayOffsets = new Vector3[]
    {
        new Vector3(0, 0.1f, 0),
        new Vector3(0, 0.5f, 0),
        new Vector3(0, 1.0f, 0),
        new Vector3(0, 1.5f, 0),
        new Vector3(0, 1.8f, 0),
        new Vector3(0.3f, 1.0f, 0),
        new Vector3(-0.3f, 1.0f, 0),
        new Vector3(0, 1.0f, 0.3f)
    };

    void Start()
    {
        allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
    }

    void Update()
    {
        CalculateExposure();
    }

    void CalculateExposure()
    {
        int hitRays = 0;
        int totalRays = 0;

        foreach (Light light in allLights)
        {
            if (!light.enabled) continue;
            if (light.intensity < 0.05f) continue;

            foreach (Vector3 offset in rayOffsets)
            {
                Vector3 rayStart = transform.position + offset;
                Vector3 toLight = light.transform.position - rayStart;
                float dist = toLight.magnitude;

                totalRays++;

                if (dist > light.range) continue;

                bool blocked = Physics.Raycast(
                    rayStart,
                    toLight.normalized,
                    dist,
                    shadowCasterLayers
                );

                if (!blocked)
                {
                    float brightness = (1f - dist / light.range) * light.intensity;
                    if (brightness > 0.1f) hitRays++;
                }
            }
        }

        float raw = totalRays > 0 ? (float)hitRays / totalRays : 0f;
        lightExposure = Mathf.Lerp(lightExposure, raw, Time.deltaTime * 10f);
    }

    public void RefreshLights()
    {
        allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
    }
}
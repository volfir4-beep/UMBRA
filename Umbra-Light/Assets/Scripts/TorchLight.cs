using UnityEngine;

public class TorchLight : MonoBehaviour
{
    [Header("Light Settings")]
    public float baseIntensity = 2.5f;
    public float baseRange = 10f;

    [Header("Flicker")]
    public float flickerSpeed = 3f;
    public float intensityVariance = 0.4f;
    public float radiusVariance = 0.08f;

    private Light torchLight;
    private float seed;

    void Start()
    {
        torchLight = GetComponent<Light>();
        seed = Random.Range(0f, 100f);

        if (torchLight != null)
        {
            torchLight.intensity = baseIntensity;
            torchLight.range = baseRange;
        }
    }

    void Update()
    {
        if (torchLight == null) return;

        float noise = Mathf.PerlinNoise(
            Time.time * flickerSpeed + seed, 0f);

        float offset = (noise - 0.5f) * 2f;

        torchLight.intensity = Mathf.Max(
            baseIntensity + offset * intensityVariance, 0.3f);

        torchLight.range = Mathf.Max(
            baseRange + offset * radiusVariance * baseRange, 3f);
    }
}
using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    private Light torchLight;

    public float baseIntensity = 2.5f;
    public float baseRange = 10f;
    public float intensityVariance = 0.4f;
    public float flickerSpeed = 3f;

    private float seed;

    void Start()
    {
        torchLight = GetComponent<Light>();
        seed = Random.Range(0f, 100f);
        torchLight.intensity = baseIntensity;
        torchLight.range = baseRange;
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(
            Time.time * flickerSpeed + seed, 0f);

        float offset = (noise - 0.5f) * 2f;

        torchLight.intensity = Mathf.Max(
            baseIntensity + offset * intensityVariance, 0.5f);

        torchLight.range = Mathf.Max(
            baseRange + offset * 0.15f * baseRange, 3f);
    }
}
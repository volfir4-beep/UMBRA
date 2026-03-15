using UnityEngine;

public class FrozenParticle : MonoBehaviour
{
    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        if (ps == null)
            Debug.LogWarning("FrozenParticle: " +
                "No ParticleSystem found on " +
                gameObject.name);
    }

    void Update()
    {
        if (ps == null) return;
        if (WorldTimeController.Instance == null) return;

        float timeScale =
            WorldTimeController.Instance.worldTimeScale;

        // Scale particle simulation speed with world time
        // 0 = completely frozen
        // 1 = full speed
        var main = ps.main;
        main.simulationSpeed = Mathf.Max(timeScale, 0f);

        // Also pause emission when fully frozen
        // Prevents new particles spawning mid-freeze
        var emission = ps.emission;
        emission.enabled = timeScale > 0.02f;
    }
}
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.DebugUI.Table;

public class LightSwitch : MonoBehaviour
{
    [Header("Lights To Control")]
    public Light[] connectedLights;
    // Drag up to 4 lights into this array in Inspector

    [Header("Settings")]
    public float interactRange = 2f;
    public KeyCode interactKey = KeyCode.Q;

    [Header("State")]
    public bool lightsOnAtStart = true;

    private Transform player;
    private bool lightsOn;

    void Start()
    {
        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Set initial state
        lightsOn = lightsOnAtStart;
        SetLights(lightsOn);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(
            transform.position, player.position);

        if (dist <= interactRange &&
            Input.GetKeyDown(interactKey))
        {
            ToggleLights();
        }
    }

    void ToggleLights()
    {
        lightsOn = !lightsOn;
        SetLights(lightsOn);

        Debug.Log("Switch toggled — lights " +
            (lightsOn ? "ON" : "OFF"));

        // Tell LightExposureCalculator lights changed
        LightExposureCalculator calc =
            FindFirstObjectByType<LightExposureCalculator>();
        calc?.RefreshLights();
    }

    void SetLights(bool state)
    {
        foreach (Light light in connectedLights)
        {
            if (light != null)
                light.enabled = state;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position, interactRange);
    }
}

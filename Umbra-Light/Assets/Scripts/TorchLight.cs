using NUnit.Framework;
using System.ComponentModel;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.UI;

public class TorchLight : MonoBehaviour
{
    [Header("Light Settings")]
    public float baseRadius = 10f;
    public float baseIntensity = 2.5f;

    [Header("Flicker")]
    public float flickerSpeed = 3f;
    public float intensityVariance = 0.4f;
    public float radiusVariance = 0.08f;

    [Header("Zone Visualization")]
    public bool showZone = true;
    public Color safeZoneColor = new Color(0.8f, 0.3f, 0f, 0.15f);
    public Color dangerZoneColor = new Color(1f, 0.6f, 0f, 0.25f);

    private Light torchLight;
    private LightExposureCalculator lightCalc;
    private float seed;
    private GameObject zoneVisual;
    private Renderer zoneRenderer;

    void Start()
    {
        torchLight = GetComponent<Light>();
        seed = Random.Range(0f, 100f);

        // Set initial light values
        torchLight.range = baseRadius;
        torchLight.intensity = baseIntensity;

        // Find and register with calculator
        lightCalc = FindFirstObjectByType<LightExposureCalculator>();
        if (lightCalc != null)
            lightCalc.RegisterTorch(this);
        else
            Debug.LogWarning("TorchLight: No LightExposureCalculator found!");

        // Create zone visual
        if (showZone) CreateZoneVisual();
    }

    void OnDestroy()
    {
        lightCalc?.UnregisterTorch(this);
        if (zoneVisual != null) Destroy(zoneVisual);
    }

    void Update()
    {
        HandleFlicker();
        UpdateZoneVisual();
    }

    void HandleFlicker()
    {
        float noise = Mathf.PerlinNoise(
            Time.time * flickerSpeed + seed, 0f);

        float offset = (noise - 0.5f) * 2f;

        torchLight.intensity = Mathf.Max(
            baseIntensity + offset * intensityVariance, 0.3f);

        torchLight.range = Mathf.Max(
            baseRadius + offset * radiusVariance * baseRadius, 3f);
    }

    void CreateZoneVisual()
    {
        // Create a flat cylinder on the floor to show light zone
        zoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zoneVisual.name = "LightZone_" + gameObject.name;

        // Remove collider — it's just visual
        Destroy(zoneVisual.GetComponent<Collider>());

        // Position flat on floor below torch
        zoneVisual.transform.position = new Vector3(
            transform.position.x,
            0.05f, // Just above floor
            transform.position.z);

        // Scale to match light radius
        // Cylinder default is 1 unit radius, 1 unit height
        // We want flat (0.05 height) and radius-sized
        float diameter = baseRadius * 2f;
        zoneVisual.transform.localScale = new Vector3(
            diameter, 0.05f, diameter);

        // Apply material
        zoneRenderer = zoneVisual.GetComponent<Renderer>();
        Material mat = new Material(
            Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = dangerZoneColor;

        // Make transparent
        mat.SetFloat("_Surface", 1); // 0=opaque, 1=transparent
        mat.SetFloat("_Blend", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;

        zoneRenderer.material = mat;
        zoneRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        zoneRenderer.receiveShadows = false;
    }

    void UpdateZoneVisual()
    {
        if (zoneVisual == null) return;

        // Scale zone visual with current light radius (flicker effect)
        float currentDiameter = torchLight.range * 2f;
        zoneVisual.transform.localScale = new Vector3(
            currentDiameter, 0.05f, currentDiameter);

        // Check if player is inside — change color
        LightExposureCalculator calc =
            FindFirstObjectByType<LightExposureCalculator>();

        if (calc != null && calc.lightExposure > 0.5f)
        {
            zoneRenderer.material.color = dangerZoneColor;
        }
        else
        {
            zoneRenderer.material.color = safeZoneColor;
        }
    }

    // Called by LightExposureCalculator to get current radius
    public float GetRadius()
    {
        return torchLight != null ? torchLight.range : baseRadius;
    }

    // Enable/disable zone visualization
    public void SetZoneVisible(bool visible)
    {
        if (zoneVisual != null)
            zoneVisual.SetActive(visible);
    }
}

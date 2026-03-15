using UnityEngine;
using System.Collections.Generic;

public class WorldTimeController : MonoBehaviour
{
    public static WorldTimeController Instance;

    public LightExposureCalculator playerExposure;
    public float worldTimeScale;

    private List<Security> allEnemies = new List<Security>();
    private List<FlareGunner> allFlareGunners
        = new List<FlareGunner>();
    private List<RotatingMechanism> allMechanisms
        = new List<RotatingMechanism>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        worldTimeScale = playerExposure.lightExposure;

        foreach (Security e in allEnemies)
            if (e != null) e.SetTimeScale(worldTimeScale);

        foreach (FlareGunner fg in allFlareGunners)
            if (fg != null) fg.SetTimeScale(worldTimeScale);

        foreach (RotatingMechanism m in allMechanisms)
            if (m != null) m.SetTimeScale(worldTimeScale);
    }

    // Security registration
    public void RegisterEnemy(Security e) => allEnemies.Add(e);
    public void UnregisterEnemy(Security e) => allEnemies.Remove(e);

    // FlareGunner registration
    public void RegisterFlareGunner(FlareGunner f)
        => allFlareGunners.Add(f);
    public void UnregisterFlareGunner(FlareGunner f)
        => allFlareGunners.Remove(f);

    // Mechanism registration
    public void RegisterMechanism(RotatingMechanism m)
        => allMechanisms.Add(m);
}
using UnityEngine;
using System.Collections.Generic;

public class WorldTimeController : MonoBehaviour
{
    public static WorldTimeController Instance;

    public LightExposureCalculator playerExposure;
    public float worldTimeScale;

    private List<Security> allEnemies =
        new List<Security>();
    private List<SentinelGuard> allSentinels =
        new List<SentinelGuard>();
    private List<Gentleman> allGentlemen =
        new List<Gentleman>();
    private List<FlareGunner> allFlareGunners =
        new List<FlareGunner>();
    private List<RotatingMechanism> allMechanisms =
        new List<RotatingMechanism>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        worldTimeScale = playerExposure.lightExposure;

        foreach (Security e in allEnemies)
            if (e != null) e.SetTimeScale(worldTimeScale);

        foreach (SentinelGuard s in allSentinels)
            if (s != null) s.SetTimeScale(worldTimeScale);

        foreach (Gentleman g in allGentlemen)
            if (g != null) g.SetTimeScale(worldTimeScale);

        foreach (FlareGunner fg in allFlareGunners)
            if (fg != null) fg.SetTimeScale(worldTimeScale);

        foreach (RotatingMechanism m in allMechanisms)
            if (m != null) m.SetTimeScale(worldTimeScale);
    }

    public void RegisterEnemy(Security e)
        => allEnemies.Add(e);
    public void UnregisterEnemy(Security e)
        => allEnemies.Remove(e);

    public void RegisterSentinel(SentinelGuard s)
        => allSentinels.Add(s);
    public void UnregisterSentinel(SentinelGuard s)
        => allSentinels.Remove(s);

    public void RegisterGentleman(Gentleman g)
    => allGentlemen.Add(g);
    public void UnregisterGentleman(Gentleman g)
        => allGentlemen.Remove(g);

    public void RegisterFlareGunner(FlareGunner f)
        => allFlareGunners.Add(f);
    public void UnregisterFlareGunner(FlareGunner f)
        => allFlareGunners.Remove(f);

    public void RegisterMechanism(RotatingMechanism m)
        => allMechanisms.Add(m);
}
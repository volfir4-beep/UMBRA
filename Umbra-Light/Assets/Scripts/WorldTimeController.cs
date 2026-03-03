using UnityEngine;
using System.Collections.Generic;

public class WorldTimeController : MonoBehaviour
{
    public static WorldTimeController Instance;
    public LightExposureCalculator playerExposure;
    public float worldTimeScale;

    private List<Enemy> allEnemies = new List<Enemy>();
    private List<RotatingMechanism> allMechanisms = new List<RotatingMechanism>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        worldTimeScale = playerExposure.lightExposure;

        foreach (Enemy e in allEnemies)
            if (e != null) e.SetTimeScale(worldTimeScale);

        foreach (RotatingMechanism m in allMechanisms)
            if (m != null) m.SetTimeScale(worldTimeScale);
    }

    public void RegisterEnemy(Enemy e) => allEnemies.Add(e);
    public void UnregisterEnemy(Enemy e) => allEnemies.Remove(e);
    public void RegisterMechanism(RotatingMechanism m) => allMechanisms.Add(m);
}
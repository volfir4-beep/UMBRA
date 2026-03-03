using UnityEngine;

public class RotatingMechanism : MonoBehaviour
{
    public Vector3 rotationAxis = Vector3.up;
    public float baseRotationSpeed = 40f;

    private float currentTimeScale = 1f;

    void Start()
    {
        WorldTimeController.Instance.RegisterMechanism(this);
    }

    void Update()
    {
        transform.Rotate(rotationAxis,
            baseRotationSpeed * currentTimeScale * Time.deltaTime);
    }

    public void SetTimeScale(float scale)
    {
        currentTimeScale = scale;
    }
}
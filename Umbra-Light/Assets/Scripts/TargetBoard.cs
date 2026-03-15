using UnityEngine;
using System.Collections;

public class TargetBoard : MonoBehaviour
{
    [Header("Break Settings")]
    public float breakForce = 3f;
    // How far pieces fly when shot

    public float destroyDelay = 2f;
    // Seconds before pieces disappear

    [Header("Optional")]
    public GameObject hitParticle;
    // Drag a particle prefab here if you have one
    // Leave empty — works fine without it

    private bool isDestroyed = false;

    // ─────────────────────────────────────────
    // Called by FrozenBullet raycast when hit
    // ─────────────────────────────────────────

    public void GetShot()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        StartCoroutine(BreakApart());
    }

    IEnumerator BreakApart()
    {
        // Spawn hit particle if assigned
        if (hitParticle != null)
            Instantiate(hitParticle,
                transform.position, Quaternion.identity);

        // Add rigidbody to every child piece
        // Makes them fly apart physically
        foreach (Transform child in transform)
        {
            Rigidbody childRb =
                child.gameObject.AddComponent<Rigidbody>();

            // Random direction for each piece
            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1.5f),
                Random.Range(-0.5f, 0.5f));

            childRb.AddForce(
                randomDir * breakForce,
                ForceMode.Impulse);

            // Random spin
            childRb.AddTorque(
                Random.insideUnitSphere * breakForce,
                ForceMode.Impulse);
        }

        // Also add physics to root if it has a renderer
        Rigidbody rootRb = GetComponent<Rigidbody>();
        if (rootRb == null)
            rootRb = gameObject.AddComponent<Rigidbody>();

        rootRb.AddForce(
            new Vector3(0f, 1f, Random.Range(-0.5f, 0.5f))
            * breakForce, ForceMode.Impulse);

        // Wait then destroy everything
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}
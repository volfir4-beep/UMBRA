using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerDeath : MonoBehaviour
{
    private bool isDead = false;
    private LightExposureCalculator lightCalc;

    void Start()
    {
        lightCalc = GetComponent<LightExposureCalculator>();

        if (lightCalc == null)
            Debug.LogError("PlayerDeath: No " +
                "LightExposureCalculator on Player");
    }

    public void Die()
    {
        if (isDead) return;

        // Null check before accessing
        if (lightCalc != null &&
            lightCalc.lightExposure < 0.05f)
        {
            Debug.Log("In shadow — protected.");
            return;
        }

        isDead = true;
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        Debug.Log("Player died");

        // Trigger death animation
        Animator anim =
            GetComponentInChildren<Animator>();
        if (anim != null)
            anim.SetBool("IsDead", true);

        // Disable movement
        // Gets component directly — no stored reference needed
        PlayerController pc =
            GetComponent<PlayerController>();
        if (pc != null)
            pc.enabled = false;

        // Disable shooting
        PlayerShooting ps =
            GetComponent<PlayerShooting>();
        if (ps != null)
            ps.enabled = false;

        // Wait for death animation
        yield return new WaitForSecondsRealtime(2f);

        // Reload current scene — restart
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name);
    }
}

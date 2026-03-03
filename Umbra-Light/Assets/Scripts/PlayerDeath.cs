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
    }

    public void Die()
    {
        if (isDead) return;

        if (lightCalc.lightExposure < 0.05f)
        {
            Debug.Log("In shadow — protected.");
            return;
        }

        isDead = true;
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        Debug.Log("Player died — restarting.");
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
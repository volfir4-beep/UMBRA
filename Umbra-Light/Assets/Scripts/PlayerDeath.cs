using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerDeath : MonoBehaviour
{
    private bool isDead = false;
    private LightExposureCalculator lightCalc;

    [Header("Death Camera")]
    public Transform cameraHolder;
    public float deathTiltAngle = 80f;
    // How far camera tilts on death — 80 = almost flat on ground
    public float deathDropAmount = 1.2f;
    // How far camera drops down
    public float deathAnimSpeed = 1.5f;
    // How fast the death animation plays

    void Start()
    {
        lightCalc = GetComponent<LightExposureCalculator>();

        // Auto find camera holder if not assigned
        if (cameraHolder == null)
        {
            PlayerController pc =
                GetComponent<PlayerController>();
            if (pc != null)
                cameraHolder = pc.cameraHolder;
        }
    }

    public void Die()
    {
        if (isDead) return;

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

        // Disable movement immediately
        PlayerController pc =
            GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        PlayerShooting ps =
            GetComponent<PlayerShooting>();
        if (ps != null) ps.enabled = false;

        // Death animation
        if (cameraHolder != null)
            StartCoroutine(DeathCameraAnim());

        // Wait for camera animation + red screen
        yield return new WaitForSecondsRealtime(2.5f);

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name);
    }

    IEnumerator DeathCameraAnim()
    {
        Vector3 startPos = cameraHolder.localPosition;
        Quaternion startRot = cameraHolder.localRotation;

        // Target — tilt sideways and drop
        Vector3 targetPos = startPos +
            Vector3.down * deathDropAmount;
        Quaternion targetRot = Quaternion.Euler(
            startRot.eulerAngles.x,
            startRot.eulerAngles.y,
            deathTiltAngle);
        // Z rotation = sideways tilt = falling over

        float elapsed = 0f;
        float duration = 1.5f / deathAnimSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            // Use unscaled so death plays even if timescale changes
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease in — starts slow then speeds up
            // Like losing consciousness
            float easedT = t * t;

            cameraHolder.localPosition = Vector3.Lerp(
                startPos, targetPos, easedT);

            cameraHolder.localRotation = Quaternion.Slerp(
                startRot, targetRot, easedT);

            yield return null;
        }
    }

    void OnGUI()
    {
        if (!isDead) return;

        // Red overlay that fades in on death
        float alpha = Mathf.Clamp01(
            (Time.realtimeSinceStartup - (Time.realtimeSinceStartup - 2.5f))
            * 0.4f);

        // Simpler — just show red overlay while dead
        GUI.color = new Color(0.8f, 0f, 0f, 0.5f);
        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}

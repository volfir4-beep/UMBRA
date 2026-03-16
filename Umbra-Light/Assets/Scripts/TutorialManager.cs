using UnityEngine;
using TMPro;
using System.Collections; // Required for Coroutines

public class TutorialManager : MonoBehaviour
{
    public TextMeshProUGUI tutorialText;
    public CanvasGroup canvasGroup; // Drag your TutorialText here
    public float fadeSpeed = 2f;
    public float stayDuration = 1.5f; // How long it stays after completion

    private int currentStep = 0;
    private float mouseMoveAmount = 0;
    private bool isTransitioning = false;

    void Start()
    {
        canvasGroup.alpha = 0; // Start invisible
        StartCoroutine(NewStep(0)); 
    }

    void Update()
    {
        if (isTransitioning) return;

        switch (currentStep)
        {
            case 0: // Look around
                mouseMoveAmount += Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y"));
                if (mouseMoveAmount > 100f) StartCoroutine(CompleteStep());
                break;

            case 1: // W and S
                if (Input.GetAxisRaw("Vertical") != 0) StartCoroutine(CompleteStep());
                break;

            case 2: // A and D
                if (Input.GetAxisRaw("Horizontal") != 0) StartCoroutine(CompleteStep());
                break;
        }
    }

    IEnumerator CompleteStep()
    {
        isTransitioning = true;
        
        // 1. Wait a bit so the player sees the "success"
        yield return new WaitForSeconds(stayDuration);

        // 2. Fade Out
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        currentStep++;
        if (currentStep <= 2)
        {
            yield return StartCoroutine(NewStep(currentStep));
        }
        else
        {
            this.enabled = false;
        }
    }

    IEnumerator NewStep(int stepIndex)
    {
        string[] messages = {
            "Use mouse to look around",
            "Press W to go forward\nPress S to go backward",
            "Press A to go left\nPress D to go right"
        };
        
        tutorialText.text = messages[stepIndex];

        // Fade In
        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        
        isTransitioning = false;
    }
}
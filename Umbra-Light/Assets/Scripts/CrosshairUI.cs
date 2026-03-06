using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    private Canvas canvas;
    private Image horizontal;
    private Image vertical;
    private Image dot;

    public Color crosshairColor = Color.white;
    public float crosshairSize = 20f;
    public float crosshairThickness = 2f;
    public float gapSize = 5f;

    void Start()
    {
        BuildCrosshair();
    }

    void BuildCrosshair()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("CrosshairCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Center dot
        dot = CreateImage("Dot", canvasObj.transform,
            crosshairThickness * 1.5f, crosshairThickness * 1.5f,
            Vector2.zero);

        // Horizontal line (left + right)
        horizontal = CreateImage("Horizontal", canvasObj.transform,
            crosshairSize, crosshairThickness,
            Vector2.zero);

        // Vertical line (up + down)
        vertical = CreateImage("Vertical", canvasObj.transform,
            crosshairThickness, crosshairSize,
            Vector2.zero);
    }

    Image CreateImage(string name, Transform parent,
        float width, float height, Vector2 position)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = position;

        Image img = obj.AddComponent<Image>();
        img.color = crosshairColor;

        return img;
    }
}
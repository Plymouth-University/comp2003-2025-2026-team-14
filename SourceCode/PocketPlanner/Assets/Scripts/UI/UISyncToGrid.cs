using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UISyncToGrid : MonoBehaviour
{
    public Tilemap boardTilemap; // Drag your grid here
    public Camera mainCamera;    // Drag your Main Camera here
    private RectTransform rectTransform;
    [Header("Specify which UI panel this is for: 0 = Bottom, 1 = Top")]
    [SerializeField] private int panelType = 0;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // We use LateUpdate so the Camera and Grid have finished moving first
    void LateUpdate()
    {
        if (boardTilemap == null || mainCamera == null || rectTransform == null)
        {
            Debug.LogWarning("UISyncToGrid: Missing references. Please assign the Tilemap, Camera, and ensure this GameObject has a RectTransform.");
            return;
        }

        if (panelType == 0)
            BottomUI();
        else
            TopUI();
    }

    void BottomUI()
    {
        // 1. Find the World Position of the bottom edge of the grid
        // LocalBounds.min.y is the very bottom of the generated tiles
        float bottomWorldY = boardTilemap.localBounds.min.y;
        Vector3 bottomEdgeWorld = new Vector3(boardTilemap.localBounds.center.x, bottomWorldY, 0);

        // 2. Convert that "World" position into "Screen" pixels
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(bottomEdgeWorld);

        // 3. Set the height of this panel to match that pixel value
        // Since the Canvas Scaler is active, we divide by the scale factor
        float canvasScale = transform.root.GetComponent<Canvas>().scaleFactor;
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, screenPoint.y / canvasScale);
    }

    void TopUI()
    {
        // 1. Find the World Position of the top edge of the grid
        // LocalBounds.max.y is the very top of the generated tiles
        float topWorldY = boardTilemap.localBounds.max.y;
        Vector3 topEdgeWorld = new Vector3(boardTilemap.localBounds.center.x, topWorldY, 0);

        // 2. Convert that "World" position into "Screen" pixels
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(topEdgeWorld);

        // 3. Set the height of this panel to match that pixel value
        // Since the Canvas Scaler is active, we divide by the scale factor
        float canvasScale = transform.root.GetComponent<Canvas>().scaleFactor;
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, (Screen.height - screenPoint.y) / canvasScale);
    }
}
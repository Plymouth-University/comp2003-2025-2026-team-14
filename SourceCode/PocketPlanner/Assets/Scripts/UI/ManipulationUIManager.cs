using UnityEngine;
using UnityEngine.UI;

public class ManipulationUIManager : MonoBehaviour
{
    [Header("Manipulation Panel References")]
    [SerializeField] private GameObject manipulationPanel;
    [SerializeField] private GameObject RotateButton;
    [SerializeField] private GameObject FlipButton;
    [SerializeField] private GameObject ConfirmButton;

    [Header("Shape Manager Reference")]
    [SerializeField] private ShapeManager shapeManager;

    [Header("UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Camera uiCamera;

    private RectTransform panelRectTransform;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (shapeManager == null)
        {
            shapeManager = FindAnyObjectByType<ShapeManager>();
            if (shapeManager == null)
            {
                Debug.LogError("ShapeManager reference is not set and could not be found in the scene.");
            }
        }

        // Get or find Canvas reference
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("ManipulationUIManager: No Canvas found in scene!");
                }
            }
        }

        // Get camera reference (use main camera if UI camera not specified)
        if (uiCamera == null)
        {
            uiCamera = Camera.main;
            if (uiCamera == null)
            {
                Debug.LogError("ManipulationUIManager: No camera found!");
            }
        }

        // Get RectTransform of the manipulation panel
        if (manipulationPanel != null)
        {
            panelRectTransform = manipulationPanel.GetComponent<RectTransform>();
            if (panelRectTransform == null)
            {
                Debug.LogError("ManipulationUIManager: manipulationPanel doesn't have a RectTransform!");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (shapeManager.activeShape == null)
        {
            manipulationPanel.SetActive(false);
            return;
        }
        else
        {
            manipulationPanel.SetActive(true);
        }

        FollowActiveShape();
        UpdateConfirmButton();
    }

    private void FollowActiveShape()
    {
        if (shapeManager.activeShape == null || panelRectTransform == null || uiCamera == null || canvas == null)
            return;

        // Get the shape's world position
        Vector3 worldPos = shapeManager.GetWorldPositionFromGridPosition(shapeManager.activeShape.position);

        // Convert world position to screen position
        Vector3 screenPos = uiCamera.WorldToScreenPoint(worldPos);


        int halfScreenWidth = Screen.width / 2;
        int halfScreenHeight = Screen.height / 2;

        // Apply offset in screen space (pixels) where the reference screen size is 1080x1920 (portrait)
        Vector2Int panelOffset = shapeManager.activeShape.shapeData.manipulationPanelOffset;
        float screenWidthRatio = (float)Screen.width / 1080f;
        float screenHeightRatio = (float)Screen.height / 1920f;
        panelOffset.x = Mathf.RoundToInt(panelOffset.x * screenWidthRatio);
        panelOffset.y = Mathf.RoundToInt(panelOffset.y * screenHeightRatio);
        // If the shape is on the right half of the screen, flip the panel to the left side of the shape
        if (screenPos.x > halfScreenWidth)
        {
            panelOffset.x = -Mathf.Abs(panelOffset.x); // Ensure offset is negative
        }
        // If the shape is on the top half of the screen, flip the panel to the bottom side of the shape
        if (screenPos.y > halfScreenHeight)
        {
            panelOffset.y = -Mathf.Abs(panelOffset.y); // Ensure offset is negative
        }
        screenPos.x += panelOffset.x;
        screenPos.y += panelOffset.y;
        
        // Convert screen position to canvas local position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCamera,
            out Vector2 localPos
        );

        // Set the panel's anchored position
        panelRectTransform.anchoredPosition = localPos;
    }

    private void UpdateConfirmButton()
    {
        if (shapeManager.activeShape.CheckPlacementRules()) {
            ConfirmButton.GetComponent<Button>().interactable = true;
        } else {
            ConfirmButton.GetComponent<Button>().interactable = false;
        }
    }
}

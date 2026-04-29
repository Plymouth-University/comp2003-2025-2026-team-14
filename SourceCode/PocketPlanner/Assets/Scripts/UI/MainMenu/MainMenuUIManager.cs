using UnityEngine;

public class MainMenuUIManager : MonoBehaviour
{
    // Handles board coloring by updating GameManager
    [SerializeField] private TMPro.TMP_Dropdown boardColorDropdown; // Dropdown for selecting board color
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set up dropdown listener to update GameManager with selected color
        if (boardColorDropdown != null)
            boardColorDropdown.onValueChanged.AddListener(OnBoardColorChanged);

        if (GameManager.Instance != null)
            GameManager.Instance.PlayerBoardColor = Color.gray; // Set default color to gray
    }

    private void OnBoardColorChanged(int index)
    {
        // Map dropdown index to a color (this can be customized as needed)
        Color selectedColor = Color.gray; // Default color
        switch (index)
        {
            case 0: selectedColor = Color.gray; break; // Default gray
            case 1: selectedColor = new Color(0.8f, 0.3f, 0.3f, 1f);; break;   // Red
            case 2: selectedColor = new Color(0.3f, 0.3f, 0.8f, 1f); break;  // Blue
            case 3: selectedColor = new Color(0.3f, 0.8f, 0.3f, 1f); break; // Green
            case 4: selectedColor = new Color(0.6f, 0.3f, 0.6f, 1f); break; // Purple
            case 5: selectedColor = new Color(0.8f, 0.8f, 0.3f, 1f); break;// Yellow
            // Add more cases for additional colors if needed
        }

        // Update the GameManager with the selected board color
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerBoardColor = selectedColor;
    }

}

using UnityEngine;
using TMPro; // Required for TextMeshPro

public class Rules_central_brain_script : MonoBehaviour
{
    [Header("UI References")]
    // The single text component on your screen
    public TextMeshProUGUI ruleTextDisplay;

    [Header("The Rules")]
    // TextArea makes the input boxes much larger in the Unity Inspector!
    [TextArea(20, 10)]
    public string[] rulePages;

    // Keeps track of which rule we are looking at
    private int currentPage = 0;

    void Start()
    {
        // Show the first rule when the menu opens
        if (rulePages.Length > 0)
        {
            UpdateRuleDisplay();
        }
    }

    public void NextRule()
    {
        // Only go forward if we aren't on the last page
        if (currentPage < rulePages.Length - 1)
        {
            currentPage++;
            UpdateRuleDisplay();
            Debug.Log("Moved to next rule: " + currentPage);
        }
    }

    public void PreviousRule()
    {
        // Only go backward if we aren't on the first page
        if (currentPage > 0)
        {
            currentPage--;
            UpdateRuleDisplay();
            Debug.Log("Moved to previous rule: " + currentPage);
        }
    }

    private void UpdateRuleDisplay()
    {
        // Inject the text from our list into the UI component
        ruleTextDisplay.text = rulePages[currentPage];
    }
}
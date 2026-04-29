using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RulesUIManager : MonoBehaviour
{
    // Only one rule page panel should be active at a time, and the others should be inactive
    [SerializeField] private List<GameObject> rulePages; // List of rule page panels (12 panels in total)
    [SerializeField] private List<GameObject> pageIndicators; // List of page indicator objects (e.g. dots or numbers)
    [SerializeField] private Button NextButton; // Button to go to the next page (should be disabled on the last page)
    [SerializeField] private Button PreviousButton; // Button to go to the previous page (should be disabled on the first page)
    private int currentPageIndex = 0; // Index of the currently active page

    void Start()
    {
        // Show page 0 on start
        ShowPage(0);

        // Wire up button listeners
        if (NextButton != null)
            NextButton.onClick.AddListener(NextPage);
        if (PreviousButton != null)
            PreviousButton.onClick.AddListener(PreviousPage);
    }

    private void ShowPage(int index)
    {
        currentPageIndex = index;

        // Activate only the current page
        for (int i = 0; i < rulePages.Count; i++)
        {
            if (rulePages[i] != null)
                rulePages[i].SetActive(i == currentPageIndex);
        }

        // Update page indicators (e.g. dots highlighting)
        for (int i = 0; i < pageIndicators.Count; i++)
        {
            if (pageIndicators[i] != null)
                pageIndicators[i].SetActive(i == currentPageIndex);
        }

        UpdateNavigationButtons();
    }

    private void NextPage()
    {
        if (currentPageIndex < 11)
            ShowPage(currentPageIndex + 1);
    }

    private void PreviousPage()
    {
        if (currentPageIndex > 0)
            ShowPage(currentPageIndex - 1);
    }

    private void UpdateNavigationButtons()
    {
        if (PreviousButton != null)
            PreviousButton.interactable = currentPageIndex > 0;
        if (NextButton != null)
            NextButton.interactable = currentPageIndex < 11;
    }

}

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TextMeshProUGUI = TMPro.TextMeshProUGUI;

public class SwapInputField : MonoBehaviour
{
    public TMPro.TMP_InputField nextInputField;
    public TMPro.TMP_InputField previousInputField;
    public bool isStartingInputField = false;
    public bool isLastInputField = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (isStartingInputField)
        {
            GetComponent<TMPro.TMP_InputField>().ActivateInputField();
        }
        GetComponent<TMPro.TMP_InputField>().onValueChanged.AddListener(OnInputChanged);
    }

    void OnInputChanged(string value)
    {
        if (value.Length > 0 && nextInputField != null && !isLastInputField)
        {
            nextInputField.ActivateInputField();
        }
        else if (value.Length == 0 && previousInputField != null && !isStartingInputField)
        {
            previousInputField.ActivateInputField();
        }
        else if (isLastInputField && value.Length > 0)
        {
            GetComponent<TMPro.TMP_InputField>().DeactivateInputField();
        }
    }
}

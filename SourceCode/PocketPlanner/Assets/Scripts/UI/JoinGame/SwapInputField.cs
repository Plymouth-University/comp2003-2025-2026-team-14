using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TextMeshProUGUI = TMPro.TextMeshProUGUI;

public class SwapInputField : MonoBehaviour
{
    public TMPro.TMP_InputField nextInputField;
    public TMPro.TMP_InputField previousInputField;
    public bool isStartingInputField = false;
    public bool isLastInputField = false;
    private static TouchScreenKeyboard keyboard; // Static reference to the touch screen keyboard  

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TMPro.TMP_InputField inputField = GetComponent<TMPro.TMP_InputField>();
        inputField.shouldHideSoftKeyboard = true; // Prevent default soft keyboard from appearing on mobile - handle manually
        keyboard = null; // Ensure keyboard reference is reset on start
        if (isStartingInputField)
        {
            inputField.ActivateInputField();
        }
        GetComponent<TMPro.TMP_InputField>().onValueChanged.AddListener(OnInputChanged);
        GetComponent<TMPro.TMP_InputField>().onSelect.AddListener(OnSelect);
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
            dismissMobileKeyboard();
            GetComponent<TMPro.TMP_InputField>().DeactivateInputField();
        }
    }

    void OnSelect(string value)
    {
        if (keyboard == null)   
        {
            // Open touch screen keyboard on mobile platforms when an input field is selected
            #if UNITY_IOS || UNITY_ANDROID
            if (TouchScreenKeyboard.isSupported)
            {
                keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
            }
            #endif
        }

        GetComponent<TMPro.TMP_InputField>().ActivateInputField();
    }

    public void dismissMobileKeyboard()
    {
        // Also attached to the onclick of buttons in the inspector
        #if UNITY_IOS || UNITY_ANDROID
        if (keyboard != null)
        {
            keyboard.active = false; // Dismiss the touch screen keyboard
            keyboard = null; // Clear the reference
        }
        #endif
    }
}

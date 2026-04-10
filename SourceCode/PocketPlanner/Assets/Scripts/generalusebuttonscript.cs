using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class generalusebuttonscript: MonoBehaviour
{
    public Button button;
    public string targetScene;
    public void buttonClicked()
    {
        Debug.Log("Button click");
        SceneManager.LoadScene(targetScene);
    }
}

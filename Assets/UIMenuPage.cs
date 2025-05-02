using SceneManagement;
using UnityEngine;

public class UIMenuPage : MonoBehaviour
{
    public void OnQuit()
    {
        UIManager.Instance.ShowPopup("", "Are you sure you want to quit?",
            UIManager.PopupType.ContinueCancel,
            Application.Quit);
    }
    
    public void OnPlay(string sceneName)
    {
        SceneTransitionManager.Instance.LoadScene(sceneName);
    }
}

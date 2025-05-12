using SceneManagement;
using UnityEngine;

public class UIResultScreen : MonoBehaviour
{
    public void OnBackToHome()
    {
        SceneTransitionManager.Instance.LoadScene("Homepage");
    }

    public void OnQuit()
    {
        UIManager.Instance.ShowPopup("", "Are you sure you want to quit?",
            UIManager.PopupType.ContinueCancel,
            Application.Quit);
    }
}

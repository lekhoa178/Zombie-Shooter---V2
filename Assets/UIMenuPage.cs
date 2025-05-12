using SceneManagement;
using UnityEngine;

public class UIMenuPage : MonoBehaviour
{
    [SerializeField] private UIPage upgradePage;
    [SerializeField] private UIPage storyPage;
    
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

    public void OnUpgradeMenu()
    {
        UIManager.Instance.ShowPage(upgradePage);
    }
    
    public void OnStoryMenu()
    {
        UIManager.Instance.ShowPage(storyPage);
    }

    public void OnClosePage()
    {
        UIManager.Instance.GoBack();
    }
}

using UnityEngine;

public class UIHUDManager : MonoBehaviour
{
    public void OnSideMenuClicked(UIPage page)
    {
        UIManager.Instance.ShowPage(page);
    }
    
    public void OnCloseSideMenuClicked()
    {
        UIManager.Instance.GoBack();
    }
}

using UnityEngine;

public class MenuUI : MonoBehaviour
{
    public static MenuUI Instance { get; set; }

    [SerializeField] private Animator menuAnimator;

    private void Awake()
    {
        Instance = this;
    }

    public void OnLocalGameButton()
    {
        menuAnimator.SetTrigger("InGame");
    }
    public void OnOnlineGameButton()
    {
        menuAnimator.SetTrigger("OnlineGameMenu");
    }
    public void OnHostButton()
    {
        menuAnimator.SetTrigger("HostMenu");
    }
    public void OnConnectButton()
    {
        //menuAnimator.SetTrigger("InGame");
    }
    public void OnBackToMainMenuButton()
    {
        menuAnimator.SetTrigger("MainMenu");
    }
    public void OnBackToOnlineGameMenuButton()
    {
        menuAnimator.SetTrigger("OnlineGameMenu");
    }


}

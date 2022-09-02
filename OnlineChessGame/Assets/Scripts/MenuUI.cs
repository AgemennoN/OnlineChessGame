using TMPro;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    public static MenuUI Instance { get; set; }

    public Server server;
    public Client client;


    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private Animator menuAnimator;

    private void Awake()
    {
        Instance = this;
    }

    public void OnLocalGameButton()
    {
        menuAnimator.SetTrigger("InGame");
        server.Init(8008);
        client.Init("127.0.0.1", 8008);
    }
    public void OnOnlineGameButton()
    {
        menuAnimator.SetTrigger("OnlineGameMenu");
    }
    public void OnHostButton()
    {
        server.Init(8008);
        client.Init("127.0.0.1", 8008);
        menuAnimator.SetTrigger("HostMenu");
    }
    public void OnConnectButton()
    {
        client.Init(addressInput.text, 8008);
        //menuAnimator.SetTrigger("InGame");
    }
    public void OnBackToMainMenuButton()
    {
        menuAnimator.SetTrigger("MainMenu");
    }
    public void OnBackToOnlineGameMenuButton()
    {
        server.Shutdown();
        client.Shutdown();
        menuAnimator.SetTrigger("OnlineGameMenu");
    }


}
